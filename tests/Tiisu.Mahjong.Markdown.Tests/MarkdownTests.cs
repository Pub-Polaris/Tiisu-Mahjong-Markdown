using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tiisu.Mahjong.Markdown.Tests;

public sealed class MarkdownTests
{
    [Fact]
    public void InlineCode_SurvivesAsCodeSegment()
    {
        var blocks = MarkdownDocument.Parse("牌型：`123z456z`");
        var paragraph = Assert.Single(blocks.Where(b => b.Kind == MarkdownBlockKind.Paragraph));
        Assert.NotNull(paragraph.Inlines);
        Assert.Contains(paragraph.Inlines!, p => p.Kind == MarkdownInlineKind.Code && p.Text == "123z456z");
    }

    [Fact]
    public void CompactNotation_Expands()
    {
        Assert.True(TileNotationParser.TryParse("123z456z", out var runs));
        Assert.Equal(new[] { "1z", "2z", "3z", "4z", "5z", "6z" },
            runs.Where(r => r.Id != null).Select(r => r.Id!));
    }

    [Fact]
    public void Underscore_IsExtraGap()
    {
        Assert.True(TileNotationParser.TryParse("11m_22m", out var runs));
        var tiles = runs.Where(r => r.Id != null).ToArray();
        Assert.Equal(4, tiles.Length);
        Assert.Equal(2f, tiles[2].ExtraGapBefore);
    }

    [Fact]
    public void AmbiguousShape_IsOnlyMultiset()
    {
        Assert.True(TileNotationParser.TryParse("123333p", out var runs));
        Assert.Equal(6, runs.Count(r => r.Id != null));
        Assert.Equal(4, runs.Count(r => r.Id == "3p"));
    }

    [Fact]
    public void Invalid_KanWithoutSuit_IsRejected()
    {
        Assert.False(TileNotationParser.TryParse("k333", out _));
    }

    [Fact]
    public void Renderer_DrawsInlineTileCommands()
    {
        var canvas = new FakeCanvas();
        var renderer = new TileMarkdownRenderer();
        var height = renderer.Render(canvas, "牌型：`123z456z`", 0, 0, 800);

        Assert.True(height > 0);
        Assert.Contains(canvas.Tiles, t => t == "1z");
        Assert.Contains(canvas.Tiles, t => t == "6z");
    }

    private sealed class FakeCanvas : IMarkdownCanvas
    {
        public List<string> Tiles { get; } = new List<string>();
        public List<string> Texts { get; } = new List<string>();

        public float MeasureText(string text, MarkdownTextStyle style) => text.Length * style.Size * 0.5f;

        public void DrawText(string text, MarkdownRect rect, MarkdownTextStyle style) => Texts.Add(text);

        public void DrawTile(string tileId, MarkdownRect rect) => Tiles.Add(tileId);
    }
}
