using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class IPDMeasurer : MonoBehaviour
{

    public static IPDMeasurer Instance;

    public enum PrintType { Float, Int }
    [System.Serializable]
    public class _Textbox {
        public TextMeshProUGUI textbox;
        public PrintType print_type;
    }

    public Transform left_eye_ref;
    public Transform right_eye_ref;

    [SerializeField, ReadOnlyInsp] private float _ipd = 0f;
    public float ipd => ipd;
    public int iipd => Mathf.RoundToInt(_ipd);
    public List<_Textbox> textboxes = new List<_Textbox>();

    void Awake() {
        Instance = this;
    }

    void Update() {
        if (left_eye_ref == null || right_eye_ref == null) {
            Debug.LogError("Cannot measure IPD from missing eye refs");
            return;
        }

        _ipd = Vector3.Distance(left_eye_ref.position, right_eye_ref.position) * 1000f;
        if (textboxes.Count > 0) {
            foreach(_Textbox t in textboxes) {
                t.textbox.text = (t.print_type == PrintType.Int) 
                    ? iipd.ToString()
                    : _ipd.ToString();
            }
        }
    }
}
