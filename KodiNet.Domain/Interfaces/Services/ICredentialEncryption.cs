namespace KodiNet.Domain.Interfaces.Services;

public interface ICredentialEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
