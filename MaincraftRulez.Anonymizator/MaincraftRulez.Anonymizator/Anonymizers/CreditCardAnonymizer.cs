// Anonymizers/CreditCardAnonymizer.cs - Updated implementation
using System.Text.RegularExpressions;

namespace MaincraftRulez.Anonymizator;

public class CreditCardAnonymizer : BaseAnonymizer
{
    private bool _preserveFirstFour;
    private bool _preserveLastFour;
    
    public CreditCardAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveFirstFour = false;
        _preserveLastFour = true;
    }
    
    public void SetPreserveFirstFour(bool preserve)
    {
        _preserveFirstFour = preserve;
    }
    
    public void SetPreserveLastFour(bool preserve)
    {
        _preserveLastFour = preserve;
    }
    
    public override string Anonymize(string creditCard)
    {
        // Remove any non-digit characters
        string digitsOnly = Regex.Replace(creditCard, @"\D", "");
        
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
            return base.Anonymize(creditCard); // Not a valid CC number
            
        string partToEncrypt = digitsOnly;
        string result = "";
        
        if (_preserveFirstFour)
        {
            result += digitsOnly.Substring(0, 4);
            partToEncrypt = digitsOnly.Substring(4);
        }
        
        if (_preserveLastFour)
        {
            string lastFour = partToEncrypt.Substring(partToEncrypt.Length - 4);
            partToEncrypt = partToEncrypt.Substring(0, partToEncrypt.Length - 4);
            
            // Encrypt middle part
            if (!string.IsNullOrEmpty(partToEncrypt))
            {
                result += _cipher.Encrypt(partToEncrypt);
            }
            
            result += lastFour;
        }
        else if (!string.IsNullOrEmpty(partToEncrypt))
        {
            // Encrypt everything except possibly first four
            result += _cipher.Encrypt(partToEncrypt);
        }
        
        // Reformat the result to match original format
        if (creditCard.Contains("-") || creditCard.Contains(" "))
        {
            // Format with separators
            string formatted = "";
            for (int i = 0; i < result.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    formatted += creditCard.Contains("-") ? "-" : " ";
                }
                formatted += result[i];
            }
            return formatted;
        }
        
        return result;
    }
    
    public override string Deanonymize(string anonymizedCC)
    {
        // Remove any non-digit characters
        string digitsOnly = Regex.Replace(anonymizedCC, @"\D", "");
        
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
            return base.Deanonymize(anonymizedCC); // Not a standard CC format
        
        string partToDecrypt = digitsOnly;
        string result = "";
        
        if (_preserveFirstFour)
        {
            result += digitsOnly.Substring(0, 4);
            partToDecrypt = digitsOnly.Substring(4);
        }
        
        if (_preserveLastFour)
        {
            string lastFour = partToDecrypt.Substring(partToDecrypt.Length - 4);
            partToDecrypt = partToDecrypt.Substring(0, partToDecrypt.Length - 4);
            
            // Decrypt middle part
            if (!string.IsNullOrEmpty(partToDecrypt))
            {
                result += _cipher.Decrypt(partToDecrypt);
            }
            
            result += lastFour;
        }
        else if (!string.IsNullOrEmpty(partToDecrypt))
        {
            // Decrypt everything except possibly first four
            result += _cipher.Decrypt(partToDecrypt);
        }
        
        // Reformat the result to match original format
        if (anonymizedCC.Contains("-") || anonymizedCC.Contains(" "))
        {
            // Format with separators
            string formatted = "";
            for (int i = 0; i < result.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    formatted += anonymizedCC.Contains("-") ? "-" : " ";
                }
                formatted += result[i];
            }
            return formatted;
        }
        
        return result;
    }
}