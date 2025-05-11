using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaincraftRulez.Anonymizator;

public class NameAnonymizer : BaseAnonymizer
{
    private bool _preserveCapitalization;
    private bool _preserveSpecialChars;
    
    // Fixed substitution alphabets (like in StringAnonymizer)
    private static readonly string SOURCE_ALPHA_LOWER = "abcdefghijklmnopqrstuvwxyz";
    private static readonly string SOURCE_ALPHA_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string SPECIAL_CHARS = "ăĂîÎâÂșȘțȚüÜöÖäÄéÉèÈêÊëËàÀáÁíÍìÌñÑçÇ";
    
    // Generated substitution alphabets
    private string _targetAlphaLower;
    private string _targetAlphaUpper;
    private string _targetSpecial;
    
    public NameAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveCapitalization = true;
        _preserveSpecialChars = false;
        
        // Generate substitution alphabets
        GenerateSubstitutionAlphabets();
    }
    
    private void GenerateSubstitutionAlphabets()
    {
        try
        {
            // For lowercase, use a simple FF3 operation to generate a seed
            string seedLower = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("a1b2c3d4e5f6");
            _targetAlphaLower = ShuffleAlphabet(SOURCE_ALPHA_LOWER, seedLower);
            
            // For uppercase, use a different seed
            string seedUpper = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("f6e5d4c3b2a1");
            _targetAlphaUpper = ShuffleAlphabet(SOURCE_ALPHA_UPPER, seedUpper);
            
            // For special characters
            string seedSpecial = _cipher.WithCustomAlphabet("0123456789abcdef").Encrypt("5p3c1alch4rs");
            _targetSpecial = ShuffleAlphabet(SPECIAL_CHARS, seedSpecial);
        }
        catch
        {
            // Fallback if cipher fails
            _targetAlphaLower = "zyxwvutsrqponmlkjihgfedcba";
            _targetAlphaUpper = "ZYXWVUTSRQPONMLKJIHGFEDCBA";
            
            // Create a reversed version of special chars as fallback
            char[] special = SPECIAL_CHARS.ToCharArray();
            Array.Reverse(special);
            _targetSpecial = new string(special);
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
    
    public void SetPreserveCapitalization(bool preserve)
    {
        _preserveCapitalization = preserve;
    }
    
    public void SetPreserveSpecialChars(bool preserve)
    {
        _preserveSpecialChars = preserve;
    }
    
    public override string Anonymize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        // Handle spaces and separators in names
        if (name.Contains(" ") || name.Contains("-") || name.Contains("."))
        {
            string[] parts = name.Split(new char[] {' ', '-', '.'}, StringSplitOptions.None);
            char[] separators = new char[parts.Length - 1];
            
            // Store separators for reconstruction
            int sepIdx = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == ' ' || name[i] == '-' || name[i] == '.')
                {
                    if (sepIdx < separators.Length)
                        separators[sepIdx++] = name[i];
                }
            }
            
            // Anonymize each part
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = AnonymizeSingleName(parts[i]);
            }
            
            // Reconstruct with original separators
            StringBuilder result = new StringBuilder(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                result.Append(separators[i-1]);
                result.Append(parts[i]);
            }
            
            return result.ToString();
        }
        
        return AnonymizeSingleName(name);
    }
    
    private string AnonymizeSingleName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        StringBuilder result = new StringBuilder(name.Length);
        
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            // Determine character type and substitute accordingly
            if (char.IsLower(c) && SOURCE_ALPHA_LOWER.Contains(c))
            {
                int index = SOURCE_ALPHA_LOWER.IndexOf(c);
                result.Append(_targetAlphaLower[index]);
            }
            else if (char.IsUpper(c) && SOURCE_ALPHA_UPPER.Contains(c))
            {
                int index = SOURCE_ALPHA_UPPER.IndexOf(c);
                result.Append(_targetAlphaUpper[index]);
            }
            else if (SPECIAL_CHARS.Contains(c))
            {
                if (_preserveSpecialChars)
                {
                    result.Append(c);
                }
                else
                {
                    int index = SPECIAL_CHARS.IndexOf(c);
                    result.Append(_targetSpecial[index]);
                }
            }
            else
            {
                // Any unrecognized character is preserved
                result.Append(c);
            }
        }
        
        // Handle capitalization if needed
        if (_preserveCapitalization && name.Length > 0 && result.Length > 0)
        {
            bool shouldCapitalize = char.IsUpper(name[0]);
            if (shouldCapitalize && !char.IsUpper(result[0]) && char.IsLetter(result[0]))
            {
                result[0] = char.ToUpper(result[0]);
            }
            else if (!shouldCapitalize && char.IsUpper(result[0]) && char.IsLetter(result[0]))
            {
                result[0] = char.ToLower(result[0]);
            }
        }
        
        return result.ToString();
    }
    
    public override string Deanonymize(string anonymizedName)
    {
        if (string.IsNullOrEmpty(anonymizedName))
            return anonymizedName;
            
        // Handle spaces and separators in names
        if (anonymizedName.Contains(" ") || anonymizedName.Contains("-") || anonymizedName.Contains("."))
        {
            string[] parts = anonymizedName.Split(new char[] {' ', '-', '.'}, StringSplitOptions.None);
            char[] separators = new char[parts.Length - 1];
            
            // Store separators for reconstruction
            int sepIdx = 0;
            for (int i = 0; i < anonymizedName.Length; i++)
            {
                if (anonymizedName[i] == ' ' || anonymizedName[i] == '-' || anonymizedName[i] == '.')
                {
                    if (sepIdx < separators.Length)
                        separators[sepIdx++] = anonymizedName[i];
                }
            }
            
            // Deanonymize each part
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = DeanonymizeSingleName(parts[i]);
            }
            
            // Reconstruct with original separators
            StringBuilder result = new StringBuilder(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                result.Append(separators[i-1]);
                result.Append(parts[i]);
            }
            
            return result.ToString();
        }
        
        return DeanonymizeSingleName(anonymizedName);
    }
    
    private string DeanonymizeSingleName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        StringBuilder result = new StringBuilder(name.Length);
        
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            // Determine character type and reverse substitute
            if (char.IsLower(c) && _targetAlphaLower.Contains(c))
            {
                int index = _targetAlphaLower.IndexOf(c);
                result.Append(SOURCE_ALPHA_LOWER[index]);
            }
            else if (char.IsUpper(c) && _targetAlphaUpper.Contains(c))
            {
                int index = _targetAlphaUpper.IndexOf(c);
                result.Append(SOURCE_ALPHA_UPPER[index]);
            }
            else if (!_preserveSpecialChars && _targetSpecial.Contains(c))
            {
                int index = _targetSpecial.IndexOf(c);
                result.Append(SPECIAL_CHARS[index]);
            }
            else
            {
                // Any unrecognized character is preserved
                result.Append(c);
            }
        }
        
        // Handle capitalization if needed
        if (_preserveCapitalization && name.Length > 0 && result.Length > 0)
        {
            bool shouldCapitalize = char.IsUpper(name[0]);
            if (shouldCapitalize && !char.IsUpper(result[0]) && char.IsLetter(result[0]))
            {
                result[0] = char.ToUpper(result[0]);
            }
            else if (!shouldCapitalize && char.IsUpper(result[0]) && char.IsLetter(result[0]))
            {
                result[0] = char.ToLower(result[0]);
            }
        }
        
        return result.ToString();
    }
}