using System;
using System.Collections.Generic;

namespace Tiisu.Mahjong.Markdown;

/// <summary>One rendered tile. <see cref="Id"/> is null for an extra blank spacer.</summary>
public readonly struct TileRun
{
    public TileRun(string? id, float extraGapBefore = 0f)
    {
        Id = id;
        ExtraGapBefore = extraGapBefore;
    }

    public string? Id { get; }

    /// <summary>Extra gap in logical pixels at 100% scale; the renderer scales it with the tile scale.</summary>
    public float ExtraGapBefore { get; }
}

/// <summary>
/// Parses the compact mahjong notation used by in-game Markdown:
/// digits followed by a suit, e.g. <c>112233m</c>, <c>123z456z</c>.
/// <c>_</c> is an extra gap; <c>c/b/a/k</c> mark chi/pon/ankan/minkan.
/// </summary>
/// <remarks>
/// The parser is intentionally a multiset parser. <c>123333p</c> means 1p + 2p + 3p×4;
/// it never guesses whether that is 12p+3333p or 123p+333p.
/// </remarks>
public static class TileNotationParser
{
    private static readonly HashSet<char> Separators = new HashSet<char> { ' ', '\t', '/', '|', '-' };

    public static bool TryParse(string? text, out IReadOnlyList<TileRun> runs)
    {
        runs = Array.Empty<TileRun>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var result = new List<TileRun>();
        var counts = new int[34];
        float pendingGap = 0f;
        bool any = false;
        int i = 0;

        while (i < text.Length)
        {
            char ch = text[i];
            if (ch == '_')
            {
                pendingGap += 2f;
                i++;
                continue;
            }

            if (Separators.Contains(ch))
            {
                i++;
                continue;
            }

            char? meld = ch == 'c' || ch == 'b' || ch == 'a' || ch == 'k' ? ch : (char?)null;
            if (meld.HasValue)
            {
                i++;
            }

            int digitStart = i;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                i++;
            }

            if (i == digitStart || i >= text.Length)
            {
                return false;
            }

            char suitChar = text[i];
            int suitIndex;
            switch (suitChar)
            {
                case 'm': suitIndex = 0; break;
                case 'p': suitIndex = 1; break;
                case 's': suitIndex = 2; break;
                case 'z': suitIndex = 3; break;
                default: return false;
            }

            i++;
            var tiles = new List<string>();
            var nums = new List<int>();
            for (int d = digitStart; d < i - 1; d++)
            {
                int num = text[d] - '0';
                if (num < 1 || num > 9 || (suitIndex == 3 && num > 7))
                {
                    return false;
                }

                int index = suitIndex * 9 + num - 1;
                if (++counts[index] > 4)
                {
                    return false;
                }

                nums.Add(num);
                tiles.Add(num.ToString() + suitChar);
            }

            if (meld.HasValue && !ValidMeld(meld.Value, suitIndex, nums))
            {
                return false;
            }

            bool first = true;
            for (int t = 0; t < tiles.Count; t++)
            {
                result.Add(new TileRun(tiles[t], first ? pendingGap : 0f));
                pendingGap = 0f;
                first = false;
                any = true;
            }
        }

        if (pendingGap > 0f)
        {
            result.Add(new TileRun(null, pendingGap));
        }

        runs = result;
        return any;
    }

    private static bool ValidMeld(char prefix, int suitIndex, IReadOnlyList<int> nums)
    {
        switch (prefix)
        {
            case 'c':
                if (nums.Count != 3 || suitIndex == 3)
                {
                    return false;
                }

                int low = Math.Min(nums[0], Math.Min(nums[1], nums[2]));
                int high = Math.Max(nums[0], Math.Max(nums[1], nums[2]));
                return high - low == 2
                    && nums[0] != nums[1]
                    && nums[1] != nums[2]
                    && nums[0] != nums[2];
            case 'b':
                return nums.Count == 3 && nums[0] == nums[1] && nums[1] == nums[2];
            case 'a':
            case 'k':
                // Docs may use a222z/k333p as a three-tile display shorthand; real kan is four tiles.
                return (nums.Count == 3 || nums.Count == 4)
                    && nums[0] == nums[1]
                    && nums[1] == nums[2]
                    && (nums.Count == 3 || nums[2] == nums[3]);
            default:
                return false;
        }
    }
}
