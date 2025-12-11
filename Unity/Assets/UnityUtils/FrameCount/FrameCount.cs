using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameCount : MonoBehaviour
{

    public static FrameCount Instance;

    public enum TextboxWriteType { FrameCount, FPS, SmoothFPS }

    [System.Serializable]
    public class Textbox
    {
        public TextMeshProUGUI textbox;
        public TextboxWriteType write_type;
    }
    
    [Header("=== Calculated (Read-Only) ===")]
    [Tooltip("The raw frame number of the current frame. Wil always increment from 0 at the start of the scene.")]
    [SerializeField, ReadOnlyInsp] private int _frame_count;
    [Tooltip("The raw frames-per-second calculated.")]
    [SerializeField, ReadOnlyInsp] private float _fps;
    [Tooltip("A smoothened FPS based on a ratio between the previous FPS calculated and the current raw FPS")]
    [SerializeField, ReadOnlyInsp] private float _smoothed_fps;
    private float _prev_fps = 0f;

    [Header("=== Settings ===")]
    [Range(0f,1f), Tooltip("The ratio for calculating the smoothed FPS. 1 = focus only on raw FPS on the current frame, 0 = focus only on the previous FPS of the previous frame.")]
    public float smoothed_ratio = 0.75f;
    [Tooltip("Which textboxes should this write to?")]
    public Textbox[] textboxes;

    // Outputs readable to other external scripts    
    public int frame_count => _frame_count;
    public float fps => _fps;
    public float smoothed_fps => _smoothed_fps;

    private void Awake() { 
        Instance = this; 
    }

    private void Update() {
        _frame_count = Time.frameCount;
        _fps = 1f / Time.unscaledDeltaTime;
        _smoothed_fps = _prev_fps * (1f-smoothed_ratio) + _fps * smoothed_ratio;
        _prev_fps = _smoothed_fps;
        foreach(Textbox t in textboxes) {
            switch(t.write_type)
            {
                case TextboxWriteType.FrameCount:
                    t.textbox.text = _frame_count.ToString();
                    break;
                case TextboxWriteType.FPS:
                    t.textbox.text = _fps.ToString();
                    break;
                case TextboxWriteType.SmoothFPS:
                    t.textbox.text = _smoothed_fps.ToString();
                    break;
            }
        }

    }
    
    
}
