using System;
using System.Collections.Generic;

namespace Tiisu.Mahjong.Markdown;

/// <summary>Minimal color type so the renderer stays independent of Skia, Unity and Godot.</summary>
public readonly struct MarkdownColor
{
    public MarkdownColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public static MarkdownColor White => new MarkdownColor(255, 255, 255);
    public static MarkdownColor Transparent => new MarkdownColor(0, 0, 0, 0);
}

public readonly struct MarkdownTextStyle
{
    public MarkdownTextStyle(float size, bool bold = false, MarkdownColor? color = null)
    {
        Size = size;
        Bold = bold;
        Color = color ?? MarkdownColor.White;
    }

    public float Size { get; }
    public bool Bold { get; }
    public MarkdownColor Color { get; }
}

public readonly struct MarkdownRect
{
    public MarkdownRect(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public float X { get; }
    public float Y { get; }
    public float W { get; }
    public float H { get; }
    public float Right => X + W;
    public float Bottom => Y + H;
}

/// <summary>
/// Host rendering adapter. Unity/Godot adapters only need to implement text measurement,
/// text drawing and tile-atlas drawing.
/// </summary>
public interface IMarkdownCanvas
{
    float MeasureText(string text, MarkdownTextStyle style);
    void DrawText(string text, MarkdownRect rect, MarkdownTextStyle style);
    void DrawTile(string tileId, MarkdownRect rect);
}

/// <summary>Renderer settings; defaults match the in-game 50% tile demo.</summary>
public sealed class TileMarkdownOptions
{
    public float BaseTileWidth { get; set; } = 60f;
    public float BaseTileHeight { get; set; } = 82f;
    public float BaseTileGap { get; set; } = 3.6f;
    public float TileScale { get; set; } = 0.5f;
    public float TextSize { get; set; } = 16f;
    public float HeadingGap { get; set; } = 8f;
}

/// <summary>
/// Portable renderer for the Markdown subset plus inline mahjong tile notation.
/// It draws by issuing text/tile commands; the host owns fonts, colors and tile textures.
/// </summary>
public sealed class TileMarkdownRenderer
{
    private readonly TileMarkdownOptions _options;

    public TileMarkdownRenderer(TileMarkdownOptions? options = null)
    {
        _options = options ?? new TileMarkdownOptions();
    }

    public float TileWidth => _options.BaseTileWidth * _options.TileScale;
    public float TileHeight => _options.BaseTileHeight * _options.TileScale;
    public float TileGap => _options.BaseTileGap * _options.TileScale;

    /// <summary>Draws parsed Markdown starting at (x, y); returns the content height.</summary>
    public float Render(IMarkdownCanvas canvas, string markdown, float x, float y, float width)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        IReadOnlyList<MarkdownBlock> blocks = MarkdownDocument.Parse(markdown);
        float cursor = y;
        var text = new MarkdownTextStyle(_options.TextSize, false, new MarkdownColor(220, 228, 220));
        var heading = new MarkdownTextStyle(24f, true, new MarkdownColor(242, 201, 76));
        var muted = new MarkdownTextStyle(15f, false, new MarkdownColor(159, 180, 170));

        foreach (MarkdownBlock block in blocks)
        {
            switch (block.Kind)
            {
                case MarkdownBlockKind.Heading:
                    cursor = DrawInlineBlock(canvas, block, x, cursor, width, heading, string.Empty) + _options.HeadingGap;
                    break;
                case MarkdownBlockKind.Paragraph:
                    cursor = DrawInlineBlock(canvas, block, x, cursor, width, text, string.Empty) + 8f;
                    break;
                case MarkdownBlockKind.Bullet:
                    cursor = DrawInlineBlock(canvas, block, x, cursor, width, text, "· ") + 4f;
                    break;
                case MarkdownBlockKind.Quote:
                    cursor = DrawInlineBlock(canvas, block, x, cursor, width, muted, "│ ") + 6f;
                    break;
                case MarkdownBlockKind.Code:
                    cursor = DrawCode(canvas, block.Text, x, cursor, width) + 8f;
                    break;
                case MarkdownBlockKind.Table:
                    cursor = DrawTextLines(canvas, block.Text, x, cursor, width, text) + 3f;
                    break;
                case MarkdownBlockKind.Rule:
                    cursor += 8f;
                    break;
            }
        }

        return cursor - y;
    }

    private float DrawInlineBlock(IMarkdownCanvas canvas, MarkdownBlock block, float x, float y, float width,
        MarkdownTextStyle style, string prefix)
    {
        IReadOnlyList<MarkdownInline>? inlines = block.Inlines;
        if (inlines == null || inlines.Count == 0)
        {
            return DrawTextLines(canvas, prefix + block.Text, x, y, width, style);
        }

        float cursorX = x;
        if (prefix.Length > 0)
        {
            canvas.DrawText(prefix, new MarkdownRect(cursorX, y, width, style.Size + 4f), style);
            cursorX += canvas.MeasureText(prefix, style);
        }

        bool first = true;
        foreach (MarkdownInline inline in inlines)
        {
            if (inline.Kind == MarkdownInlineKind.Code
                && TileNotationParser.TryParse(inline.Text, out IReadOnlyList<TileRun> runs))
            {
                foreach (TileRun run in runs)
                {
                    if (run.Id != null)
                    {
                        if (!first)
                        {
                            cursorX += TileGap;
                        }

                        cursorX += run.ExtraGapBefore * _options.TileScale;
                        canvas.DrawTile(run.Id, new MarkdownRect(cursorX, y, TileWidth, TileHeight));
                        cursorX += TileWidth;
                        first = false;
                    }
                    else
                    {
                        cursorX += run.ExtraGapBefore * _options.TileScale;
                    }
                }
            }
            else if (inline.Text.Length > 0)
            {
                if (!first)
                {
                    cursorX += 4f;
                }

                float w = canvas.MeasureText(inline.Text, style);
                canvas.DrawText(inline.Text,
                    new MarkdownRect(cursorX, y + Math.Max(0f, TileHeight - style.Size) / 2f, w, style.Size + 4f),
                    style);
                cursorX += w;
                first = false;
            }
        }

        return y + Math.Max(TileHeight, style.Size + 4f);
    }

    private float DrawTextLines(IMarkdownCanvas canvas, string text, float x, float y, float width,
        MarkdownTextStyle style)
    {
        float cursor = y;
        foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            canvas.DrawText(line, new MarkdownRect(x, cursor, width, style.Size + 4f), style);
            cursor += style.Size + 6f;
        }

        return cursor;
    }

    private float DrawCode(IMarkdownCanvas canvas, string text, float x, float y, float width)
    {
        var codeStyle = new MarkdownTextStyle(13f, false, new MarkdownColor(196, 232, 190));
        float cursor = y;
        foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            canvas.DrawText(line, new MarkdownRect(x, cursor, width, 19f), codeStyle);
            cursor += 19f;
        }

        return cursor;
    }
}
