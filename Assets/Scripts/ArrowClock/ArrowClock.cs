using System;
using UnityEngine;

[ExecuteAlways]
public class ArrowClock : MonoBehaviour
{
    public enum Mode
    {
        SystemTime,          
        GameTimeFromStart,
        Manual
    }

    [Header("References (RectTransforms of the hand sprites)")]
    [SerializeField]
    private RectTransform hourHand;

    [SerializeField]
    private RectTransform minuteHand;

    [SerializeField]
    private RectTransform secondHand;

    [Header("Mode")]
    [SerializeField]
    private Mode mode = Mode.SystemTime;

    [Tooltip("If true, uses DateTime.Now; if false, uses DateTime.UtcNow + TimezoneOffsetMinutes.")]
    [SerializeField] 
    private bool useLocalSystemTime = true;

    [Tooltip("Minutes to add when useLocalSystemTime is false (applied to UTC).")]
    [SerializeField] 
    private int timezoneOffsetMinutes = 0;

    [Header("Game Time (Mode = GameTimeFromStart)")]
    [Tooltip("Start time of day (ignored in SystemTime).")]
    [SerializeField]
    private TimeSpan startTimeOfDay = new TimeSpan(9, 30, 0);

    [Tooltip("How many in-game seconds pass per real-time second.")]
    [SerializeField]
    private double gameSecondsPerRealSecond = 60.0;

    [Tooltip("If true, uses unscaledDeltaTime so timescale doesn’t affect the clock.")]
    [SerializeField]
    private bool useUnscaledDeltaTime = true;

    [Header("Manual (Mode = Manual)")]
    [SerializeField] 
    private TimeSpan manualTimeOfDay = new TimeSpan(12, 0, 0);

    [Header("Hand Motion")]
    [Tooltip("Hour hand moves smoothly with minutes/seconds.")]
    [SerializeField]
    private bool smoothHour = true;

    [Tooltip("Minute hand moves smoothly with seconds.")]
    [SerializeField]
    private bool smoothMinute = true;

    [Tooltip("Second hand moves continuously (true) or ticks per second (false).")]
    [SerializeField]
    private bool smoothSecond = true;

    [Header("Dial Orientation")]
    [Tooltip("Angle (in degrees) the hands should have when time is 12:00:00. If your arrow points straight up by default, set 0. If it points right, set 90.")]
    [SerializeField]
    private float angleAtTwelve = 0f;

    [Tooltip("If true, angles increase clockwise (typical analog clock).")]
    [SerializeField]
    private bool clockwise = true;

    private double gameSeconds;
    private bool initialized;

    private void OnEnable()
    {
        EnsureInit();
        UpdateHandsEditorSafe();
    }

    private void Reset()
    {
        angleAtTwelve = 0f;
        clockwise = true;
        smoothHour = smoothMinute = true;
        smoothSecond = true;
        useLocalSystemTime = true;
        gameSecondsPerRealSecond = 60.0;
        startTimeOfDay = new TimeSpan(9, 30, 0);
        manualTimeOfDay = new TimeSpan(12, 0, 0);
    }

