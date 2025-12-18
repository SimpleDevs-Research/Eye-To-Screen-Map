using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BlinkCalibration : MonoBehaviour
{
    enum State { TURNAROUND, START, COUNTDOWN, METRONOME, END, STUDYCOMPLETE }
    [Header("Parameters")]
    public Transform headRef;
    [SerializeField] private Image m_movingDot;
    [SerializeField] private TextMeshProUGUI m_tmp;
    [SerializeField] private float m_movingDotTime;
    [SerializeField] private float m_movingDotExtent;
    [SerializeField] private float m_movingDotCenterLeniency;
    [SerializeField] private Color m_movingDotOffColor;
    [SerializeField] private Color m_movingDotOnColor;
    [SerializeField] private int m_totalOverlaps;
    [SerializeField] private string m_nextScene;
    [SerializeField] public Vector3 m_targetForward;
    [SerializeField] private Animator m_animator;

    [Header("=== OUTPUT WRITER ===")]
    public CSVWriter writer;

    [Header("Outcomes -- READ ONLY")]
    [SerializeField] private float start_timestamp;
    [SerializeField] private State m_state;
    [SerializeField] private int m_moveDir;
    [SerializeField] private int m_overlaps;
    [SerializeField] private float m_movingDotSpeed;
    [SerializeField] private int m_lastOverlapDir;
    [SerializeField] private Vector3 m_movingDotOriginPos;

    public static BlinkCalibration Instance;

    public delegate void CalibrationFinishedEvent();
    public CalibrationFinishedEvent onCalibrationFinished;

    void Awake()
    {
        Instance = this;
        m_animator = GetComponent<Animator>();
        m_movingDotOriginPos = m_movingDot.rectTransform.localPosition;
    }

    void Update()
    {
        if (headRef == null) return;
        switch (m_state) {
            case State.TURNAROUND:
                m_animator.SetTrigger("HideAll");
                m_tmp.text = "Please turn around";
                float diff = Vector3.Angle(m_targetForward, headRef.forward);
                if (diff < 25)
                {
                    m_state = State.START;
                }
                break;
            case State.START:
                m_tmp.text = "Blink when the circles overlap";
                m_movingDot.rectTransform.localPosition = m_movingDotOriginPos;
                m_movingDot.color = m_movingDotOffColor;
                m_moveDir = 1;
                m_movingDotSpeed = m_movingDotExtent / m_movingDotTime;
                m_state = State.COUNTDOWN;
                m_overlaps = 0;
                m_lastOverlapDir = 0;
                m_animator.SetTrigger("BeginCalibration");

                // Start Writer
                start_timestamp = Time.time;
                writer.Initialize();
                WriteState("Start");
                break;

            case State.METRONOME:
                m_movingDot.rectTransform.Translate(new Vector3(m_moveDir * m_movingDotSpeed * Time.deltaTime, 0, 0));
                if(Mathf.Abs(m_movingDot.transform.localPosition.x) <= m_movingDotCenterLeniency)
                {
                    m_movingDot.color = m_movingDotOnColor;
                    if(m_lastOverlapDir != m_moveDir)
                    {
                        m_overlaps += 1;
                        WriteOverlap();
                        m_lastOverlapDir = m_moveDir;
                    }
                } else
                {
                    m_movingDot.color = m_movingDotOffColor;
                }
                if(m_movingDot.rectTransform.localPosition.x > m_movingDotExtent && m_moveDir == 1)
                {
                    m_moveDir = -1;
                }
                if (m_movingDot.rectTransform.localPosition.x < -m_movingDotExtent && m_moveDir == -1)
                {
                    m_moveDir = 1;
                }
                if (m_overlaps >= m_totalOverlaps)
                {
                    m_state = State.END;
                    m_tmp.text = "Calibration complete";
                    StartCoroutine(DelayThenNext());
                }
                break;

        }
    }
    public IEnumerator DelayThenNext()
    {
        yield return new WaitForSeconds(2.0f);
        WriteState("End");
        writer.Disable();
        if(m_nextScene != "")
            SceneManager.LoadScene(m_nextScene, LoadSceneMode.Single);
        else
        {
            m_state = State.TURNAROUND;
            onCalibrationFinished?.Invoke();
        }
    }
    public void StartAnimFinished()
    {
        m_state = State.METRONOME;
    }
    private void WriteOverlap() {
        writer.AddPayload(Time.frameCount); // Current frame
        writer.AddPayload(Time.time - start_timestamp); // Relative timestamp
        writer.AddPayload("Overlap");
        writer.AddPayload(m_overlaps);

        writer.AddPayload(IPDMeasurer.Instance.iipd);   // IPD
        writer.AddPayload(FrameCount.Instance.fps); // Raw FPS
        writer.AddPayload(FrameCount.Instance.smoothed_fps); // Smooth FPS
        writer.AddPayload(BlinkCalibrationInitialization.Instance.GetTrialConfidenceThreshold()); // Confidence Threshold

        writer.WriteLine();
    }
    private void WriteState(string s) {
        writer.AddPayload(Time.frameCount);
        writer.AddPayload(Time.time - start_timestamp);
        writer.AddPayload(s);
        writer.AddPayload(m_overlaps);

        writer.AddPayload(IPDMeasurer.Instance.iipd);   // IPD
        writer.AddPayload(FrameCount.Instance.fps); // Raw FPS
        writer.AddPayload(FrameCount.Instance.smoothed_fps); // Smooth FPS
        writer.AddPayload(BlinkCalibrationInitialization.Instance.GetTrialConfidenceThreshold()); // Confidence Threshold

        writer.WriteLine();
    }
    public void ShowEnd()
    {
        StopCoroutine(DelayThenNext());
        m_state = State.STUDYCOMPLETE;
        m_tmp.text = "The study has concluded.\nPlease inform the researcher.";
    }
}
