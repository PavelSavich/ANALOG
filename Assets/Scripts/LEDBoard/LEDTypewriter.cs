using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDTypewriter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private LEDDisplay display;

    [SerializeField] 
    private LEDFont font;

    [Header("Text")]
    [TextArea(2, 6)]
    [SerializeField] 
    private string text = "Hello world!";

    [SerializeField] 
    private bool upperCase = true;

    [SerializeField]
    private bool trimWhitespaceEnd = true;

    [Header("Render Target (pixels)")]
    [Tooltip("If 0, uses display current cols/rows. Set explicitly if you resize the display later.")]
    [SerializeField]
    private int targetCols = 0;

    [SerializeField] 
    private int targetRows = 0;

    [Header("Typewriter")]
    [SerializeField, Min(0.01f)]
    private float charsPerSecond = 30f;

    [Tooltip("Extra pause (sec) when revealing '.', '!', '?'")]
    [SerializeField]
    private float punctuationPauseSeconds = 0.15f;

    [Tooltip("Optional pause on comma/semicolon/colon")]
    [SerializeField] 
    private float lightPunctuationPauseSeconds = 0.07f;

    [Tooltip("Auto start typing on OnEnable")]
    [SerializeField] 
    private bool autoStart = true;

    [Header("Caret")]
    [SerializeField] 
    private bool showCaretWhileTyping = true;

    [SerializeField] 
    private float caretBlinkHz = 2f;

    [SerializeField] 
    private int caretThicknessRows = 1;

    [SerializeField] 
    private int caretInsetRows = 0;

    [SerializeField] 
    private Color caretColor = new Color(0.15f, 0.9f, 0.2f, 1f);

    [Header("On Complete")]
    [SerializeField] private CompletionMode onComplete = CompletionMode.Stop;
    [SerializeField] private float completeDelaySeconds = 2f;


    private bool _caretOn = true;
    private float _caretTimer = 0f;


    public System.Action<char, int> OnCharPrinted;
    public System.Action OnTypingFinished;

    private string _source;
    private int _visibleChars = 0;
    private Coroutine _typingRoutine;

    private List<GlyphToken> _linear;
    private List<LineLayout> _lines;
    private int _offW, _offH;

    public enum CompletionMode { Stop, ClearThenStop, RestartFromBeginning }

    private struct GlyphToken
    {
        public char c;
        public LEDFont.Glyph g;
        public int width;
        public bool isNewline;
        public bool isSpace;
    }

    private class LineLayout
    {
        public List<int> tokenIndices = new();
        public int y;
        public int widthPixels;
    }

    void Reset()
    {
        if (!display)
        {
            display = GetComponent<LEDDisplay>();
        }
    }

    void OnEnable()
    {
        if (autoStart)
        {
            Prepare(text);
            StartTyping();
        }
    }

    void OnDisable()
    {
        StopTyping();
    }

    // ------------------- Public API -------------------

    public void SetText(string t, bool startImmediately = true)
    {
        Prepare(t);
        if (startImmediately)
        {
            StartTyping();
        }
        else
        {
            RenderPrefix(0);
        }
    }

    public void StartTyping()
    {
        StopTyping();
        _visibleChars = 0;
        _typingRoutine = StartCoroutine(TypeRoutine());
    }

    public void StopTyping()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }
    }

    public void SkipToEnd()
    {
        StopTyping();
        _visibleChars = _linear.Count;
        RenderPrefix(_visibleChars, drawCaret: false);
        HandleCompletion();
    }


    public void SetSpeed(float cps)
    {
        charsPerSecond = Mathf.Max(0.01f, cps);
    }

    // ------------------- Core -------------------

    private void Prepare(string raw)
    {
        if (display == null || font == null)
        {
            return;
        }

        _source = raw ?? string.Empty;
        if (upperCase)
        {
            _source = _source.ToUpperInvariant();
        }

        if (trimWhitespaceEnd)
        {
            _source = _source.TrimEnd();
        }

        _linear = new List<GlyphToken>(_source.Length);
        foreach (char c in _source)
        {
            if (c == '\n')
            {
                _linear.Add(new GlyphToken { c = c, g = null, width = 0, isNewline = true, isSpace = false });
                continue;
            }

            LEDFont.Glyph g = font.GetGlyph(c);
            int w = g != null ? g.Width : font.glyphWidth;
            _linear.Add(new GlyphToken
            {
                c = c,
                g = g,
                width = w,
                isNewline = false,
                isSpace = (c == ' ')
            });
        }

        int cols = targetCols > 0 ? targetCols : display.Cols > 0 ? display.Cols : Mathf.Max(1, font.glyphWidth);
        int rows = targetRows > 0 ? targetRows : display.Rows > 0 ? display.Rows : Mathf.Max(1, font.glyphHeight);
        display.SetResolution(cols, rows);

        BuildWrappedLayout(cols, rows, out _offW, out _offH);

        _visibleChars = 0;
        RenderPrefix(_visibleChars);
    }

    private IEnumerator TypeRoutine()
    {
        float acc = 0f;
        _caretOn = true;
        _caretTimer = 0f;

        while (_visibleChars < _linear.Count)
        {
            float dt = Time.deltaTime;
            acc += dt * charsPerSecond;

            while (acc >= 1f && _visibleChars < _linear.Count)
            {
                acc -= 1f;
                _visibleChars++;

                char printedChar = _linear[Mathf.Clamp(_visibleChars - 1, 0, _linear.Count - 1)].c;
                OnCharPrinted?.Invoke(printedChar, _visibleChars - 1);

                RenderPrefix(_visibleChars, drawCaret: showCaretWhileTyping && _visibleChars < _linear.Count);

                if (printedChar == '.' || printedChar == '!' || printedChar == '?')
                {
                    if (punctuationPauseSeconds > 0f)
                    {
                        yield return new WaitForSeconds(punctuationPauseSeconds);
                    }
                }
                else if (printedChar == ',' || printedChar == ';' || printedChar == ':')
                {
                    if (lightPunctuationPauseSeconds > 0f)
                    {
                        yield return new WaitForSeconds(lightPunctuationPauseSeconds);
                    }
                }
            }

            if (showCaretWhileTyping && caretBlinkHz > 0f && _visibleChars < _linear.Count)
            {
                _caretTimer += dt;
                float period = 1f / caretBlinkHz;
                if (_caretTimer >= period)
                {
                    _caretTimer -= period;
                    _caretOn = !_caretOn;

                    RenderPrefix(_visibleChars, drawCaret: true);
                }
            }

            yield return null;
        }

        _typingRoutine = null;

        HandleCompletion();
    }


    private void BuildWrappedLayout(int maxWidthPixels, int maxHeightPixels, out int offW, out int offH)
    {
        _lines = new List<LineLayout>();
        int y = 0;
        int lineH = font.glyphHeight;
        int totalH = lineH;

        int i = 0;
        while (i < _linear.Count)
        {
            if (_linear[i].isNewline)
            {
                _lines.Add(new LineLayout { y = y });
                y += lineH + font.lineSpacing;
                totalH = y + lineH;
                i++;
                continue;
            }

            LineLayout line = new LineLayout 
            {
                y = y, 
                widthPixels = 0 
            };

            int x = 0;

            while (i < _linear.Count && !_linear[i].isNewline)
            {
                int wordStart = i;
                int wordPixels = 0;
                int trailSpaces = 0;

                while (i < _linear.Count && !_linear[i].isNewline && !_linear[i].isSpace)
                {
                    wordPixels += _linear[i].width;
                    i++;

                    if (i < _linear.Count && !_linear[i].isNewline && !_linear[i].isSpace)
                    {
                        wordPixels += font.charSpacing;
                    }
                }

                int spacesStart = i;
                while (i < _linear.Count && _linear[i].isSpace)
                {
                    trailSpaces++;
                    wordPixels += _linear[i].width;
                    
                    if (i + 1 < _linear.Count && _linear[i + 1].isSpace)
                    {
                        wordPixels += font.charSpacing;
                    }

                    i++;
                }

                bool isFirstOnLine = line.tokenIndices.Count == 0;

                if (!isFirstOnLine && x + wordPixels > maxWidthPixels)
                {
                    _lines.Add(line);
                    y += lineH + font.lineSpacing;
                    totalH = y + lineH;

                    line = new LineLayout { y = y, widthPixels = 0 };
                    x = 0;

                    int k = wordStart;
                    while (k < spacesStart && k < _linear.Count)
                    {
                        line.tokenIndices.Add(k);
                        if (_linear[k].g != null)
                        {
                            x += _linear[k].width;

                            if (k + 1 < spacesStart)
                            {
                                x += font.charSpacing;
                            }
                        }
                        k++;
                    }
                }
                else
                {
                    for (int k = wordStart; k < i; k++)
                    {
                        if (_linear[k].isSpace && line.tokenIndices.Count == 0)
                        {
                            continue;
                        }

                        line.tokenIndices.Add(k);

                        if (_linear[k].g != null)
                        {
                            x += _linear[k].width;

                            if (k + 1 < _linear.Count
                                && !_linear[k + 1].isNewline
                                && !(k + 1 == i && trailSpaces > 0))
                            {
                                x += font.charSpacing;
                            }
                        }
                    }
                }
            }

            _lines.Add(line);
            y += lineH + font.lineSpacing;
            totalH = y + lineH;

            if (i < _linear.Count && _linear[i].isNewline)
            {
                i++;
            }
        }

        offW = maxWidthPixels;
        offH = Mathf.Max(font.glyphHeight, totalH - font.lineSpacing);
    }

    private void RenderPrefix(int visibleChars, bool drawCaret = false)
    {
        if (display == null || font == null || _lines == null)
        {
            return;
        }

        int W = display.Cols;
        int H = display.Rows;

        bool[,] window = new bool[W, H];

        int shown = 0;
        foreach (LineLayout line in _lines)
        {
            int x = 0;
            int yTop = line.y;

            foreach (int idx in line.tokenIndices)
            {
                if (shown >= visibleChars)
                {
                    goto AFTER_DRAW;
                }

                GlyphToken tk = _linear[idx];

                if (tk.isNewline)
                {
                    continue;
                }

                if (tk.g != null)
                {
                    for (int gy = 0; gy < tk.g.Height; gy++)
                    {
                        int py = yTop + gy;
                        if (py < 0 || py >= H)
                        {
                            continue;
                        }

                        var row = tk.g.rows[gy];
                        for (int gx = 0; gx < tk.g.Width; gx++)
                        {
                            int px = x + gx;
                            if (px < 0 || px >= W)
                            {
                                continue;
                            }

                            if (row[gx] == '#')
                            {
                                window[px, py] = true;
                            }
                        }
                    }

                    x += tk.g.Width;
                    x += font.charSpacing;
                }

                shown++;
                if (shown >= visibleChars)
                {
                    goto AFTER_DRAW;
                }
            }
        }

    AFTER_DRAW:

        bool[,] caretMask = null;

        if (drawCaret && _caretOn && visibleChars < _linear.Count)
        {
            if (TryGetCursorXY(visibleChars, out int cx, out int cy))
            {
                caretMask = new bool[W, H];

                int caretW = font.glyphWidth;
                LEDFont.Glyph nextGlyph = _linear[visibleChars].g;

                if (nextGlyph != null)
                {
                    caretW = nextGlyph.Width;
                }

                int thickness = Mathf.Clamp(caretThicknessRows, 1, 3);
                int baselineY = cy + font.glyphHeight - 1 - Mathf.Max(0, caretInsetRows);

                for (int t = 0; t < thickness; t++)
                {
                    int py = baselineY - t;

                    if (py < 0 || py >= H)
                    {
                        continue;
                    }

                    for (int gx = 0; gx < caretW; gx++)
                    {
                        int px = cx + gx;

                        if (px < 0 || px >= W)
                        {
                            continue;
                        }

                        caretMask[px, py] = true;
                    }
                }
            }
        }

        if (caretMask != null)
        {
            display.Blit(window, caretMask, caretColor);
        }
        else
        {
            display.Blit(window);
        }
    }


    private bool TryGetCursorXY(int nextIndex, out int outX, out int outY)
    {
        outX = 0; outY = 0;
        int count = 0;

        foreach (LineLayout line in _lines)
        {
            int x = 0;
            int yTop = line.y;

            foreach (int idx in line.tokenIndices)
            {
                if (count == nextIndex)
                {
                    outX = x;
                    outY = yTop;
                    return true;
                }

                GlyphToken tk = _linear[idx];

                if (tk.g != null)
                {
                    x += tk.g.Width;
                    x += font.charSpacing;
                }
                count++;
            }

            if (count == nextIndex)
            {
                outX = x;
                outY = yTop;
                return true;
            }
        }

        return false;
    }

    private void HandleCompletion()
    {
        RenderPrefix(_linear.Count, drawCaret: false);

        switch (onComplete)
        {
            case CompletionMode.Stop:
                OnTypingFinished?.Invoke();
                break;

            case CompletionMode.ClearThenStop:
                StartCoroutine(CompleteClearRoutine());
                break;

            case CompletionMode.RestartFromBeginning:
                StartCoroutine(CompleteRestartRoutine());
                break;
        }
    }

    private IEnumerator CompleteClearRoutine()
    {
        if (completeDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(completeDelaySeconds);
        }

        display?.Clear();
        OnTypingFinished?.Invoke();
    }

    private IEnumerator CompleteRestartRoutine()
    {
        if (completeDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(completeDelaySeconds);
        }

        Prepare(_source);
        StartTyping();
    }


}