    private void EnsureInit()
    {
        if (initialized) return;

        if (mode == Mode.GameTimeFromStart)
        {
            gameSeconds = startTimeOfDay.TotalSeconds;
        }
        else if (mode == Mode.Manual)
        {
            gameSeconds = manualTimeOfDay.TotalSeconds;
        }

        initialized = true;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateHandsEditorSafe();
            return;
        }
#endif
        Tick(Time.deltaTime, Time.unscaledDeltaTime);
        UpdateHandsEditorSafe();
    }

    private void Tick(float dt, float unscaledDt)
    {
        if (mode == Mode.GameTimeFromStart)
        {
            double delta = (useUnscaledDeltaTime ? (double)unscaledDt : (double)dt) * gameSecondsPerRealSecond;
            gameSeconds = NormalizeSeconds(gameSeconds + delta);
        }
        else if (mode == Mode.Manual)
        {
            gameSeconds = NormalizeSeconds(gameSeconds);
        }
    }

    private void UpdateHandsEditorSafe()
    {
        TimeSpan nowSpan = GetCurrentTimeOfDay();

        GetHandAngles(nowSpan, out float hAngle, out float mAngle, out float sAngle);

        float dir = clockwise ? -1f : 1f;
        float baseOffset = angleAtTwelve;

        if (hourHand)
        {
            hourHand.localRotation = Quaternion.Euler(0, 0, baseOffset + dir * hAngle);
        }

        if (minuteHand)
        {
            minuteHand.localRotation = Quaternion.Euler(0, 0, baseOffset + dir * mAngle);
        }

        if (secondHand)
        {
            secondHand.localRotation = Quaternion.Euler(0, 0, baseOffset + dir * sAngle);
        }
    }

    private TimeSpan GetCurrentTimeOfDay()
    {
        switch (mode)
        {
            case Mode.SystemTime:
                DateTime dt = useLocalSystemTime ? DateTime.Now : DateTime.UtcNow.AddMinutes(timezoneOffsetMinutes);
                return dt.TimeOfDay;

            case Mode.GameTimeFromStart:
                return TimeSpan.FromSeconds(NormalizeSeconds(gameSeconds));

            case Mode.Manual:
                return TimeSpan.FromSeconds(NormalizeSeconds(gameSeconds));

            default:
                return TimeSpan.Zero;
        }
    }

    private static double NormalizeSeconds(double sec)
    {
        // wrap to [0, 86400)
        const double day = 24d * 60d * 60d;
        sec %= day;
        if (sec < 0) sec += day;
        return sec;
    }

    private void GetHandAngles(TimeSpan t, out float hourDeg, out float minuteDeg, out float secondDeg)
    {
        // Seconds
        double seconds = smoothSecond ? (t.Seconds + t.Milliseconds / 1000.0) : t.Seconds;
        secondDeg = (float)(seconds * 6.0); // 360/60

        // Minutes
        double minutes = smoothMinute ? (t.Minutes + seconds / 60.0) : t.Minutes;
        minuteDeg = (float)(minutes * 6.0); // 360/60

        // Hours (12h dial)
        int hour12 = t.Hours % 12;
        double hours = smoothHour ? (hour12 + minutes / 60.0) : hour12;
        hourDeg = (float)(hours * 30.0); // 360/12
    }

    // -----------------------
    // Public API
    // -----------------------

    public void SetMode(Mode newMode)
    {
        mode = newMode;
        EnsureInit();
    }

    public void SetStartTimeOfDay(TimeSpan start)
    {
        startTimeOfDay = start;

        if (mode == Mode.GameTimeFromStart)
        {
            gameSeconds = start.TotalSeconds;
        }
    }

    public void SetRate(double secondsPerRealSecond)
    {
        gameSecondsPerRealSecond = Math.Max(0.0, secondsPerRealSecond);
    }

    private double _savedRate = -1;

    public void Pause()
    {
        if (_savedRate < 0)
        {
            _savedRate = gameSecondsPerRealSecond;
        }

        gameSecondsPerRealSecond = 0.0;
    }

    public void Resume()
    {
        if (_savedRate >= 0)
        {
            gameSecondsPerRealSecond = _savedRate;
            _savedRate = -1;
        }
    }

    public void Nudge(double seconds)
    {
        if (mode == Mode.SystemTime) return;
        gameSeconds = NormalizeSeconds(gameSeconds + seconds);
    }

    public void SetManualTime(TimeSpan timeOfDay)
    {
        manualTimeOfDay = timeOfDay;

        if (mode == Mode.Manual)
        {
            gameSeconds = manualTimeOfDay.TotalSeconds;
        }
    }

    public void SetManualTime(int hours, int minutes, int seconds)
    {
        SetManualTime(new TimeSpan(hours % 24, minutes % 60, seconds % 60));
    }

    public void SetSecondsHandVisible(bool visible)
    {
        if (secondHand)
        {
            secondHand.gameObject.SetActive(visible);
        }
    }
}

