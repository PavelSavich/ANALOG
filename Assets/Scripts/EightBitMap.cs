using System;
using UnityEngine;

[Serializable]
public class EightBitMap
{
    public string value;

    [Range(0, 127)]
    public byte bits;

    public void SetDigit(int d)
    {
        bits = SevenSegLut.ForDigit(d);
    }

    public bool IsOn(SevenSeg seg)
    {
        return (bits & (byte)seg) != 0;
    }

    public string ToBinaryString7()
    {
        return Convert.ToString(bits, 2).PadLeft(7, '0'); // A..G MSB→LSB
    }
}

