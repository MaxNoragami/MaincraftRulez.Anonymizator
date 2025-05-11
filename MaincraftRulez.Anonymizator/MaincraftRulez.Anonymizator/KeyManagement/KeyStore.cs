using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MaincraftRulez.Anonymizator;

public class KeyStore
{
    private readonly string _storePath;
    private readonly string _password;
    private Dictionary<string, KeyTweakPair> _pairs = new Dictionary<string, KeyTweakPair>();
    
    public KeyStore(string storePath, string password)
    {
        _storePath = storePath;
        _password = password;
        
        if (File.Exists(_storePath))
        {
            LoadFromFile();
        }
    }
    
    public void AddKeyTweakPair(KeyTweakPair pair)
    {
        _pairs[pair.Name] = pair;
        SaveToFile();
    }
    
    public KeyTweakPair GetKeyTweakPair(string name)
    {
        if (_pairs.TryGetValue(name, out KeyTweakPair pair))
        {
            return pair;
        }
        
        return null;
    }
    
    public List<string> GetAllPairNames()
    {
        return new List<string>(_pairs.Keys);
    }
    
    private void LoadFromFile()
    {
        try
        {
            byte[] encryptedData = File.ReadAllBytes(_storePath);
            byte[] decryptedData = Decrypt(encryptedData, _password);
            
            string json = Encoding.UTF8.GetString(decryptedData);
            var storedPairs = JsonSerializer.Deserialize<List<StoredPair>>(json);
            
            _pairs.Clear();
            foreach (var storedPair in storedPairs)
            {
                _pairs[storedPair.Name] = new KeyTweakPair(
                    storedPair.Key,
                    ByteUtil.HexStringToBytes(storedPair.TweakHex),
                    storedPair.Name
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading key store: {ex.Message}");
            // In production, log this properly
        }
    }
    
    private void SaveToFile()
    {
        try
        {
            var storedPairs = new List<StoredPair>();
            
            foreach (var pair in _pairs.Values)
            {
                storedPairs.Add(new StoredPair
                {
                    Name = pair.Name,
                    Key = pair.Key,
                    TweakHex = ByteUtil.BytesToHexString(pair.Tweak)
                });
            }
            
            string json = JsonSerializer.Serialize(storedPairs);
            byte[] data = Encoding.UTF8.GetBytes(json);
            
            byte[] encryptedData = Encrypt(data, _password);
            File.WriteAllBytes(_storePath, encryptedData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving key store: {ex.Message}");
            // In production, log this properly
        }
    }
    
    private byte[] Encrypt(byte[] data, string password)
    {
        using (Aes aes = Aes.Create())
        {
            // Generate a key and IV from the password
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            var key = new Rfc2898DeriveBytes(password, salt, 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);
            
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length);
                
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                
                return ms.ToArray();
            }
        }
    }
    
    private byte[] Decrypt(byte[] encryptedData, string password)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[16];
            Array.Copy(encryptedData, 0, salt, 0, 16);
            
            var key = new Rfc2898DeriveBytes(password, salt, 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);
            
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedData, 16, encryptedData.Length - 16);
                    cs.FlushFinalBlock();
                }
                
                return ms.ToArray();
            }
        }
    }
    
    private class StoredPair
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string TweakHex { get; set; }
    }
}