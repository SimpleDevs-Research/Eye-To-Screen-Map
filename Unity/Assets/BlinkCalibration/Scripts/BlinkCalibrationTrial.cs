using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlinkCalibrationTrial", menuName = "BlinkCalibration/Trial", order = 1)]
public class BlinkCalibrationTrial : ScriptableObject
{
    public string trial_name;
    [Range(0f,1f)] public float _confidence_threshold;
    public float confidence_threshold => _confidence_threshold;
    public RefreshRate _refresh_rate;
    public int refresh_rate => (int)_refresh_rate;

    public enum RefreshRate { SeventyTwo = 72, Eighty = 90, Ninety = 90 }
}
