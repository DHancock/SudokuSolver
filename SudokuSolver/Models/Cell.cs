using SudokuSolver.Common;

namespace SudokuSolver.Models;

internal sealed class Cell : CellBase
{        
    public const int cMaxFormatSize = 16;

    public Cell(int index) : base(index)
    {
    }

    public Cell(Cell source) : base(source)                        
    {
    }

    public void Reset()
    {
        Origin = Origins.NotDefined;
        Value = 0;
        Possibles = BitField.AllTrue;
        HorizontalDirections = BitField.Empty;
        VerticalDirections = BitField.Empty;
    }


    public bool TryFormat(Span<char> chars, out int charsWritten)
    {
        if (HasValue)
        {
            if (chars.Length >= 2)
            {
                chars[0] = (char)('0' + Value);
                chars[1] = (char)('0' + (int)Origin);
                charsWritten = 2;
                return true;
            }
        }
        else if (chars.Length >= 6) // the minimum size required
        {
            chars[0] = '0';
            chars = chars.Slice(1);
            charsWritten = 1;

            if (Possibles.TryFormat(chars, out int written))
            {
                chars[written] = '.';
                chars = chars.Slice(written + 1);
                charsWritten += written + 1;

                if (HorizontalDirections.TryFormat(chars, out written))
                {
                    chars[written] = '.';
                    chars = chars.Slice(written + 1);
                    charsWritten += written + 1;

                    if (VerticalDirections.TryFormat(chars, out written))
                    {
                        charsWritten += written;
                        return true;
                    }
                }
            }
        }

        charsWritten = 0;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> span, ref Cell cell)             
    {
        try
        {
            if (int.TryParse(span.Slice(0, 1), out int value) && (value >= 0) && (value <= 9))
            {
                cell.Value = value;

                if (cell.HasValue)
                {
                    if (Enum.TryParse(span.Slice(1), out Origins origin) && (origin != Origins.NotDefined))
                    {
                        cell.Origin = origin;
                        return true;
                    }
                }
                else
                {
                    span = span.Slice(1);
                    int length = span.IndexOf('.');

                    if (BitField.TryParse(span.Slice(0, length), out BitField possible))
                    {
                        cell.Possibles = possible;

                        span = span.Slice(length + 1);
                        length = span.IndexOf('.');

                        if (BitField.TryParse(span.Slice(0, length), out BitField horizontal))
                        {
                            cell.HorizontalDirections = horizontal;

                            if (BitField.TryParse(span.Slice(length + 1), out BitField vertical))
                            {
                                cell.VerticalDirections = vertical;
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return false;
    }
}

