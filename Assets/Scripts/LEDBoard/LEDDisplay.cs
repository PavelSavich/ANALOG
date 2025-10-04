using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class LEDDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] 
    private RectTransform root;

    [SerializeField] 
    private GridLayoutGroup grid;

    [SerializeField] 
    private Image pixelPrefab;

    [Header("Appearance")]
    [SerializeField] 
    private Color offColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [SerializeField] 
    private Color onColor = new Color(1f, 0.15f, 0.1f, 1f);

    [Tooltip("Space between pixels (UI units).")]
    [SerializeField] 
    private Vector2 spacing = new Vector2(2, 2);

    [Tooltip("If true, cell size adapts to RectTransform.")]
    [SerializeField] 
    private bool autoFitCellSize = true;

    [SerializeField] 
    private Vector2 explicitCellSize = new Vector2(16, 16);

    private int _cols;
    private int _rows;
    private readonly List<Image> _pixels = new();

    public int Cols
    {
        get
        {
            return _cols;
        }
    }

    public int Rows
    {
        get
        {
            return _rows;
        }
    }

    public Color OnColor
    {
        get
        {
            return onColor;
        }

        set 
        { 
            onColor = value; 
        }
    }

    public Color OffColor
    {
        get
        {
            return offColor;
        }

        set
        {
            offColor = value;
        }
    }

    void Reset()
    {
        root = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
    }

    void OnValidate()
    {
        if (grid == null)
        {
            grid = GetComponent<GridLayoutGroup>();
        }

        if (root == null)
        {
            root = GetComponent<RectTransform>();
        }

        ApplyLayout();
    }

    void Update()
    {
        if (autoFitCellSize)
        {
            ApplyLayout();
        }
    }

    public void SetResolution(int cols, int rows)
    {
        _cols = Mathf.Max(1, cols);
        _rows = Mathf.Max(1, rows);

        int needed = _cols * _rows;
        
        while (_pixels.Count < needed)
        {
            Image img = Instantiate(pixelPrefab, grid.transform);
            img.color = offColor;
            _pixels.Add(img);
        }
        while (_pixels.Count > needed)
        {
            Image last = _pixels[_pixels.Count - 1];
            _pixels.RemoveAt(_pixels.Count - 1);

            if (last)
            {
                DestroyImmediate(last.gameObject);
            }
        }

        ApplyLayout();
        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < _pixels.Count; i++)
        {
            _pixels[i].color = offColor;
        }
    }

    public void Blit(bool[,] bitmap)
    {
        if (bitmap == null)
        { 
            Clear();
            return;
        }

        int cols = bitmap.GetLength(0);
        int rows = bitmap.GetLength(1);

        int width = Mathf.Min(cols, _cols);
        int height = Mathf.Min(rows, _rows);

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                int idx = y * _cols + x;

                if (idx >= _pixels.Count)
                {
                    continue;
                }

                bool on = (x < width && y < height) ? bitmap[x, y] : false;
                _pixels[idx].color = on ? onColor : offColor;
            }
        }
    }

    public void Blit(bool[,] bitmap, bool[,] caretMask, Color caretColor)
    {
        if (bitmap == null) { Clear(); return; }
        int cols = bitmap.GetLength(0);
        int rows = bitmap.GetLength(1);

        int w = Mathf.Min(cols, _cols);
        int h = Mathf.Min(rows, _rows);

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                int idx = y * _cols + x;
                if (idx >= _pixels.Count) continue;

                bool on = (x < w && y < h) ? bitmap[x, y] : false;

                // If caret mask provided and this cell is part of the caret, use caretColor.
                if (caretMask != null &&
                    x < caretMask.GetLength(0) &&
                    y < caretMask.GetLength(1) &&
                    caretMask[x, y])
                {
                    _pixels[idx].color = caretColor;
                }
                else
                {
                    _pixels[idx].color = on ? onColor : offColor;
                }
            }
        }
    }

    private void ApplyLayout()
    {
        if (grid == null || root == null)
        {
            return;
        }

        grid.spacing = spacing;

        if (autoFitCellSize)
        {
            Vector2 size = root.rect.size;
            float cellW = (size.x - spacing.x * (_cols - 1)) / Mathf.Max(1, _cols);
            float cellH = (size.y - spacing.y * (_rows - 1)) / Mathf.Max(1, _rows);
            grid.cellSize = new Vector2(Mathf.Max(1, cellW), Mathf.Max(1, cellH));
        }
        else
        {
            grid.cellSize = explicitCellSize;
        }

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, _cols);
    }
}
