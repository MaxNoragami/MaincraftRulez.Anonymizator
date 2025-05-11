using System;
using System.Numerics;
using System.Text;

namespace MaincraftRulez.Anonymizator;

public static class IntegerUtil
{
    public static BigInteger DecodeRadix(string str, string alphabet)
    {
        BigInteger result = 0;
        int radix = alphabet.Length;
        
        foreach (char c in str)
        {
            int index = alphabet.IndexOf(c);
            if (index == -1)
                throw new ArgumentException($"Character '{c}' not in alphabet");
                
            result = result * radix + index;
        }
        
        return result;
    }
    
    public static string EncodeRadix(BigInteger value, string alphabet, int length = 0)
    {
        int radix = alphabet.Length;
        StringBuilder result = new StringBuilder();
        
        // Ensure value is non-negative
        value = BigInteger.Abs(value);
        
        // Handle zero case explicitly
        if (value == 0)
        {
            result.Append(alphabet[0]);
        }
        else
        {
            while (value > 0)
            {
                BigInteger remainder;
                value = BigInteger.DivRem(value, radix, out remainder);
                
                // Ensure remainder is a valid index
                int index = (int)remainder;
                if (index < 0 || index >= alphabet.Length)
                {
                    index = Math.Abs(index) % alphabet.Length;
                }
                
                result.Insert(0, alphabet[index]);
            }
        }
        
        // Pad with leading characters if needed
        if (length > 0 && result.Length < length)
        {
            result.Insert(0, new string(alphabet[0], length - result.Length));
        }
        
        return result.ToString();
    }
    
    public static BigInteger AddMod(BigInteger a, BigInteger b, BigInteger modulus)
    {
        return ((a + b) % modulus + modulus) % modulus; // Ensure result is positive
    }
    
    public static BigInteger SubMod(BigInteger a, BigInteger b, BigInteger modulus)
    {
        return ((a - b) % modulus + modulus) % modulus; // Ensure result is positive
    }
}