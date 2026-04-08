using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EduZim.Infrastructure.Persistence.Encryption;

/// <summary>
/// Encrypts/decrypts PII at rest using DataProtection. Value comparer compares decrypted values so EF does not mark rows modified when ciphertext changes between Protect calls.
/// </summary>
public static class PiiStringConverter
{
    public const string Purpose = "EduZim.Pii.v1";

    public static ValueConverter<string?, string?> Create(IDataProtectionProvider provider)
    {
        var protector = provider.CreateProtector(Purpose);

        return new ValueConverter<string?, string?>(
            v => v == null ? null : protector.Protect(v),
            v => v == null ? null : protector.Unprotect(v));
    }

    public static ValueComparer<string?> CreateComparer(IDataProtectionProvider provider)
    {
        var protector = provider.CreateProtector(Purpose);

        return new ValueComparer<string?>(
            (a, b) => string.Equals(Unprotect(protector, a), Unprotect(protector, b), StringComparison.Ordinal),
            v => UnprotectHash(protector, v),
            v => v);
    }

    private static int UnprotectHash(IDataProtector protector, string? stored)
    {
        var plain = Unprotect(protector, stored);
        return plain is null ? 0 : plain.GetHashCode(StringComparison.Ordinal);
    }

    private static string? Unprotect(IDataProtector protector, string? stored)
    {
        if (stored is null)
            return null;
        try
        {
            return protector.Unprotect(stored);
        }
        catch
        {
            return stored;
        }
    }
}
