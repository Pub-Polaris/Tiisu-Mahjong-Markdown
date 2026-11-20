# Tiisu Mahjong Markdown

Portable C# Markdown + mahjong tile notation renderer used by **Tiisu Mahjong**.

It is designed for C# hosts such as **Unity**, **Godot**, and desktop frameworks. The library does not bundle fonts or tile textures; the host supplies text measurement, text drawing, and a tile atlas by implementing `IMarkdownCanvas`.

## What it renders

- fenced code, headings, paragraphs, bullets, quotes, tables
- inline code spans
- mahjong tile notation inside inline code spans

```markdown
牌型：`123z456z`
七对：`11223344556677z`
空位：`11m_22m_33m`
吃碰：`c123m` `b777z`
暗杠明杠：`a222z` `k333p`
```

Tile notation is a multiset notation:

```text
112233m = 1m 1m 2m 2m 3m 3m
123456789m = 123m + 456m + 789m
123333p = 1p + 2p + 3p + 3p + 3p + 3p
```

`123333p` does **not** imply `12p+3333p` or `123p+333p`; a real scoring engine decides the hand decomposition.

## Install

Copy the `src/Tiisu.Mahjong.Markdown` folder into your C# project, or reference the project.

Target: `netstandard2.1`.

## Host adapter

```csharp
public sealed class MyCanvas : IMarkdownCanvas
{
    public float MeasureText(string text, MarkdownTextStyle style) => /* engine text measure */;
    public void DrawText(string text, MarkdownRect rect, MarkdownTextStyle style) => /* engine text draw */;
    public void DrawTile(string tileId, MarkdownRect rect) => /* draw tile sprite by id */;
}

var renderer = new TileMarkdownRenderer();
var height = renderer.Render(canvas, markdown, x: 40, y: 32, width: 720);
```

`tileId` uses the compact ids `1m`, `2p`, `7z`, etc. The host chooses the atlas, texture, scale and color style.

Default demo scale is 50% of the in-game hand tile size:

```text
tile: 30 × 41
normal gap: 1.8
'_' extra gap: 1
```

Change with `TileMarkdownOptions`.

## Unity notes

- Implement `IMarkdownCanvas` with `TextMeshPro` measurement/drawing.
- Map `tileId` to your sprite atlas.
- The library has no Unity dependency; it is plain C#.

## Godot notes

- Implement `IMarkdownCanvas` with `Label`/`Font` measurement and `Sprite2D`/`TextureRect` drawing.
- Map `tileId` to your atlas.
- The library has no Godot dependency.

## Notation reference

### Suits

```text
m = 万
p = 筒
s = 索
z = 字（1..7）
```

A suit letter applies to all digits since the previous suit letter.

### Spacing

- normal tile gap: 1.8px at default 50% scale
- `_`: one extra 2px blank at 100% scale (1px at 50%)
- spaces, `/`, `|`, `-` are reading separators and do not add tiles

### Melds

```text
c123m   chi
b777z   pon
a222z   ankan (three-tile display shorthand)
k333p   minkan (three-tile display shorthand)
```

Canonical four-tile forms `a2222z` / `k3333p` are also accepted.

Missing suit is rejected: `c123`, `k333` cannot be parsed without external context.

## NuGet publishing

This repository publishes through NuGet.org **trusted publishing** (OIDC), not a long-lived API key.

- workflow: `.github/workflows/publish.yml`
- NuGet.org trusted policy:
  - repository owner: `Pub-Polaris`
  - repository: `Tiisu-Mahjong-Markdown`
  - workflow file: `publish.yml`
  - environment: leave empty
- GitHub repository secret: `NUGET_USER` = NuGet.org username (not email)
- package version is defined in `src/Tiisu.Mahjong.Markdown/Tiisu.Mahjong.Markdown.csproj`

The workflow runs on a published release or by manual `workflow_dispatch`.

## Attribution

The renderer and notation rules are derived from the in-game Markdown renderer in
[Tiisu Mahjong](https://github.com/Pub-Polaris/Tiisu-Mahjong-CSharp).

Original author: **公民北极星_Official**.  
License: MIT; retain the copyright notice when redistributing.

Third-party assets/fonts are not included.
