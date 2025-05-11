using System;

namespace MaincraftRulez.Anonymizator;

public static class ByteUtil
{
    public static byte[] HexStringToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();
            
        int len = hex.Length;
        byte[] bytes = new byte[len / 2];
        
        for (int i = 0; i < len; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            
        return bytes;
    }
    
    public static string BytesToHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }
}