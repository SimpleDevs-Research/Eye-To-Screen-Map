import cv2
import os
import numpy as np 
import os
import pandas as pd
from tqdm import tqdm
import argparse
import warnings
import helpers as h
from classes import Trial, Frame

pd.options.mode.chained_assignment = None  # default='warn'
warnings.filterwarnings(
    "ignore",
    message="'pin_memory' argument is set as true but not supported on MPS"
)

# Initialize variables
drawing = False     # True while mouse is down
ix, iy = -1, -1     # Initial mouse down position
ex, ey = -1, -1     # Current mouse move position
x1, x2, y1, y2 = -1,-1,-1,-1
roi_selected = False
roi = None          # The selected region
frame_for_roi = None
display_frame = None

# Bounding Box drawer
def draw_rectangle(event, x, y, flags, param):
    global ix, iy, ex, ey, x1, y1, x2, y2, drawing, roi_selected, display_frame, roi

    if event == cv2.EVENT_LBUTTONDOWN:
        drawing = True
        ix, iy = x, y
        ex, ey = x, y

    elif event == cv2.EVENT_MOUSEMOVE:
        if drawing:
            ex, ey = x, y
            # Show live-updating rectangle
            temp = frame_for_roi.copy()
            cv2.rectangle(temp, (ix, iy), (ex, ey), (0, 255, 0), 2)
            display_frame = temp

    elif event == cv2.EVENT_LBUTTONUP:
        drawing = False
        ex, ey = x, y
        roi_selected = True

        # Ensure coordinates are sorted
        x1, x2 = min(ix, ex), max(ix, ex)
        y1, y2 = min(iy, ey), max(iy, ey)

        roi = frame_for_roi[y1:y2, x1:x2].copy()
        print(f"ROI: ({x1},{y1}) - ({x2},{y2}) w/ shape ", roi.shape)

        # Draw the final rectangle
        temp = frame_for_roi.copy()
        cv2.rectangle(temp, (x1, y1), (x2, y2), (0, 255, 0), 2)
        display_frame = temp

# ------------------------------------------------------------
# ESTIMATIONS: Given a trial with a Transformer established, map video frames to Unity frames.
# Then remap positional data from VR screenspace to video space. This outputs a new video with
# The positions marked and a CSV with the re-positioned entities.
# ------------------------------------------------------------

def frame_count_bounding_box(video_filepath:str):
    global frame_for_roi, display_frame, roi, x1, y1, x2, y2

    # Initialize a frame capture, get the first frame
    cap = cv2.VideoCapture(video_filepath)
    ret, frame = cap.read()
    if not ret:
        raise RuntimeError("Failed to read first frame.")
    frame_for_roi = frame.copy()
    display_frame = frame.copy()
    cv2.namedWindow("Select ROI")
    cv2.setMouseCallback("Select ROI", draw_rectangle)

    # ROI Selection Loop
    while True:
        cv2.imshow("Select ROI", display_frame)

        key = cv2.waitKey(1) & 0xFF
        if key == 13:  # ENTER key
            break
        if key == 27:  # ESC key cancels
            roi = None
            break
    cv2.waitKey(1)  # 1 ms delay
    cv2.destroyWindow("Select ROI")

    if roi is None:
        print("No ROI selected.")
    else:
        print("ROI stored! shape =", roi.shape)

    return (x1,y1), (x2,y2)


