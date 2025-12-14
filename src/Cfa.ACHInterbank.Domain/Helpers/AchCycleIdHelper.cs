using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Domain.Helpers;

public static class AchCycleIdHelper
{
    public static string GenerateId(int clearingHouseId, string cycleName, DateTime processingDate)
    {
        string payload = $"{clearingHouseId}-{processingDate:yyyyMMdd}-{cycleName}";
        using SHA1 sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
