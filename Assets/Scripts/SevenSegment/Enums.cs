using System;

[Flags]
public enum SevenSeg : byte
{
    A = 1 << 6, // MSB
    B = 1 << 5,
    C = 1 << 4,
    D = 1 << 3,
    E = 1 << 2,
    F = 1 << 1,
    G = 1 << 0, // LSB
}

public enum SevenSegSymbol
{
    Empty = 10,
    Error = 11,
    LoadingA = 12,
    LoadingB = 13,
    LoadingC = 14,
    LoadingD = 15,
    LoadingE = 16,
    LoadingF = 17,
    LoadingG = 18
}

public enum AfterErrorBehavior 
{
    ResetToZero,
    RevertToLastValid 
}

