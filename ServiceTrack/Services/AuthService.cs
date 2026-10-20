using System.Security.Cryptography;

namespace ServiceTrack;

// Password hashing with a random salt and PBKDF2 (built into .NET, no external package needed).
public static class AuthService
{
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        string[] parts = stored.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromHexString(parts[0]);
        byte[] expected = Convert.FromHexString(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
