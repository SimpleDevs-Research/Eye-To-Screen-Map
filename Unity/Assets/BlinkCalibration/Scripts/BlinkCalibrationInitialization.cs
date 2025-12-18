using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkCalibrationInitialization : MonoBehaviour
{
    public static BlinkCalibrationInitialization Instance;

    public BlinkCalibration cal_ref;
    public Vector3 relative_position = new Vector3(0f, -1.53f, 3.64f);
    public BlinkCalibrationTrial[] trials;
    [ReadOnlyInsp] public GameObject head_ref;
    [ReadOnlyInsp] public OVREyeGaze left_gaze_ref, right_gaze_ref;
    [ReadOnlyInsp] public int current_trial_index = -1;
    
    void Awake() {
        Instance = this;
    }

    void OnEnable() {
        // Set the event to make sure that when calibration is finished, we can do something
        cal_ref.onCalibrationFinished += CalibrationFinished;

        // We want to make sure that we can set the appriopriate parenting and refs.
        if (AdditiveSceneManager.Instance != null) {
            // Initialize GameObject query out reference
            GameObject query_ref;
            // First: is there a center eye? if so, parent the blink calibration to that parent.
            if (AdditiveSceneManager.Instance.TryGetRef("center_eye", out query_ref)) {
                // Calibrate position of BlinkCalibration object
                cal_ref.transform.SetParent(query_ref.transform);
                cal_ref.transform.localPosition = relative_position;
                cal_ref.transform.localRotation = Quaternion.identity;
                cal_ref.headRef = query_ref.transform;
                Debug.Log("Center eye Transform found!");
            }
            // Secondly, Gotta reference each over eye gaze
            if (AdditiveSceneManager.Instance.TryGetRef("left_eye_gaze", out query_ref)) {
                left_gaze_ref = query_ref.GetComponent<OVREyeGaze>();
                Debug.Log("Left eye OVR Gaze found!");
            }
            if (AdditiveSceneManager.Instance.TryGetRef("right_eye_gaze", out query_ref)) {
                right_gaze_ref = query_ref.GetComponent<OVREyeGaze>();
                Debug.Log("Right eye OVR Gaze found!");
            }

            // Finally, we only do trials if the left and right eyes are set
            if (left_gaze_ref != null && right_gaze_ref != null && trials.Length > 0) {
                NextTrial();
            }
        }

        // At the end, we activate the calibrator
        cal_ref.gameObject.SetActive(true);
    }

    void NextTrial() {
        current_trial_index += 1;
        // Terminate if we reached beyond the last trial
        if (current_trial_index >= trials.Length) {
            Debug.Log("All Trials Finished!");
            return;
        }
        Debug.Log($"Initializing next trial at index {current_trial_index}");
        BlinkCalibrationTrial trial = trials[current_trial_index];
        // Set FPS
        switch(trial.refresh_rate) {
            case 72:
                OVRPlugin.systemDisplayFrequency = 72f;
                break;
            default:
                OVRPlugin.systemDisplayFrequency = 90f;
                break;
        }
        // Set eye confidence threshold manually
        left_gaze_ref.ConfidenceThreshold = trial.confidence_threshold;
        right_gaze_ref.ConfidenceThreshold = trial.confidence_threshold;
        // Set the writer's filename
        cal_ref.writer.fileName = trial.trial_name;

        // Restart blink calibration
        cal_ref.gameObject.SetActive(true);
    }

    void CalibrationFinished() {
        // Disable the calibrator
        cal_ref.gameObject.SetActive(false);
        // We run the next trial if the right conditions are checked
        if (left_gaze_ref != null && right_gaze_ref != null && trials.Length > 0) {
            NextTrial();
        }
    }

    public float GetTrialConfidenceThreshold() {
        if (trials.Length == 0) return -1f;
        return trials[current_trial_index].confidence_threshold;
    }

    void OnDisable() {
        cal_ref.gameObject.SetActive(false);
        cal_ref.headRef = null;
        cal_ref.transform.SetParent(this.transform);
    }
}
