using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;


namespace MaincraftRulez.Anonymizator;

public class FF3Cipher : IFF3Cipher
{
    private readonly byte[] _key;
    private readonly int _radix;
    private readonly string _alphabet;
    private byte[] _tweak;
    private const int DOMAIN_MIN = 1000000; // FF3-1 minimum domain size
    private const int TWEAK_LEN = 7;        // FF3-1 uses 56-bit tweak
    
    public FF3Cipher(string key, byte[] tweak, int radix = 10)
    {
        _key = ByteUtil.HexStringToBytes(key);
        _radix = radix;
        _alphabet = AlphabetUtil.GetDefaultAlphabet(radix);
        ValidateTweak(tweak);
        _tweak = tweak;
    }
    
    public FF3Cipher(string key, byte[] tweak, string alphabet)
    {
        _key = ByteUtil.HexStringToBytes(key);
        _alphabet = alphabet;
        _radix = alphabet.Length;
        ValidateTweak(tweak);
        _tweak = tweak;
    }
    
    public static FF3Cipher WithCustomAlphabet(string key, byte[] tweak, string alphabet)
    {
        return new FF3Cipher(key, tweak, alphabet);
    }
    
    public IFF3Cipher WithCustomAlphabet(string alphabet)
    {
        return new FF3Cipher(ByteUtil.BytesToHexString(_key), _tweak, alphabet);
    }
    
    private void ValidateTweak(byte[] tweak)
    {
        if (tweak == null || tweak.Length != TWEAK_LEN)
            throw new ArgumentException($"Tweak must be {TWEAK_LEN} bytes");
        
        // Validate domain size
        int minLen = (int)Math.Ceiling(Math.Log(DOMAIN_MIN) / Math.Log(_radix));
        if (minLen < 2)
            throw new ArgumentException($"Radix {_radix} requires minimum length of 2");
    }
    
    public string Encrypt(string plaintext, byte[] tweak = null)
    {
        var tweakToUse = tweak ?? _tweak;
        ValidateInput(plaintext);
        
        int n = plaintext.Length;
        int u = n / 2;
        int v = n - u;
        
        // Split the message
        string A = plaintext.Substring(0, u);
        string B = plaintext.Substring(u);
        
        // Start rounds
        for (int i = 0; i < 8; i++)
        {
            if (i % 2 == 0)
            {
                byte[] P = CalculateP(i, tweakToUse, B);
                string c = EncryptBlock(P);
                BigInteger y = IntegerUtil.AddMod(
                    IntegerUtil.DecodeRadix(A, _alphabet),
                    IntegerUtil.DecodeRadix(c, _alphabet),
                    BigInteger.Pow(new BigInteger(_radix), u)
                );
                A = IntegerUtil.EncodeRadix(y, _alphabet, u);
            }
            else
            {
                byte[] P = CalculateP(i, tweakToUse, A);
                string c = EncryptBlock(P);
                BigInteger y = IntegerUtil.AddMod(
                    IntegerUtil.DecodeRadix(B, _alphabet),
                    IntegerUtil.DecodeRadix(c, _alphabet),
                    BigInteger.Pow(new BigInteger(_radix), v)
                );
                B = IntegerUtil.EncodeRadix(y, _alphabet, v);
            }
        }
        
        return A + B;
    }
    
    public string Decrypt(string ciphertext, byte[] tweak = null)
    {
        // Implementation similar to Encrypt with reversed rounds
        // (I'll abbreviate this for now, but it follows the same pattern)
        var tweakToUse = tweak ?? _tweak;
        ValidateInput(ciphertext);
        
        int n = ciphertext.Length;
        int u = n / 2;
        int v = n - u;
        
        // Split the message
        string A = ciphertext.Substring(0, u);
        string B = ciphertext.Substring(u);
        
        // Start rounds (decryption runs the rounds in reverse)
        for (int i = 7; i >= 0; i--)
        {
            if (i % 2 == 0)
            {
                byte[] P = CalculateP(i, tweakToUse, B);
                string c = EncryptBlock(P);
                BigInteger y = IntegerUtil.SubMod(
                    IntegerUtil.DecodeRadix(A, _alphabet),
                    IntegerUtil.DecodeRadix(c, _alphabet),
                    BigInteger.Pow(new BigInteger(_radix), u)
                );
                A = IntegerUtil.EncodeRadix(y, _alphabet, u);
            }
            else
            {
                byte[] P = CalculateP(i, tweakToUse, A);
                string c = EncryptBlock(P);
                BigInteger y = IntegerUtil.SubMod(
                    IntegerUtil.DecodeRadix(B, _alphabet),
                    IntegerUtil.DecodeRadix(c, _alphabet),
                    BigInteger.Pow(new BigInteger(_radix), v)
                );
                B = IntegerUtil.EncodeRadix(y, _alphabet, v);
            }
        }
        
        return A + B;
    }
    
    private void ValidateInput(string input)
    {
        // Validate each character is in the alphabet
        if (input.Any(c => !_alphabet.Contains(c)))
            throw new ArgumentException("Input contains characters not in alphabet");
            
        // Validate length constraints
        int maxLen = (int)(2 * Math.Floor(96 / Math.Log(_radix, 2)));
        if (input.Length < 2 || input.Length > maxLen)
            throw new ArgumentException($"Input length must be between 2 and {maxLen}");
    }
    
    private byte[] CalculateP(int i, byte[] tweak, string X)
    {
        // Create P as in the FF3-1 spec
        byte[] result = new byte[16];
        
        // Set first byte based on round
        result[0] = (byte)(i & 0xFF);
        
        // TW_1, TW_2, ... according to spec
        if (i % 2 == 0)
        {
            Array.Copy(tweak, 0, result, 1, 3);
            Array.Copy(tweak.Skip(3).ToArray(), 0, result, 8, 4);
        }
        else
        {
            Array.Copy(tweak, 3, result, 1, 3);
            Array.Copy(tweak, 0, result, 8, 3);
            result[11] = 0;
        }
        
        // Representation of X as a numeral string
        BigInteger numX = IntegerUtil.DecodeRadix(X, _alphabet);
        byte[] bNumX = numX.ToByteArray().Reverse().ToArray(); // Network byte order
        
        // Ensure correct length with padding
        byte[] paddedNumX = new byte[4];
        int copyLen = Math.Min(bNumX.Length, 4);
        Array.Copy(bNumX, 0, paddedNumX, 4 - copyLen, copyLen);
        
        Array.Copy(paddedNumX, 0, result, 12, 4);
        
        return result;
    }
    
    private string EncryptBlock(byte[] block)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            
            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] encrypted = encryptor.TransformFinalBlock(block, 0, block.Length);
            
            // Create a positive BigInteger from the encrypted bytes
            byte[] positiveBytes = new byte[encrypted.Length + 1];
            Array.Copy(encrypted, 0, positiveBytes, 0, encrypted.Length);
            positiveBytes[encrypted.Length] = 0; // Ensure positive by adding a zero byte at the end
            
            BigInteger y = new BigInteger(positiveBytes.Reverse().ToArray());
            return IntegerUtil.EncodeRadix(y, _alphabet);
        }
    }
}