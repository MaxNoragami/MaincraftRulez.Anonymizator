using System;
using System.Security.Cryptography;


namespace MaincraftRulez.Anonymizator;

public class KeyGenerator
{
    private const int KEY_SIZE_BYTES = 32; // 256 bits
    private const int TWEAK_SIZE_BYTES = 7; // 56 bits for FF3-1
    
    public KeyTweakPair GenerateKeyTweakPair(string name = "")
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            // Generate key
            byte[] keyBytes = new byte[KEY_SIZE_BYTES];
            rng.GetBytes(keyBytes);
            string key = ByteUtil.BytesToHexString(keyBytes);
            
            // Generate tweak
            byte[] tweak = new byte[TWEAK_SIZE_BYTES];
            rng.GetBytes(tweak);
            
            return new KeyTweakPair(key, tweak, name);
        }
    }
    
    public KeyTweakPair DeriveKeyTweakPair(string masterKey, string identifier)
    {
        // Convert master key to bytes
        byte[] masterKeyBytes = ByteUtil.HexStringToBytes(masterKey);
        
        // Use HMAC-SHA256 for key derivation
        using (var hmac = new HMACSHA256(masterKeyBytes))
        {
            // Derive key bytes from identifier + "KEY"
            byte[] keyInput = System.Text.Encoding.UTF8.GetBytes(identifier + "KEY");
            byte[] keyBytes = hmac.ComputeHash(keyInput);
            string derivedKey = ByteUtil.BytesToHexString(keyBytes);
            
            // Derive tweak from identifier + "TWEAK"
            byte[] tweakInput = System.Text.Encoding.UTF8.GetBytes(identifier + "TWEAK");
            byte[] tweakBytes = hmac.ComputeHash(tweakInput);
            // Take only the first 7 bytes for FF3-1 tweak
            byte[] tweak = new byte[TWEAK_SIZE_BYTES];
            Array.Copy(tweakBytes, tweak, TWEAK_SIZE_BYTES);
            
            return new KeyTweakPair(derivedKey, tweak, identifier);
        }
    }
    
    public KeyTweakPair DerivePhoneSpecificPair(string masterKey, string phoneNumber)
    {
        // Remove non-digit characters
        string digitsOnly = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"\D", "");
        
        // Use the normalized phone number as the identifier
        return DeriveKeyTweakPair(masterKey, digitsOnly);
    }
}