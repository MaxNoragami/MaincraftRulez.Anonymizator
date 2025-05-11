using System;
using static MaincraftRulez.Anonymizator.Constants;

namespace MaincraftRulez.Anonymizator;

public static class AlphabetUtil
{
    public static string GetDefaultAlphabet(int radix)
    {
        if (radix <= 0)
            throw new ArgumentException("Radix must be positive");
            
        if (radix <= 10)
            return Alphabets.Digits.Substring(0, radix);
            
        if (radix <= 36)
            return Alphabets.Digits + Alphabets.LowerAlpha.Substring(0, radix - 10);
            
        if (radix <= 62)
            return Alphabets.Digits + Alphabets.LowerAlpha + 
                    Alphabets.UpperAlpha.Substring(0, radix - 36);
            
        throw new ArgumentException($"Radix {radix} > 62 requires custom alphabet");
    }
}