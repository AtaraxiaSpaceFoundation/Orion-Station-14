namespace Content.Shared._Orion.Localizations;

public static class RomanNumerals
{
    private static readonly (int Value, string Symbol)[] Symbols =
    [
        (1000, "M"),
        (900, "CM"),
        (500, "D"),
        (400, "CD"),
        (100, "C"),
        (90, "XC"),
        (50, "L"),
        (40, "XL"),
        (10, "X"),
        (9, "IX"),
        (5, "V"),
        (4, "IV"),
        (1, "I"),
    ];

    public static string ToRoman(int number)
    {
        if (number <= 0)
            return string.Empty;

        var remaining = number;
        var result = string.Empty;

        foreach (var (value, symbol) in Symbols)
        {
            while (remaining >= value)
            {
                result += symbol;
                remaining -= value;
            }
        }

        return result;
    }
}
