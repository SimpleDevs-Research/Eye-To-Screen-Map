using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

public class EyeTrackingMonitor : MonoBehaviour
{   
    public static EyeTrackingMonitor Instance;

    public UnityEvent OnEyeTrackingActivated;
    public UnityEvent OnEyeTrackingDeactivated;

    public float poll_delay = 0.5f;
    private bool lastState = false;

    private void Awake() {
        Instance = this;
    }

    // Listen for permission
    private void OnEnable() {
        OVRPermissionsRequester.PermissionGranted += OnPermissionGranted;
    }
    private void OnDisable() {
        OVRPermissionsRequester.PermissionGranted -= OnPermissionGranted;
    }

    // Start polling
    private void Start() {
        StartCoroutine(PollEyeTrackingState());
    }

    // Runs every 0.5 seconds.
    private IEnumerator PollEyeTrackingState() {
        WaitForSeconds delay = new WaitForSeconds(poll_delay);
        while (true) {
            bool supported = OVRPlugin.eyeTrackingSupported;
            bool enabled = supported && OVRPlugin.eyeTrackingEnabled;

            if (enabled != lastState) {
                if (enabled) OnEyeTrackingActivated?.Invoke();
                else OnEyeTrackingDeactivated?.Invoke();

                lastState = enabled;
            }

            yield return delay;
        }
    }

    // Listener
    private void OnPermissionGranted(string permissionId) {
        if (permissionId == OVRPermissionsRequester.EyeTrackingPermission) {
            Debug.Log("[EyeTrackingMonitor] Permission granted for eye tracking.");
            // Permission granted; next poll will detect actual activation
        }
    }
}