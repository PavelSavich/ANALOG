using System.Collections.Generic;
using UnityEngine;

public static class SevenSegLut
{
    // A B C D E F G (MSB→LSB)
    public static readonly Dictionary<int, byte> DigitMasks = new Dictionary<int, byte>
    {
        { 0,  0b111_1110 },
        { 1,  0b011_0000 },
        { 2,  0b110_1101 },
        { 3,  0b111_1001 },
        { 4,  0b011_0011 },
        { 5,  0b101_1011 },
        { 6,  0b101_1111 },
        { 7,  0b111_0000 },
        { 8,  0b111_1111 },
        { 9,  0b111_1011 },

        // Special symbols
        { (int)SevenSegSymbol.Empty,    0b000_0000 },
        { (int)SevenSegSymbol.Error,    0b110_1101 },

        { (int)SevenSegSymbol.LoadingA, 0b100_0000 }, // A
        { (int)SevenSegSymbol.LoadingB, 0b010_0000 }, // B
        { (int)SevenSegSymbol.LoadingC, 0b001_0000 }, // C
        { (int)SevenSegSymbol.LoadingD, 0b000_1000 }, // D
        { (int)SevenSegSymbol.LoadingE, 0b000_0100 }, // E
        { (int)SevenSegSymbol.LoadingF, 0b000_0010 }, // F
        { (int)SevenSegSymbol.LoadingG, 0b000_0001 } // G
    };

    public static byte ForDigit(int d)
    {
        return DigitMasks.TryGetValue(d, out var mask) ? mask : (byte)0;
    }
}
