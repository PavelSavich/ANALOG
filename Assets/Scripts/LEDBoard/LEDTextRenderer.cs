using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScrollMode { None, Left, Right, Up, Down }

public class LEDTextRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LEDDisplay display;
    [SerializeField] private LEDFont font;

    [Header("Text")]
    [TextArea(2, 6)]
    [SerializeField] private string text = "HELLO WORLD";
    [SerializeField] private bool upperCase = true;
    [SerializeField] private bool trimWhitespace = false;

    [Header("Render Target (in pixels)")]
    [Tooltip("Display pixel width. If 0, use display current cols.")]
    [SerializeField] private int targetCols = 0;
    [Tooltip("Display pixel height. If 0, use display current rows.")]
    [SerializeField] private int targetRows = 0;

    [Header("Animation")]
    [SerializeField] private ScrollMode scroll = ScrollMode.None;
    [SerializeField] private float scrollSpeedPixelsPerSecond = 20f;
    [SerializeField] private bool flashing = false;
    [SerializeField] private float flashHz = 2f;
    [SerializeField] private int loopGapPixels = 0;

    private bool[,] _offscreen; // whole raster of the message
    private int _offW, _offH;
    private float _scrollPos; // sub-pixel scroll offset (in pixels)
    private bool _flashOn = true;
    private Coroutine _animRoutine;

    void Start()
    {
        Refresh();
        StartAnimation();
    }

    void OnValidate()
    {
        if (display == null)
        {
            display = GetComponent<LEDDisplay>();
        }
    }

    void OnEnable()
    {
        StartAnimation();
    }

    void OnDisable()
    {
        StopAnimation();
    }

    public void SetText(string value)
    {
        text = value;
        Refresh();
    }

    public void SetScroll(ScrollMode mode)
    {
        scroll = mode;
        RestartAnimation();
    }

    public void SetFlashing(bool enabled)
    {
        flashing = enabled;
        RestartAnimation();
    }

    public void Refresh()
    {
        if (display == null || font == null)
        {
            return;
        }

        string t = text ?? "";

        if (upperCase)
        {
            t = t.ToUpperInvariant();
        }

        if (trimWhitespace)
        {
            t = t.TrimEnd();
        }

        // 1) Rasterize full string to an off-screen bitmap
        RasterizeText(t, out _offscreen, out _offW, out _offH);

        // 2) Ensure the display resolution matches requested target or font height
        int cols = targetCols > 0 ? targetCols : display.Cols > 0 ? display.Cols : Mathf.Max(1, _offW);
        int rows = targetRows > 0 ? targetRows : display.Rows > 0 ? display.Rows : Mathf.Max(1, font.glyphHeight);

        display.SetResolution(cols, rows);

        // 3) Immediately draw a frame (no scroll)
        _scrollPos = 0;
        BlitWindow();
    }

    private void StartAnimation()
    {
        if (_animRoutine != null)
        {
            return;
        }

        _animRoutine = StartCoroutine(Animate());
    }

    private void StopAnimation()
    {
        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }
    }

    private void RestartAnimation()
    {
        StopAnimation();
        StartAnimation();
    }

    private IEnumerator Animate()
    {
        float flashTimer = 0f;

        while (true)
        {
            float dt = Time.deltaTime;

            // Flashing
            if (flashing && flashHz > 0f)
            {
                flashTimer += dt;
                float period = 1f / flashHz;
                if (flashTimer >= period)
                {
                    flashTimer -= period;
                    _flashOn = !_flashOn;
                    BlitWindow();
                }
            }

            // Scrolling
            if (scroll != ScrollMode.None && scrollSpeedPixelsPerSecond != 0f)
            {
                _scrollPos += scrollSpeedPixelsPerSecond * dt;

                // Seamless wrap: period = content size + optional gap
                int periodW = Mathf.Max(1, _offW + Mathf.Max(0, loopGapPixels));
                int periodH = Mathf.Max(1, _offH + Mathf.Max(0, loopGapPixels));

                switch (scroll)
                {
                    case ScrollMode.Left:
                    case ScrollMode.Right:
                        if (_offW <= display.Cols && loopGapPixels == 0) _scrollPos = 0; // nothing to scroll
                        else _scrollPos = Mathf.Repeat(_scrollPos, periodW);
                        break;
                    case ScrollMode.Up:
                    case ScrollMode.Down:
                        if (_offH <= display.Rows && loopGapPixels == 0) _scrollPos = 0;
                        else _scrollPos = Mathf.Repeat(_scrollPos, periodH);
                        break;
                }

                BlitWindow();
            }

            // If neither flash nor scroll is active, just idle cheaply.
            if (!flashing && scroll == ScrollMode.None)
            {
                yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    private void BlitWindow()
    {
        if (_offscreen == null)
        {
            return;
        }

        int Width = display.Cols;
        int Height = display.Rows;

        bool[,] window = new bool[Width, Height];

        int periodW = Mathf.Max(1, _offW + Mathf.Max(0, loopGapPixels));
        int periodH = Mathf.Max(1, _offH + Mathf.Max(0, loopGapPixels));

        int shift = Mathf.FloorToInt(_scrollPos);

        switch (scroll)
        {
            case ScrollMode.Left:
                {
                    // content moves left -> window shows increasing x from shift
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool on = SampleLoopX(x + shift, y);

                            if (_flashOn)
                            {
                                window[x, y] = on;
                            }
                            else
                            {
                                window[x, y] = false;
                            }
                        }
                    }

                    break;
                }
            case ScrollMode.Right:
                {
                    // content moves right -> start moves backwards
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool on = SampleLoopX(x - shift, y);

                            if (_flashOn)
                            {
                                window[x, y] = on;
                            }
                            else
                            {
                                window[x, y] = false;
                            }
                        }
                    }

                    break;
                }
            case ScrollMode.Up:
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool on = SampleLoopY(x, y + shift);

                            if (_flashOn)
                            {
                                window[x, y] = on;
                            }
                            else
                            {
                                window[x, y] = false;
                            }
                        }
                    }

                    break;
                }
            case ScrollMode.Down:
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool on = SampleLoopY(x, y - shift);

                            if (_flashOn)
                            {
                                window[x, y] = on;
                            }
                            else
                            {
                                window[x, y] = false;
                            }
                        }
                    }

                    break;
                }
            case ScrollMode.None:
            default:
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool on = (x < _offW && y < _offH) ? _offscreen[x, y] : false;

                            if (_flashOn)
                            {
                                window[x, y] = on;
                            }
                            else
                            {
                                window[x, y] = false;
                            }
                        }
                    }

                    break;
                }
        }

        display.Blit(window);

        // --- local helpers (looping with optional blank gap) ---
        bool SampleLoopX(int sx, int sy)
        {
            if (sy < 0 || sy >= _offH)
            {
                return false;
            }

            int mod = ((sx % periodW) + periodW) % periodW; // positive modulo

            if (mod >= _offW)
            {
                return false;                 // inside the gap
            }

            return _offscreen[mod, sy];
        }

        bool SampleLoopY(int sx, int sy)
        {
            if (sx < 0 || sx >= _offW)
            {
                return false;
            }

            int mod = ((sy % periodH) + periodH) % periodH;

            if (mod >= _offH)
            {
                return false;                 // inside the gap
            }

            return _offscreen[sx, mod];
        }
    }


    private void RasterizeText(string text, out bool[,] map, out int totalWidth, out int totalHeight)
    {
        // First pass: measure required size
        int lineWidth = 0;
        int maxWidth = 0;
        int totalGlyphHeight = font.glyphHeight;

        List<(char c, LEDFont.Glyph g)> tokens = new();

        foreach (char character in text)
        {
            if (character == '\n')
            {
                maxWidth = Mathf.Max(maxWidth, lineWidth);
                lineWidth = 0;
                totalGlyphHeight += font.glyphHeight + font.lineSpacing;
                tokens.Add(('\n', null));
                continue;
            }

            LEDFont.Glyph glyph = font.GetGlyph(character);
            if (glyph == null)
            {
                continue;
            }

            tokens.Add((character, glyph));
            lineWidth += glyph.Width + font.charSpacing;
        }

        maxWidth = Mathf.Max(maxWidth, lineWidth > 0 ? lineWidth - font.charSpacing : 0);
        totalWidth = Mathf.Max(1, maxWidth);
        totalHeight = Mathf.Max(1, totalGlyphHeight);

        // Create bitmap
        map = new bool[totalWidth, totalHeight];

        int characterX = 0;
        int characterY = 0;

        foreach ((char c, LEDFont.Glyph g) tk in tokens)
        {
            if (tk.c == '\n')
            {
                characterY += font.glyphHeight + font.lineSpacing;
                characterX = 0;

                continue;
            }

            LEDFont.Glyph g = tk.g;

            // Blit glyph at (characterX, characterY)
            for (int gy = 0; gy < g.Height; gy++)
            {
                for (int gx = 0; gx < g.Width; gx++)
                {
                    bool on = g.Get(gx, gy);
                    int px = characterX + gx;
                    int py = characterY + gy;

                    if (px >= 0 && px < totalWidth && py >= 0 && py < totalHeight)
                    {
                        map[px, py] = on || map[px, py];
                    }
                }
            }

            characterX += g.Width + font.charSpacing;
        }
    }
}
