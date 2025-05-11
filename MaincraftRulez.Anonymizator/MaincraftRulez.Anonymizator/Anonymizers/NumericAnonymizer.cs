// Anonymizers/NumericAnonymizer.cs
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using static MaincraftRulez.Anonymizator.Constants;

namespace MaincraftRulez.Anonymizator;

public class NumericAnonymizer : BaseAnonymizer
{
    private bool _preserveSign;
    private bool _preserveDecimalPoint;
    private int _preserveDecimalPlaces;
    private bool _preserveMagnitude;
    private const int CHUNK_SIZE = 16; // Safe size for FF3-1
    
    public NumericAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveSign = true;
        _preserveDecimalPoint = true;
        _preserveDecimalPlaces = -1; // -1 means preserve all decimal places
        _preserveMagnitude = false;
    }
    
    public void SetPreserveSign(bool preserve)
    {
        _preserveSign = preserve;
    }
    
    public void SetPreserveDecimalPoint(bool preserve)
    {
        _preserveDecimalPoint = preserve;
    }
    
    public void SetPreserveDecimalPlaces(int places)
    {
        _preserveDecimalPlaces = places;
    }
    
    public void SetPreserveMagnitude(bool preserve)
    {
        _preserveMagnitude = preserve;
    }
    
    public override string Anonymize(string input)
    {
        // Try to parse as a number
        if (!double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
        {
            // Not a valid number, try the base implementation
            return base.Anonymize(input);
        }
        
        // Extract components
        bool isNegative = value < 0;
        string numberString = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
        
        // Split integer and decimal parts
        string[] parts = numberString.Split('.');
        string integerPart = parts[0];
        string decimalPart = parts.Length > 1 ? parts[1] : "";
        
        // Process integer part in chunks if needed
        string encryptedIntegerPart = EncryptInChunks(integerPart);
        
        // Adjust length for magnitude preservation
        if (_preserveMagnitude && encryptedIntegerPart.Length != integerPart.Length)
        {
            if (encryptedIntegerPart.Length < integerPart.Length)
            {
                encryptedIntegerPart = encryptedIntegerPart.PadLeft(integerPart.Length, '0');
            }
            else
            {
                encryptedIntegerPart = encryptedIntegerPart.Substring(0, integerPart.Length);
            }
        }
        
        // Process decimal part if present
        string encryptedDecimalPart = "";
        if (_preserveDecimalPoint && !string.IsNullOrEmpty(decimalPart))
        {
            if (_preserveDecimalPlaces >= 0 && _preserveDecimalPlaces < decimalPart.Length)
            {
                // Preserve some decimal places
                string preservedDecimal = decimalPart.Substring(0, _preserveDecimalPlaces);
                string toEncryptDecimal = decimalPart.Substring(_preserveDecimalPlaces);
                
                if (!string.IsNullOrEmpty(toEncryptDecimal))
                {
                    string encryptedExtra = EncryptInChunks(toEncryptDecimal);
                    encryptedDecimalPart = preservedDecimal + encryptedExtra;
                }
                else
                {
                    encryptedDecimalPart = preservedDecimal;
                }
            }
            else
            {
                // Encrypt all decimal places
                encryptedDecimalPart = EncryptInChunks(decimalPart);
            }
        }
        
        // Build result
        string result = encryptedIntegerPart;
        if (_preserveDecimalPoint && !string.IsNullOrEmpty(encryptedDecimalPart))
        {
            result += "." + encryptedDecimalPart;
        }
        
        // Add sign if needed
        if (_preserveSign && isNegative)
        {
            result = "-" + result;
        }
        
        return result;
    }
    
    public override string Deanonymize(string input)
    {
        // Check if it's a valid number format
        if (!Regex.IsMatch(input, @"^[+-]?\d+(\.\d+)?$"))
        {
            // Not a valid number format, use base implementation
            return base.Deanonymize(input);
        }
        
        // Extract components
        bool isNegative = input.StartsWith("-");
        string valueString = isNegative ? input.Substring(1) : input;
        
        // Split integer and decimal parts
        string[] parts = valueString.Split('.');
        string encryptedIntegerPart = parts[0];
        string encryptedDecimalPart = parts.Length > 1 ? parts[1] : "";
        
        // Decrypt integer part
        string decryptedIntegerPart = DecryptInChunks(encryptedIntegerPart);
        
        // Handle magnitude preservation
        if (_preserveMagnitude && decryptedIntegerPart.Length != encryptedIntegerPart.Length)
        {
            if (decryptedIntegerPart.Length < encryptedIntegerPart.Length)
            {
                decryptedIntegerPart = decryptedIntegerPart.PadLeft(encryptedIntegerPart.Length, '0');
            }
            else
            {
                decryptedIntegerPart = decryptedIntegerPart.Substring(0, encryptedIntegerPart.Length);
            }
        }
        
        // Decrypt decimal part if present
        string decryptedDecimalPart = "";
        if (_preserveDecimalPoint && !string.IsNullOrEmpty(encryptedDecimalPart))
        {
            if (_preserveDecimalPlaces >= 0 && encryptedDecimalPart.Length > _preserveDecimalPlaces)
            {
                // The first _preserveDecimalPlaces digits were preserved
                string preservedPart = encryptedDecimalPart.Substring(0, _preserveDecimalPlaces);
                string encryptedPart = encryptedDecimalPart.Substring(_preserveDecimalPlaces);
                
                if (!string.IsNullOrEmpty(encryptedPart))
                {
                    string decryptedPart = DecryptInChunks(encryptedPart);
                    decryptedDecimalPart = preservedPart + decryptedPart;
                }
                else
                {
                    decryptedDecimalPart = preservedPart;
                }
            }
            else
            {
                // All decimal places were encrypted
                decryptedDecimalPart = DecryptInChunks(encryptedDecimalPart);
            }
        }
        
        // Build the result
        string result = decryptedIntegerPart;
        if (_preserveDecimalPoint && !string.IsNullOrEmpty(decryptedDecimalPart))
        {
            result += "." + decryptedDecimalPart;
        }
        
        // Add sign if needed
        if (_preserveSign && isNegative)
        {
            result = "-" + result;
        }
        
        return result;
    }
    
    private string EncryptInChunks(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        if (input.Length <= CHUNK_SIZE)
        {
            // If it's short enough, encrypt directly
            try
            {
                // Make sure it's at least 2 characters (minimum requirement)
                if (input.Length < 2)
                    input = input.PadRight(2, '0');
                    
                return _cipher.WithCustomAlphabet(Alphabets.Digits).Encrypt(input);
            }
            catch
            {
                // If encryption fails, return original
                return input;
            }
        }
        
        // For longer inputs, break into chunks
        var result = "";
        for (int i = 0; i < input.Length; i += CHUNK_SIZE)
        {
            int length = Math.Min(CHUNK_SIZE, input.Length - i);
            string chunk = input.Substring(i, length);
            
            // Ensure at least 2 characters
            if (chunk.Length < 2)
                chunk = chunk.PadRight(2, '0');
                
            try
            {
                string encrypted = _cipher.WithCustomAlphabet(Alphabets.Digits).Encrypt(chunk);
                result += encrypted;
            }
            catch
            {
                // On failure, add the original chunk
                result += chunk;
            }
        }
        
        return result;
    }
    
    private string DecryptInChunks(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        if (input.Length <= CHUNK_SIZE)
        {
            // If it's short enough, decrypt directly
            try
            {
                // Ensure it meets minimum length
                if (input.Length < 2)
                    return input;
                    
                return _cipher.WithCustomAlphabet(Alphabets.Digits).Decrypt(input);
            }
            catch
            {
                // If decryption fails, return original
                return input;
            }
        }
        
        // For longer inputs, break into chunks
        var result = "";
        for (int i = 0; i < input.Length; i += CHUNK_SIZE)
        {
            int length = Math.Min(CHUNK_SIZE, input.Length - i);
            string chunk = input.Substring(i, length);
            
            // Ensure meets minimum length
            if (chunk.Length < 2)
            {
                result += chunk;
                continue;
            }
                
            try
            {
                string decrypted = _cipher.WithCustomAlphabet(Alphabets.Digits).Decrypt(chunk);
                result += decrypted;
            }
            catch
            {
                // On failure, add the original chunk
                result += chunk;
            }
        }
        
        return result;
    }
}