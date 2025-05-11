// Anonymizers/StringAnonymizer.cs - Complete redesign
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MaincraftRulez.Anonymizator;

public class StringAnonymizer : BaseAnonymizer
{
    private bool _preserveCase;
    private bool _preserveSpaces;
    private bool _preservePunctuation;
    
    // Fixed substitution alphabets
    private static readonly string SOURCE_ALPHA_LOWER = "abcdefghijklmnopqrstuvwxyz";
    private static readonly string SOURCE_ALPHA_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string SOURCE_DIGITS = "0123456789";
    
    // We'll generate these for each instance based on the key
    private string _targetAlphaLower;
    private string _targetAlphaUpper;
    private string _targetDigits;
    
    public StringAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveCase = true;
        _preserveSpaces = true;
        _preservePunctuation = true;
        
        // Generate substitution alphabets using the cipher as a seed
        GenerateSubstitutionAlphabets();
    }
    
    private void GenerateSubstitutionAlphabets()
    {
        // Use the cipher to generate deterministic but shuffled alphabets
        try
        {
            // For lowercase, use a simple FF3 operation to generate a seed
            string seedLower = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("a1b2c3d4e5f6");
            _targetAlphaLower = ShuffleAlphabet(SOURCE_ALPHA_LOWER, seedLower);
            
            // For uppercase, use a different seed
            string seedUpper = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("f6e5d4c3b2a1");
            _targetAlphaUpper = ShuffleAlphabet(SOURCE_ALPHA_UPPER, seedUpper);
            
            // For digits
            string seedDigits = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("1a2b3c4d5e6f");
            _targetDigits = ShuffleAlphabet(SOURCE_DIGITS, seedDigits);
        }
        catch
        {
            // Fallback if cipher fails
            _targetAlphaLower = "zyxwvutsrqponmlkjihgfedcba";
            _targetAlphaUpper = "ZYXWVUTSRQPONMLKJIHGFEDCBA";
            _targetDigits = "9876543210";
        }
    }
    
    private string ShuffleAlphabet(string sourceAlphabet, string seed)
    {
        // Create a reproducible shuffle of the alphabet using the seed
        List<char> chars = sourceAlphabet.ToList();
        int seedValue = 0;
        
        // Use the seed to create a numeric value
        foreach (char c in seed)
        {
            seedValue = (seedValue * 31 + c) % 997; // Prime number operations for better distribution
        }
        
        // Fisher-Yates shuffle with deterministic randomness
        for (int i = chars.Count - 1; i > 0; i--)
        {
            seedValue = (seedValue * 31 + i) % 997;
            int j = seedValue % (i + 1);
            
            // Swap characters
            char temp = chars[i];
            chars[i] = chars[j];
            chars[j] = temp;
        }
        
        return new string(chars.ToArray());
    }
    
    public void SetPreserveCase(bool preserve)
    {
        _preserveCase = preserve;
    }
    
    public void SetPreserveSpaces(bool preserve)
    {
        _preserveSpaces = preserve;
    }
    
    public void SetPreservePunctuation(bool preserve)
    {
        _preservePunctuation = preserve;
    }
    
    public override string Anonymize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // If pattern preservation is specified, use base implementation
        if (!string.IsNullOrEmpty(_preservePattern) || _preserveChars.Length > 0)
        {
            return base.Anonymize(input);
        }
        
        // Process character by character
        char[] result = new char[input.Length];
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            // Determine if this character should be preserved
            bool preserve = false;
            
            if (_preserveSpaces && char.IsWhiteSpace(c))
                preserve = true;
                
            if (_preservePunctuation && (char.IsPunctuation(c) || char.IsSymbol(c)))
                preserve = true;
                
            // Always preserve non-ASCII characters
            if (c > 127)
                preserve = true;
                
            if (preserve)
            {
                result[i] = c;
            }
            else if (char.IsLower(c) && SOURCE_ALPHA_LOWER.Contains(c))
            {
                // Substitute lowercase letter
                int index = SOURCE_ALPHA_LOWER.IndexOf(c);
                result[i] = _targetAlphaLower[index];
            }
            else if (char.IsUpper(c) && SOURCE_ALPHA_UPPER.Contains(c))
            {
                // Substitute uppercase letter
                int index = SOURCE_ALPHA_UPPER.IndexOf(c);
                result[i] = _targetAlphaUpper[index];
            }
            else if (char.IsDigit(c) && SOURCE_DIGITS.Contains(c))
            {
                // Substitute digit
                int index = SOURCE_DIGITS.IndexOf(c);
                result[i] = _targetDigits[index];
            }
            else
            {
                // Anything else is preserved
                result[i] = c;
            }
        }
        
        return new string(result);
    }
    
    public override string Deanonymize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // If pattern preservation is specified, use base implementation
        if (!string.IsNullOrEmpty(_preservePattern) || _preserveChars.Length > 0)
        {
            return base.Deanonymize(input);
        }
        
        // Process character by character
        char[] result = new char[input.Length];
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            // Determine if this character should be preserved
            bool preserve = false;
            
            if (_preserveSpaces && char.IsWhiteSpace(c))
                preserve = true;
                
            if (_preservePunctuation && (char.IsPunctuation(c) || char.IsSymbol(c)))
                preserve = true;
                
            // Always preserve non-ASCII characters
            if (c > 127)
                preserve = true;
                
            if (preserve)
            {
                result[i] = c;
            }
            else if (char.IsLower(c) && _targetAlphaLower.Contains(c))
            {
                // Reverse substitute lowercase letter
                int index = _targetAlphaLower.IndexOf(c);
                result[i] = SOURCE_ALPHA_LOWER[index];
            }
            else if (char.IsUpper(c) && _targetAlphaUpper.Contains(c))
            {
                // Reverse substitute uppercase letter
                int index = _targetAlphaUpper.IndexOf(c);
                result[i] = SOURCE_ALPHA_UPPER[index];
            }
            else if (char.IsDigit(c) && _targetDigits.Contains(c))
            {
                // Reverse substitute digit
                int index = _targetDigits.IndexOf(c);
                result[i] = SOURCE_DIGITS[index];
            }
            else
            {
                // Anything else is preserved
                result[i] = c;
            }
        }
        
        return new string(result);
    }
}