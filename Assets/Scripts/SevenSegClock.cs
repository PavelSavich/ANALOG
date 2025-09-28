using UnityEngine;
using System;

public class SevenSegClock : MonoBehaviour
{
    [Header("Assign SevenSegRenderer objects in order (H1 H2 M1 M2 [S1 S2])")]
    public SevenSegRenderer hourTens;
    public SevenSegRenderer hourOnes;
    public SevenSegRenderer minuteTens;
    public SevenSegRenderer minuteOnes;

    [Space]
    public SevenSegRenderer secondTens;   // Optional
    public SevenSegRenderer secondOnes;   // Optional

    [Header("Clock Settings")]
    public bool useSystemTime = true;     // If false, you can set custom time
    public int customHour = 0;            // 0–23
    public int customMinute = 0;          // 0–59
    public int customSecond = 0;          // 0–59
    public float refreshRate = 1f;        // How often to update (in seconds)

    private float _timer;

    private void OnEnable()
    {
        UpdateClock();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= refreshRate)
        {
            _timer -= refreshRate;
            UpdateClock();
        }
    }

    private void UpdateClock()
    {
        int hour, minute, second;

        if (useSystemTime)
        {
            DateTime now = DateTime.Now;
            hour = now.Hour;   // 0–23
            minute = now.Minute;
            second = now.Second;
        }
        else
        {
            hour = Mathf.Clamp(customHour, 0, 23);
            minute = Mathf.Clamp(customMinute, 0, 59);
            second = Mathf.Clamp(customSecond, 0, 59);
        }

        // Split into digits
        int h1 = hour / 10;
        int h2 = hour % 10;
        int m1 = minute / 10;
        int m2 = minute % 10;
        int s1 = second / 10;
        int s2 = second % 10;

        // Show digits
        if (hourTens != null) ShowDigit(hourTens, h1);
        if (hourOnes != null) ShowDigit(hourOnes, h2);
        if (minuteTens != null) ShowDigit(minuteTens, m1);
        if (minuteOnes != null) ShowDigit(minuteOnes, m2);

        // Optional seconds
        if (secondTens != null) ShowDigit(secondTens, s1);
        if (secondOnes != null) ShowDigit(secondOnes, s2);
    }

    private void ShowDigit(SevenSegRenderer renderer, int digit)
    {
        byte mask = SevenSegLut.ForDigit(digit);
        renderer.map.bits = mask;
        renderer.Apply();
    }
}
