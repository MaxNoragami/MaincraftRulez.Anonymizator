using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MaincraftRulez.Anonymizator;

public class PhoneNumberAnonymizer : BaseAnonymizer
{
    private bool _preserveCountryCode;
    private int _preserveLeadingDigits;
    
    // Dictionary of country calling codes (sorted by length for better detection)
    private static readonly Dictionary<string, string> CountryCodes = new Dictionary<string, string>
    {
        // 3-digit codes
        {"373", "Moldova"}, {"670", "East Timor"}, {"672", "Antarctica"}, {"673", "Brunei"}, {"674", "Nauru"}, 
        {"675", "Papua New Guinea"}, {"676", "Tonga"}, {"677", "Solomon Islands"}, {"678", "Vanuatu"}, 
        {"679", "Fiji"}, {"680", "Palau"}, {"682", "Cook Islands"}, {"683", "Niue"},
        {"685", "Samoa"}, {"686", "Kiribati"}, {"687", "New Caledonia"}, {"688", "Tuvalu"},
        {"689", "French Polynesia"}, {"690", "Tokelau"}, {"691", "Micronesia"}, {"692", "Marshall Islands"},
        
        // 2-digit codes
        {"20", "Egypt"}, {"27", "South Africa"},{"40", "Romania"}, {"41", "Switzerland"}, {"43", "Austria"}, 
        {"44", "United Kingdom"}, {"45", "Denmark"}, {"46", "Sweden"}, {"47", "Norway"}, 
        {"48", "Poland"}, {"49", "Germany"}, {"51", "Peru"}, {"52", "Mexico"}, 
        {"53", "Cuba"}, {"54", "Argentina"}, {"55", "Brazil"}, {"56", "Chile"}, 
        {"57", "Colombia"}, {"58", "Venezuela"}, {"60", "Malaysia"}, {"61", "Australia"}, 
        {"62", "Indonesia"}, {"63", "Philippines"}, {"64", "New Zealand"}, {"65", "Singapore"}, 
        {"66", "Thailand"}, {"81", "Japan"}, {"82", "South Korea"}, {"84", "Vietnam"}, 
        {"86", "China"}, {"90", "Turkey"}, {"91", "India"}, {"92", "Pakistan"}, 
        {"93", "Afghanistan"}, {"94", "Sri Lanka"}, {"95", "Myanmar"}, {"98", "Iran"},
        
        // 1-digit codes
        {"1", "North America"}, {"7", "Russia/Kazakhstan"}
    };
    
    public PhoneNumberAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveCountryCode = true;
        _preserveLeadingDigits = 0;
        // No need to set pattern here as we'll handle formatting explicitly
    }
    
    public void SetPreserveCountryCode(bool preserve)
    {
        _preserveCountryCode = preserve;
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
        
        // Determine the country code and rest of the number
        string countryCode = "";
        string restOfNumber = digitsOnly;
        
        // Try to detect country code using our dictionary - modified to be more reliable
        bool foundCountryCode = false;
        
        // First check for 3-digit codes (most specific)
        if (digitsOnly.Length >= 3)
        {
            string potentialCode = digitsOnly.Substring(0, 3);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfNumber = digitsOnly.Substring(3);
                foundCountryCode = true;
            }
        }
        
        // Then check 2-digit codes if no 3-digit code was found
        if (!foundCountryCode && digitsOnly.Length >= 2)
        {
            string potentialCode = digitsOnly.Substring(0, 2);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfNumber = digitsOnly.Substring(2);
                foundCountryCode = true;
            }
        }
        
        // Finally check 1-digit codes if no longer code was found
        if (!foundCountryCode && digitsOnly.Length >= 1)
        {
            string potentialCode = digitsOnly.Substring(0, 1);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfNumber = digitsOnly.Substring(1);
            }
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
        
        // Extract the country code
        string countryCode = "";
        string restOfDigits = digitsOnly;
        
        // Try to detect country code using our dictionary - modified to match Anonymize method
        bool foundCountryCode = false;
        
        // First check for 3-digit codes (most specific)
        if (digitsOnly.Length >= 3)
        {
            string potentialCode = digitsOnly.Substring(0, 3);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfDigits = digitsOnly.Substring(3);
                foundCountryCode = true;
            }
        }
        
        // Then check 2-digit codes if no 3-digit code was found
        if (!foundCountryCode && digitsOnly.Length >= 2)
        {
            string potentialCode = digitsOnly.Substring(0, 2);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfDigits = digitsOnly.Substring(2);
                foundCountryCode = true;
            }
        }
        
        // Finally check 1-digit codes if no longer code was found
        if (!foundCountryCode && digitsOnly.Length >= 1)
        {
            string potentialCode = digitsOnly.Substring(0, 1);
            if (CountryCodes.ContainsKey(potentialCode))
            {
                countryCode = potentialCode;
                restOfDigits = digitsOnly.Substring(1);
            }
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
        
        // Reformat to original style
        return ReformatPhoneNumber(anonymizedPhone, result);
    }
}



