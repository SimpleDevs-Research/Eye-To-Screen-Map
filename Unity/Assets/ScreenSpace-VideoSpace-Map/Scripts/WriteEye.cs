using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OVREyeGaze))]
public class WriteEye : MonoBehaviour
{
    private OVREyeGaze eye_gaze_ref;    // Will be set at Awake

    [Header("=== Settings ===")]
    public Camera center_eye_ref;
    public Camera left_eye_ref;
    public Camera right_eye_ref;
    public CSVWriter writer;

    private void Awake() {
        // Set reference to OVREyeGaze
        eye_gaze_ref = GetComponent<OVREyeGaze>();
    }

    public void EnableWriter()
    {
        if (writer.is_active) {
            Debug.Log("Writer already enabled!");
            return;
        }
        Debug.Log("Initializing eye gaze writer!");
        writer.Initialize();
    }

    public void DisableWriter()
    {
        if (!writer.is_active)
        {
            Debug.Log("Writer is already disabled");
            return;
        }
        Debug.Log("Disabling eye gaze writer!");
        writer.Disable();
    }

    void Update()
    {
        if (!writer.is_active) return;

        int frame = Time.frameCount;
        Vector3 world_pos = eye_gaze_ref.transform.position + eye_gaze_ref.transform.forward * 200f;
        RaycastHit hit;
        if (Physics.Raycast(eye_gaze_ref.transform.position, eye_gaze_ref.transform.forward, out hit, 200))
        {
            world_pos = hit.point;
        }
        Vector3 center_screen_pos = center_eye_ref.WorldToScreenPoint(world_pos);
        Vector3 left_screen_pos = left_eye_ref.WorldToScreenPoint(world_pos);
        Vector3 right_screen_pos = right_eye_ref.WorldToScreenPoint(world_pos);

        writer.AddPayload(frame);
        writer.AddPayload(IPDMeasurer.Instance.iipd);
        writer.AddPayload(world_pos);
        writer.AddPayload(center_screen_pos);
        writer.AddPayload(left_screen_pos);
        writer.AddPayload(right_screen_pos);
        writer.WriteLine();
    }

    void OnDestroy() {
        if (writer.is_active) writer.Disable();
    }
}
