using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static MaincraftRulez.Anonymizator.Constants;

namespace MaincraftRulez.Anonymizator;

public abstract class BaseAnonymizer : IAnonymizer
{
    protected readonly IFF3Cipher _cipher;
    protected char[] _preserveChars;
    protected string _preservePattern;
    
    protected BaseAnonymizer(IFF3Cipher cipher)
    {
        _cipher = cipher;
        _preserveChars = new char[0];
        _preservePattern = string.Empty;
    }
    
    public virtual string Anonymize(string input)
    {
        // Apply pattern preservation if specified
        if (!string.IsNullOrEmpty(_preservePattern))
        {
            return AnonymizeWithPattern(input);
        }
        
        // Apply character preservation
        if (_preserveChars.Length > 0)
        {
            return AnonymizeWithPreservedChars(input);
        }
        
        // Basic anonymization
        return _cipher.Encrypt(input);
    }
    
    public virtual string Deanonymize(string input)
    {
        // Apply pattern preservation if specified
        if (!string.IsNullOrEmpty(_preservePattern))
        {
            return DeanonymizeWithPattern(input);
        }
        
        // Apply character preservation
        if (_preserveChars.Length > 0)
        {
            return DeanonymizeWithPreservedChars(input);
        }
        
        // Basic deanonymization
        return _cipher.Decrypt(input);
    }
    
    public void SetPreserveCharacters(char[] characters)
    {
        _preserveChars = characters;
    }
    
    public void SetPreservePattern(string pattern)
    {
        _preservePattern = pattern;
    }
    
    protected virtual string AnonymizeWithPreservedChars(string input)
{
    //Console.WriteLine($"AnonymizeWithPreservedChars: Input: {input}");
    //Console.WriteLine($"PreservedChars: {string.Join(", ", _preserveChars)}");
    
    if (string.IsNullOrEmpty(input))
        return input;
        
    // 1. Create a map of positions to preserved characters
    Dictionary<int, char> preservedCharMap = new Dictionary<int, char>();
    
    // 2. Create a string without the preserved characters
    StringBuilder plaintext = new StringBuilder();
    for (int i = 0; i < input.Length; i++)
    {
        if (_preserveChars.Contains(input[i]))
        {
            preservedCharMap[i] = input[i];
        }
        else
        {
            plaintext.Append(input[i]);
        }
    }
    
    //Console.WriteLine($"Preserved positions: {string.Join(", ", preservedCharMap.Keys)}");
    //Console.WriteLine($"Plaintext (without preserved chars): {plaintext}");
    
    // 3. Early return if nothing to encrypt
    if (plaintext.Length == 0)
        return input;
        
    // Ensure minimum length for FF3
    if (plaintext.Length < 2)
        plaintext.Append('X');
            
    // Use a very strict alphabet approach - only use letters and numbers
    StringBuilder filteredText = new StringBuilder();
    foreach (char c in plaintext.ToString())
    {
        if ((c >= 'a' && c <= 'z') || 
            (c >= 'A' && c <= 'Z') || 
            (c >= '0' && c <= '9'))
        {
            filteredText.Append(c);
        }
    }
    
    string safeText = filteredText.ToString();
    
    // If text is too short or empty after filtering
    if (string.IsNullOrEmpty(safeText) || safeText.Length < 2)
        safeText = "XX";
    
    //Console.WriteLine($"Safe text to encrypt: {safeText}");
    
    // IMPORTANT: Use a custom alphabet that matches exactly what we're encrypting
    string customAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    // Encrypt using custom alphabet for reliability
    string ciphertext = _cipher.WithCustomAlphabet(customAlphabet).Encrypt(safeText);
    //Console.WriteLine($"Encrypted result: {ciphertext}");
    
    // 6. Build the final result with preserved characters
    StringBuilder result = new StringBuilder(ciphertext);
    
    // Process in ascending order to maintain correct positions
    foreach (var pos in preservedCharMap.Keys.OrderBy(k => k))
    {
        // Insert only if position is within bounds
        if (pos <= result.Length)
        {
            result.Insert(pos, preservedCharMap[pos]);
            //Console.WriteLine($"Inserted {preservedCharMap[pos]} at position {pos}, result now: {result}");
        }
        else
        {
            result.Append(preservedCharMap[pos]);
            //Console.WriteLine($"Appended {preservedCharMap[pos]}, result now: {result}");
        }
    }
    
    string finalResult = result.ToString();
    //Console.WriteLine($"Final anonymization result: {finalResult}");
    
    return finalResult;
}

