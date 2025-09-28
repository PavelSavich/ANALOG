using UnityEngine;

public class SevenSegLoading : MonoBehaviour
{
    [Header("Target 7-segment display")]
    [SerializeField]
    private SevenSegRenderer target;

    [Header("Playback")]
    [Min(0.01f)]
    [SerializeField]
    private float framesPerSecond = 8f;

    [SerializeField]
    private bool playOnEnable = true;

    [SerializeField]
    private bool loop = true;

    private float _timer;
    private int _frame;
    private bool _playing;

    private const int FrameCount = 6;

    private void OnEnable()
    {
        _frame = 0;
        _timer = 0f;
        _playing = playOnEnable;

        if (target != null)
        {
            ShowFrame(_frame);
        }
    }

    private void Update()
    {
        if (!_playing || target == null)
        {
            return;
        }

        _timer += Time.deltaTime;
        float frameDur = 1f / Mathf.Max(0.01f, framesPerSecond);

        if (_timer >= frameDur)
        {
            _timer -= frameDur;
            _frame++;

            if (_frame >= FrameCount)
            {
                if (loop)
                {
                    _frame = 0;
                }
                else
                {
                    _frame = FrameCount - 1;
                    _playing = false;
                }
            }

            ShowFrame(_frame);
        }
    }

    private void ShowFrame(int index)
    {
        byte mask = SevenSegLut.ForDigit((int)SevenSegSymbol.LoadingA + index);

        target.map.bits = mask;
        target.Apply();
    }

    // --- Public controls ---
    public void Play()
    {
        _playing = true; 
    }

    public void Pause() 
    { 
        _playing = false;
    }

    public void Stop()
    {
        _playing = false;
        _frame = 0;

        if (target != null)
        {
            ShowFrame(_frame);
        }
    }

    public void SetSpeed(float fps)
    {
        framesPerSecond = Mathf.Max(0.01f, fps);
    }
}
