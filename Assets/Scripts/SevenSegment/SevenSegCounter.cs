using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SevenSegCounter : MonoBehaviour
{
    [Header("Digits (index 0=ones, 1=tens, 2=hundreds, ...)")]
    [SerializeField]
    private List<SevenSegRenderer> digits = new List<SevenSegRenderer>();

    [Header("Behavior")]
    [SerializeField]
    private bool showLeadingZeros = true;          // If false, higher unused places show Empty

    [SerializeField]
    private bool playErrorIfInvalid = true;

    [Min(0f)]
    [SerializeField]
    private float errorHoldSeconds = 1.25f;

    [SerializeField]
    private AfterErrorBehavior afterError = AfterErrorBehavior.ResetToZero;

    [Header("Initial")]
    [Min(0)]
    [SerializeField]
    private int initialValue = 0;

    private int _current;         // current valid value
    private int _lastValid;       // last valid value
    private bool _errorActive;

    private Coroutine _errorCo;

    private void OnEnable()
    {
        _lastValid = Mathf.Max(0, initialValue);
        _current = Mathf.Max(0, initialValue);
        ForceRender(_current);
    }

    // -------------------- Public API --------------------

    public void Add(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        TrySet(_current + amount);
    }

    public void Reduce(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        TrySet(_current - amount);
    }

    public void Set(int amount)
    {
        TrySet(amount);
    }

    public void Reset()
    {
        _current = 0;
        _lastValid = 0;
        ForceRender(_current);
    }

    // -------------------- Core --------------------

    private void TrySet(int value)
    {
        if (_errorActive)
        {
            return; // ignore requests while flashing error
        }

        if (IsRepresentable(value))
        {
            _lastValid = value;
            _current = value;
            ForceRender(_current);
        }
        else
        {
            if (!playErrorIfInvalid)
            {
                return;
            }

            // show ERROR on all digits for a bit
            if (_errorCo != null)
            {
                StopCoroutine(_errorCo);
            }

            _errorCo = StartCoroutine(ErrorRoutine());
        }
    }

    private bool IsRepresentable(int value)
    {
        if (value < 0)
        {
            return false;
        }

        int cap = MaxValue();
        return value <= cap;
    }

    private int MaxValue()
    {
        // If no digits, nothing is representable
        int number = digits != null ? digits.Count : 0;

        if (number <= 0)
        {
            return -1;
        }
        // Max is 10^number - 1 (e.g., for 3 digits: 999)
        // Clamp to int range (very high number unlikely in practice)
        double max = System.Math.Pow(10, number) - 1;

        if (max > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)max;
    }

    // Render a number across the SevenSegRenderers
    private void ForceRender(int value)
    {
        if (digits == null || digits.Count == 0)
        {
            return;
        }

        for (int i = 0; i < digits.Count; i++)
        {
            SevenSegRenderer renderer = digits[i];

            if (renderer == null)
            {
                continue;
            }

            int placeValue = Pow10(i);
            int digit = (value / placeValue) % 10;

            bool shouldShowZero = showLeadingZeros || value >= placeValue;

            if (shouldShowZero)
            {
                WriteDigit(renderer, digit);
            }
            else
            {
                // Show empty on higher places if not using leading zeros
                WriteMask(renderer, SevenSegLut.ForDigit((int)SevenSegSymbol.Empty));
            }
        }
    }

    private void WriteDigit(SevenSegRenderer renderer, int d)
    {
        byte mask = SevenSegLut.ForDigit(d);
        WriteMask(renderer, mask);
    }

    private void WriteMask(SevenSegRenderer renderer, byte mask)
    {
        renderer.map.bits = mask;
        renderer.Apply();
    }

    private IEnumerator ErrorRoutine()
    {
        _errorActive = true;

        byte err = SevenSegLut.ForDigit((int)SevenSegSymbol.Error);
        for (int i = 0; i < digits.Count; i++)
        {
            if (digits[i] != null)
            {
                WriteMask(digits[i], err);
            }
        }

        if (errorHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(errorHoldSeconds);
        }

        // After error: either reset to 0 or revert to last valid
        if (afterError == AfterErrorBehavior.ResetToZero)
        {
            _current = 0;
            _lastValid = 0;
            ForceRender(_current);
        }
        else
        {
            // Revert to _lastValid (if list changed and it no longer fits, fallback to 0)
            if (IsRepresentable(_lastValid))
            {
                _current = _lastValid;
                ForceRender(_current);
            }
            else
            {
                _current = 0;
                _lastValid = 0;
                ForceRender(_current);
            }
        }

        _errorActive = false;
        _errorCo = null;
    }

    private static int Pow10(int exp)
    {
        // quick small-table could be faster; this is fine for few digits
        int result = 1;
        for (int i = 0; i < exp; i++)
        {
            result *= 10;
        }

        return result;
    }
}
