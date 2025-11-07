using SudokuSolver.Common;

namespace SudokuSolver.Models;

internal sealed class Cell : CellBase
{        
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
        else if (chars.Length >= 6)
        {
            chars[0] = '0';
            int total = 1;

            if (Possibles.TryFormat(chars.Slice(total), out int written))
            {
                total += written;
                chars[total++] = '.';
                
                if (HorizontalDirections.TryFormat(chars.Slice(total), out written))
                {
                    total += written;
                    chars[total++] = '.';

                    if (VerticalDirections.TryFormat(chars.Slice(total), out written))
                    {
                        charsWritten = total + written;
                        return true;
                    }
                }
            }
        }

        charsWritten = 0;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> span, ref readonly Cell cell)             
    {
        try
        {
            if (int.TryParse(span.Slice(0, 1), out cell.Value) && (cell.Value >= 0) && (cell.Value <= 9))
            {
                if (cell.HasValue)
                {
                    return Enum.TryParse(span.Slice(1), out cell.Origin) && (cell.Origin != Origins.NotDefined);
                }
                
                span = span.Slice(1);
                int length = span.IndexOf('.');

                if (BitField.TryParse(span.Slice(0, length), out cell.Possibles))
                {
                    span = span.Slice(length + 1);
                    length = span.IndexOf('.');

                    return BitField.TryParse(span.Slice(0, length), out cell.HorizontalDirections) &&
                            BitField.TryParse(span.Slice(length + 1), out cell.VerticalDirections);
                }
            }
        }
        catch
        {
        }

        return false;
    }
}

