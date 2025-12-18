using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTrackDisplay : MonoBehaviour
{

    [ReadOnlyInsp] public GameObject eye;
    private bool is_active = false;

    private void OnEnable() {
        if (AdditiveSceneManager.Instance != null) is_active = AdditiveSceneManager.Instance.TryGetRef("left_eye_gaze", out eye);
    }

    void Update() {
        if (!is_active) return;
        
    }
}
