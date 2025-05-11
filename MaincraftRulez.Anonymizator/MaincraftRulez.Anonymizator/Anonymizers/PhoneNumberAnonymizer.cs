using System.Text.RegularExpressions;

namespace MaincraftRulez.Anonymizator;

public class PhoneNumberAnonymizer : BaseAnonymizer
{
    private bool _preserveCountryCode;
    private bool _preserveAreaCode;
    private int _preserveLeadingDigits;
    
    public PhoneNumberAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveCountryCode = true;
        _preserveAreaCode = false;
        _preserveLeadingDigits = 0;
        // No need to set pattern here as we'll handle formatting explicitly
    }
    
    public void SetPreserveCountryCode(bool preserve)
    {
        _preserveCountryCode = preserve;
    }
    
    public void SetPreserveAreaCode(bool preserve)
    {
        _preserveAreaCode = preserve;
    }
    
    public void SetPreserveLeadingDigits(int count)
    {
        _preserveLeadingDigits = count;
    }
    
    public override string Anonymize(string phoneNumber)
    {
        // Extract only the digits for processing
        string digitsOnly = Regex.Replace(phoneNumber, @"\D", "");
        
        if (string.IsNullOrEmpty(digitsOnly))
        {
            return phoneNumber; // Nothing to encrypt
        }
        
        // Determine the country code, area code, and rest of the number
        string countryCode = "";
        string areaCode = "";
        string restOfNumber = digitsOnly;
        
        // Simple logic to extract parts (you may need to adjust this for different formats)
        if (digitsOnly.Length >= 10)
        {
            // Assume international format if longer than 10 digits
            if (digitsOnly.Length > 10)
            {
                countryCode = digitsOnly.Substring(0, digitsOnly.Length - 10);
                digitsOnly = digitsOnly.Substring(digitsOnly.Length - 10);
            }
            
            // Extract area code (first 3 digits in North American formats)
            areaCode = digitsOnly.Substring(0, 3);
            restOfNumber = digitsOnly.Substring(3);
        }
        
        // Determine which parts to encrypt
        string toEncrypt = "";
        string result = "";
        
        // Handle country code
        if (_preserveCountryCode && !string.IsNullOrEmpty(countryCode))
        {
            result += countryCode;
        }
        else
        {
            toEncrypt += countryCode;
        }
        
        // Handle area code
        if (_preserveAreaCode && !string.IsNullOrEmpty(areaCode))
        {
            result += areaCode;
        }
        else
        {
            toEncrypt += areaCode;
        }
        
        // Handle leading digits of remaining number
        if (_preserveLeadingDigits > 0 && restOfNumber.Length > _preserveLeadingDigits)
        {
            string leadingDigits = restOfNumber.Substring(0, _preserveLeadingDigits);
            string remainingDigits = restOfNumber.Substring(_preserveLeadingDigits);
            
            result += leadingDigits;
            toEncrypt += remainingDigits;
        }
        else
        {
            toEncrypt += restOfNumber;
        }
        
        // Encrypt the parts that should be encrypted
        string encryptedPart = "";
        if (!string.IsNullOrEmpty(toEncrypt))
        {
            encryptedPart = _cipher.Encrypt(toEncrypt);
        }
        
        // Combine preserved and encrypted parts
        string newDigits = result + encryptedPart;
        
        // Reconstruct the output with the original format
        return ReformatPhoneNumber(phoneNumber, newDigits);
    }
    
    private string ReformatPhoneNumber(string originalFormat, string newDigits)
    {
        int digitIndex = 0;
        char[] result = new char[originalFormat.Length];
        
        for (int i = 0; i < originalFormat.Length; i++)
        {
            if (char.IsDigit(originalFormat[i]))
            {
                if (digitIndex < newDigits.Length)
                {
                    result[i] = newDigits[digitIndex++];
                }
                else
                {
                    result[i] = 'X'; // Placeholder if we run out of digits
                }
            }
            else
            {
                result[i] = originalFormat[i]; // Keep non-digit characters
            }
        }
        
        return new string(result);
    }
    
    public override string Deanonymize(string anonymizedPhone)
    {
        // For deanonymization, we would need to know the original format
        // This is a simplified version
        string digitsOnly = Regex.Replace(anonymizedPhone, @"\D", "");
        
        // Extract the parts that were preserved
        string countryCode = "";
        string areaCode = "";
        string restOfDigits = digitsOnly;
        
        if (digitsOnly.Length >= 10)
        {
            if (digitsOnly.Length > 10)
            {
                countryCode = digitsOnly.Substring(0, digitsOnly.Length - 10);
                digitsOnly = digitsOnly.Substring(digitsOnly.Length - 10);
            }
            
            areaCode = digitsOnly.Substring(0, 3);
            restOfDigits = digitsOnly.Substring(3);
        }
        
        // Determine which parts need to be decrypted
        string toDecrypt = "";
        string result = "";
        
        if (_preserveCountryCode && !string.IsNullOrEmpty(countryCode))
        {
            result += countryCode;
        }
        else
        {
            toDecrypt += countryCode;
        }
        
        if (_preserveAreaCode && !string.IsNullOrEmpty(areaCode))
        {
            result += areaCode;
        }
        else
        {
            toDecrypt += areaCode;
        }
        
        if (_preserveLeadingDigits > 0 && restOfDigits.Length > _preserveLeadingDigits)
        {
            string leadingDigits = restOfDigits.Substring(0, _preserveLeadingDigits);
            string remainingDigits = restOfDigits.Substring(_preserveLeadingDigits);
            
            result += leadingDigits;
            toDecrypt += remainingDigits;
        }
        else
        {
            toDecrypt += restOfDigits;
        }
        
        // Decrypt the parts that were encrypted
        if (!string.IsNullOrEmpty(toDecrypt))
        {
            string decryptedPart = _cipher.Decrypt(toDecrypt);
            result += decryptedPart;
        }
        
        // Reformat to original style (simplistic approach)
        return ReformatPhoneNumber(anonymizedPhone, result);
    }
}