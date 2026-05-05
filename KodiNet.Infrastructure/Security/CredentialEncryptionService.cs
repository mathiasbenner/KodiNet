using KodiNet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.DataProtection;

namespace KodiNet.Infrastructure.Security;

// ───  AES encryption of credentials via DataProtection API ──────────────────

public sealed class CredentialEncryptionService(IDataProtector protector) : ICredentialEncryption
{
    public string Encrypt(string plainText)  => protector.Protect(plainText);
    public string Decrypt(string cipherText) => protector.Unprotect(cipherText);
}