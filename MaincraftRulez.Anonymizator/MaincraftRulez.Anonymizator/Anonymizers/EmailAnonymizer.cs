// Modified EmailAnonymizer.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static MaincraftRulez.Anonymizator.Constants;

namespace MaincraftRulez.Anonymizator;

public class EmailAnonymizer : BaseAnonymizer
{
    private bool _preserveDomain;
    private bool _preserveDots;
    private bool _preserveUnderscores;
    
    public EmailAnonymizer(IFF3Cipher cipher) : base(cipher)
    {
        _preserveDomain = true;
        _preserveDots = false;
        _preserveUnderscores = false;
        
        // Only preserve @ by default
        SetPreserveCharacters(new[] { '@' });
    }
    
    public void SetPreserveDomain(bool preserve)
    {
        _preserveDomain = preserve;
    }
    
    public void SetPreserveDots(bool preserve)
    {
        _preserveDots = preserve;
        UpdatePreservedChars();
    }
    
    public void SetPreserveUnderscores(bool preserve)
    {
        _preserveUnderscores = preserve;
        UpdatePreservedChars();
    }
    
    private void UpdatePreservedChars()
    {
        var chars = new List<char> { '@' };
        
        if (_preserveDots)
            chars.Add('.');
            
        if (_preserveUnderscores)
            chars.Add('_');
            
        SetPreserveCharacters(chars.ToArray());
    }
    
    public override string Anonymize(string email)
    {
        var match = Regex.Match(email, Patterns.EmailPattern);
        
        if (!match.Success)
            return base.Anonymize(email);
            
        string username = match.Groups[1].Value;
        string domain = match.Groups[2].Value;
        
        // Simply use BaseAnonymizer's character preservation logic
        // which is already deterministic and doesn't rely on instance state
        if (_preserveDots || _preserveUnderscores)
        {
            return base.Anonymize(email);
        }
        else
        {
            // Handle username part
            string encryptedUsername = _cipher.WithCustomAlphabet(Alphabets.Email).Encrypt(username);
            
            // Handle domain part
            if (_preserveDomain)
                return encryptedUsername + "@" + domain;
                
            string encryptedDomain = _cipher.WithCustomAlphabet(Alphabets.Email).Encrypt(domain);
            return encryptedUsername + "@" + encryptedDomain;
        }
    }
    
    public override string Deanonymize(string anonymizedEmail)
    {
        var match = Regex.Match(anonymizedEmail, Patterns.EmailPattern);
        
        if (!match.Success)
            return base.Deanonymize(anonymizedEmail);
            
        string encryptedUsername = match.Groups[1].Value;
        string domain = match.Groups[2].Value;
        
        // Use BaseAnonymizer's character preservation logic for consistency
        if (_preserveDots || _preserveUnderscores)
        {
            return base.Deanonymize(anonymizedEmail);
        }
        else
        {
            // Handle username part
            string decryptedUsername = _cipher.WithCustomAlphabet(Alphabets.Email).Decrypt(encryptedUsername);
            
            // Handle domain part
            if (_preserveDomain)
                return decryptedUsername + "@" + domain;
                
            string decryptedDomain = _cipher.WithCustomAlphabet(Alphabets.Email).Decrypt(domain);
            return decryptedUsername + "@" + decryptedDomain;
        }
    }
}