 protected virtual string DeanonymizeWithPreservedChars(string input)
{
    //Console.WriteLine($"DeanonymizeWithPreservedChars: Input: {input}");
    //Console.WriteLine($"PreservedChars: {string.Join(", ", _preserveChars)}");
    
    if (string.IsNullOrEmpty(input))
        return input;
        
    // 1. Create a map of positions to preserved characters
    Dictionary<int, char> preservedCharMap = new Dictionary<int, char>();
    
    // 2. Create a string without the preserved characters
    StringBuilder ciphertext = new StringBuilder();
    for (int i = 0; i < input.Length; i++)
    {
        if (_preserveChars.Contains(input[i]))
        {
            preservedCharMap[i] = input[i];
        }
        else
        {
            ciphertext.Append(input[i]);
        }
    }
    
    //Console.WriteLine($"Preserved positions: {string.Join(", ", preservedCharMap.Keys)}");
    //Console.WriteLine($"Ciphertext (without preserved chars): {ciphertext}");
    
    // 3. Early return if nothing to decrypt
    if (ciphertext.Length == 0)
        return input;
        
    // Ensure minimum length for FF3
    if (ciphertext.Length < 2)
        return input;
    
    // Use same strict alphabet as in encryption
    string customAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    // 5. Decrypt using custom alphabet for reliability
    string plaintext;
    try
    {
        plaintext = _cipher.WithCustomAlphabet(customAlphabet).Decrypt(ciphertext.ToString());
        //Console.WriteLine($"Decrypted result: {plaintext}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during decryption: {ex.Message}");
        plaintext = ciphertext.ToString();
    }
    
    // 6. Build the final result with preserved characters
    StringBuilder result = new StringBuilder(plaintext);
    
    // Process in ascending order to maintain correct positions
    foreach (var pos in preservedCharMap.Keys.OrderBy(k => k))
    {
        // Insert only if position is within bounds
        if (pos <= result.Length)
        {
            result.Insert(pos, preservedCharMap[pos]);
            //Console.WriteLine($"Inserted {preservedCharMap[pos]} at position {pos}, result now: {result}");
        }
        else
        {
            result.Append(preservedCharMap[pos]);
            //Console.WriteLine($"Appended {preservedCharMap[pos]}, result now: {result}");
        }
    }
    
    string finalResult = result.ToString();
    //Console.WriteLine($"Final deanonymization result: {finalResult}");
    
    return finalResult;
}


    protected virtual string AnonymizeWithPattern(string input)
    {
        var regex = new Regex(_preservePattern);
        var match = regex.Match(input);
        
        if (match.Success)
        {
            // Create a copy of the original string to work with
            string result = input;
            int offset = 0;
            
            // Process each capturing group
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group.Success)
                {
                    // Extract the part to encrypt
                    string partToEncrypt = group.Value;
                    if (string.IsNullOrEmpty(partToEncrypt))
                        continue;
                    
                    // Skip minimum length check if needed
                    if (partToEncrypt.Length < 2)
                    {
                        // Just use a literal character replacement for single characters
                        char[] safeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
                        Random rnd = new Random();
                        char randomChar = safeChars[rnd.Next(safeChars.Length)];
                        string encryptedChar = randomChar.ToString();
                        
                        // Replace in the result - avoiding local variable declaration
                        result = result.Substring(0, group.Index + offset) + 
                                encryptedChar + 
                                result.Substring(group.Index + offset + group.Length);
                        
                        // Update offset
                        offset += encryptedChar.Length - group.Length;
                        continue;
                    }
                    
                    // Create a safe version of the text to encrypt
                    string safeText = new string(partToEncrypt
                        .Where(c => Alphabets.AlphaNumeric.Contains(c))
                        .ToArray());
                    
                    // If after filtering it's too short, use a placeholder
                    if (string.IsNullOrEmpty(safeText) || safeText.Length < 2)
                    {
                        safeText = "XX";
                    }
                    
                    // Encrypt using safe text and standard alphabet
                    string encryptedPart = _cipher.Encrypt(safeText);
                    
                    // Replace in the result - avoiding local variable declaration
                    result = result.Substring(0, group.Index + offset) + 
                            encryptedPart + 
                            result.Substring(group.Index + offset + group.Length);
                    
                    // Update offset
                    offset += encryptedPart.Length - group.Length;
                }
            }
            
            return result;
        }
        
        // If no match, encrypt the entire string with safe characters
        string safePlaintext = new string(input
            .Where(c => Alphabets.AlphaNumeric.Contains(c))
            .ToArray());
        
        if (string.IsNullOrEmpty(safePlaintext) || safePlaintext.Length < 2)
            safePlaintext = "XX";
            
        return _cipher.Encrypt(safePlaintext);
    }

    protected virtual string DeanonymizeWithPattern(string input)
    {
        var regex = new Regex(_preservePattern);
        var match = regex.Match(input);
        
        if (match.Success)
        {
            // Create a copy of the original string to work with
            string result = input;
            int offset = 0;
            
            // Process each capturing group
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group.Success)
                {
                    // Extract the part to decrypt
                    string partToDecrypt = group.Value;
                    if (string.IsNullOrEmpty(partToDecrypt))
                        continue;
                    
                    // Skip single character
                    if (partToDecrypt.Length < 2)
                    {
                        // We can't properly decrypt a single character with FF3
                        // Just leave it as is
                        continue;
                    }
                    
                    // Try to decrypt, with fallback
                    string decryptedPart;
                    try 
                    {
                        decryptedPart = _cipher.Decrypt(partToDecrypt);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error decrypting with pattern: {ex.Message}");
                        // If decryption fails, return the encrypted part unchanged
                        decryptedPart = partToDecrypt;
                        continue;
                    }
                    
                    // Replace in the result - avoiding local variable declaration
                    result = result.Substring(0, group.Index + offset) + 
                            decryptedPart + 
                            result.Substring(group.Index + offset + group.Length);
                    
                    // Update offset
                    offset += decryptedPart.Length - group.Length;
                }
            }
            
            return result;
        }
        
        // If no pattern match, decrypt the entire string
        try
        {
            return _cipher.Decrypt(input);
        }
        catch
        {
            // On failure, return as-is
            return input;
        }
    }
}