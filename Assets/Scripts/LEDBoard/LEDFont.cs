using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLEDFont", menuName = "LED Board/LEDFont")]
public class LEDFont : ScriptableObject
{
    [Serializable]
    public class Glyph
    {
        public string name = "A";
        public char character = 'A';
        [TextArea(1, 20)]
        public string[] rows; // top->bottom, '.' = off, '#' = on

        public bool Get(int x, int y)
        {
            if (rows == null || y < 0 || y >= rows.Length)
            {
                return false;
            }

            string line = rows[y];

            return x >= 0 && x < line.Length && line[x] == '#';
        }

        public int Width
        {
            get
            {
                if (rows != null && rows.Length > 0)
                {
                    return rows[0].Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int Height
        {
            get
            {
                if (rows != null)
                {
                    return rows.Length;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    [Header("Glyph metrics")]
    [Tooltip("Nominal width of glyph bitmaps (columns). You can still store variable widths per glyph; this is used for layout defaults.")]
    public int glyphWidth = 5;

    [Tooltip("Nominal height of glyph bitmaps (rows). Should match actual rows length in your glyphs.")]
    public int glyphHeight = 7;

    [Header("Layout")]
    [Tooltip("Columns between characters (in pixels).")]
    public int charSpacing = 1;

    [Tooltip("Rows between lines (in pixels).")]
    public int lineSpacing = 1;

    [Header("Glyphs")]
    public List<Glyph> glyphs = new List<Glyph>();

    [Header("Fallback")]
    [Tooltip("Used for undefined characters.")]
    public Glyph undefinedGlyph;

    private Dictionary<char, Glyph> _map;

    void OnEnable()
    {
        BuildMap();
    }

    public void BuildMap()
    {
        _map = new Dictionary<char, Glyph>();

        if (glyphs != null)
        {
            foreach (Glyph glyph in glyphs)
            {
                if (!_map.ContainsKey(glyph.character))
                {
                    _map.Add(glyph.character, glyph);
                }
            }
        }
    }

    public Glyph GetGlyph(char c)
    {
        if (_map == null || _map.Count == 0)
        {
            BuildMap();
        }

        if (_map.TryGetValue(c, out var g))
        {
            return g;
        }

        if (undefinedGlyph != null)
        {
            return undefinedGlyph;
        }
        else
        {
            return null;
        }
    }
}
