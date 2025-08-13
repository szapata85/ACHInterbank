using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Application.Helpers.Hash;

public static class HashHelper
{
    public static string GenerateHashSha1(string input)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha1.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}