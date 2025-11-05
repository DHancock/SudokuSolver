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

    public string GetEncodedString()
    {
        if (HasValue)
        {
            return string.Create(2, this, (chars, state) =>
            {
                chars[0] = (char)('0' + state.Value);
                chars[1] = (char)('0' + (int)state.Origin);
            });
        }

        int size = Possibles.DigitCount + 
                     HorizontalDirections.DigitCount + 
                     VerticalDirections.DigitCount + 3;

        return string.Create(size, this, (chars, state) =>
        {
            chars[0] = '0';
            chars = chars.Slice(1);

            Possibles.TryFormat(chars, out int charsWritten);
            chars[charsWritten] = '.';
            chars = chars.Slice(charsWritten + 1);

            HorizontalDirections.TryFormat(chars, out charsWritten);
            chars[charsWritten] = '.';
            chars = chars.Slice(charsWritten + 1);

            VerticalDirections.TryFormat(chars, out charsWritten);
        });
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

