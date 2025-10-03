using UnityEngine;

public class SevenSegCountdown : MonoBehaviour
{
    [Header("Target 7-segment display")]
    [SerializeField]
    private SevenSegRenderer target;   // Assign your SevenSegRenderer in Inspector

    [Header("Playback Settings")]
    [Min(0.1f)]
    [SerializeField]
    private float secondsPerDigit = 1f; // Time to show each digit

    [SerializeField]
    private bool playOnEnable = true;

    private float _timer;
    private int _currentDigit = 9;
    private bool _playing;

    private void OnEnable()
    {
        _timer = 0f;
        _currentDigit = 9;
        _playing = playOnEnable;

        if (target != null)
        {
            ShowDigit(_currentDigit);
        }
    }

    private void Update()
    {
        if (!_playing || target == null)
        {
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= secondsPerDigit)
        {
            _timer -= secondsPerDigit;
            _currentDigit--;

            if (_currentDigit < 0)
            {
                _currentDigit = 9; 
            }

            ShowDigit(_currentDigit);
        }
    }

    private void ShowDigit(int digit)
    {
        byte mask = SevenSegLut.ForDigit(digit);
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

    public void ResetCountdown()
    {
        _currentDigit = 9;
        _timer = 0f;

        if (target != null)
        {
            ShowDigit(_currentDigit);
        }
    }
}
