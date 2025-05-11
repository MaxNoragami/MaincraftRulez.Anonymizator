
namespace MaincraftRulez.Anonymizator;

public interface IFF3Cipher
{
    string Encrypt(string plaintext, byte[] tweak = null);
    string Decrypt(string ciphertext, byte[] tweak = null);
    IFF3Cipher WithCustomAlphabet(string alphabet);
}