def estimate_positions(trial:Trial, 
                       positions_filename:str, 
                       video_filename:str,
                       frame_colname:str='frame',
                       x_colname:str='left_screen_pos_x',
                       y_colname:str='left_screen_pos_y',
                       output_video:bool=False,
                       preview:bool=False,
                       verbose:bool=True):
    
    # Assertions for necessary files and the Transformer
    positions_filepath = os.path.join(trial.root_dir, positions_filename)
    video_filepath = os.path.join(trial.root_dir, video_filename)
    assert os.path.exists(positions_filepath), f"Anchor image '{positions_filepath}' does not exist."
    assert os.path.exists(video_filepath), f"Requested video '{video_filepath}' does not exist in the root directory."
    assert trial.transformer is not None, "The trial does not have a Transformer set; make sure to assign a Transformer first."

    # Create output directory
    outdir = h.mkdirs(os.path.join(trial.root_dir, 'estimations'))

    # Extract positions dataframe, for reference later
    pdf = pd.read_csv(positions_filepath)
    pdf[frame_colname] = pdf[frame_colname].astype(int)

    # Prepare video(s)
    cap = cv2.VideoCapture(video_filepath)  # Get a cpature window
    assert cap.isOpened(), f"Could not open video '{video_filename}'"
    if output_video:
        output_video_basename, output_video_extension = os.path.splitext(video_filename)
        fps    = cap.get(cv2.CAP_PROP_FPS)
        width  = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        codec, output_ext = h.derive_fourcc_codec(cap, verbose=verbose)
        fourcc = cv2.VideoWriter_fourcc(*codec)
        output_video_filepath = os.path.join(outdir, output_video_basename+output_ext)
        out = cv2.VideoWriter(output_video_filepath, fourcc, fps, (width, height))

    # Allow the user to select a bounding box for identifying frame counts in the video
    bbox_min, bbox_max = frame_count_bounding_box(video_filepath)
    print("ROI coordinates:", bbox_min, bbox_max)

    # Iterate through video frames. Open preview window if we are previewing
    fidx, success = 0, True
    reposition_dfs = []
    if preview:
        cv2.namedWindow("Position Estimation")
    while success:
        # Read frame from video, exit early if issue arises
        ok, frame = cap.read() # Read frame
        if not ok: 
            if verbose: print("\tEnding frame analysis")
            break
        # Copy the frame if outputting
        if output_video or preview:
            outframe = Frame(fidx)
            outframe.set_frame(frame.copy())
        # Use OCR to interpret VR frame index from video frame
        vr_frame_number, is_int = h.check_frame_number(frame, bbox_min, bbox_max, return_frames=False)
        # If we know it's an integer, strong likelihood that it's a frame. Let's process
        if is_int:
            # Find all rows where the frame number matches
            frame_positions = pdf[pdf[frame_colname]==int(vr_frame_number)]
            if len(frame_positions.index) > 0:
                # Extract the positions in vr screen space
                xs = frame_positions[x_colname].tolist()
                ys = frame_positions[y_colname].tolist()
                positions = list(zip(xs, ys))
                # Transform the vr screen space coords to video coords
                repositions = [trial.transformer.screen_to_frame(p) for p in positions]
                rx, ry = zip(*repositions)
                frame_positions['video_x'] = rx
                frame_positions['video_y'] = ry
                # Cache the results
                reposition_dfs.append(frame_positions)
                # If we are outputting, we modify the outframe
                if output_video or preview:
                    for rp in repositions: 
                        outframe.draw_marker(rp, color=[255,225,0], inplace=True)
        # if we are outputting, write the frame
        if output_video: out.write(outframe.frame)
        if preview: 
            cv2.imshow("Position Estimation", outframe.frame)
            cv2.waitKey(1)  # 1 ms delay
        fidx += 1
    # Reached the end, closing cap
    cap.release()
    if preview:
        cv2.destroyWindow("Position Estimation")

    # Outputting results
    rpdf = pd.concat(reposition_dfs, axis=0)
    rpdf.to_csv(os.path.join(outdir, 'repositions.csv'), index=0)
    if output_video: out.release()

    # Close and return
    return rpdf


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('root_dir', help="Relative directory to your trial", type=str)
    parser.add_argument('trial_filename', help="Trial filename relative to your root directory", type=str)
    parser.add_argument('positions_filename', help="Fileame of the positions file, including extension, relative to the trial dir", type=str)
    parser.add_argument('video_filename', help="Fileame of the video file, including extension, relative to the trial dir", type=str)
    parser.add_argument('-o', '--output_video', help="If set, will generate an output video with the transformed positions per frame", action="store_true")
    parser.add_argument('-p', '--preview', help="If set, will preview transformations live", action="store_true")
    args = parser.parse_args()

    trial = Trial(root_dir=args.root_dir, json_src=args.trial_filename)
    estimate_positions(trial, args.positions_filename, args.video_filename, output_video=args.output_video, preview=args.preview, verbose=True )
