using System;
using UnityEngine;

[Serializable]
public class EightBitMap
{
    // 1) Your string value (e.g., "score", "speed", etc.)
    public string value;

    // 2) 7 lights packed into a byte (we only use bits 6..0)
    [Range(0, 127)]
    public byte bits;

    public void SetDigit(int d) => bits = SevenSegLut.ForDigit(d);

    public bool IsOn(SevenSeg seg) => (bits & (byte)seg) != 0;

    public string ToBinaryString7() =>
        Convert.ToString(bits, 2).PadLeft(7, '0'); // A..G MSB→LSB
}

