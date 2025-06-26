#region 4. 安全验证（HMAC-SHA256）

using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Device;

public static class SecurityUtil
{
    private static string _secretKey = "your-secure-key";

    public static string GenerateRequestSignature(byte[] payload)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        byte[] rawData = Encoding.UTF8.GetBytes($"{payload}|{timestamp}|{deviceId}");
        
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
        {
            byte[] hash = hmac.ComputeHash(rawData);
            return Convert.ToBase64String(hash);
        }
    }

    public static bool ValidateResponseSignature(byte[] data, string receivedSignature)
    {
        if (string.IsNullOrEmpty(receivedSignature)) return false;
        
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
        {
            byte[] hash = hmac.ComputeHash(data);
            string computedSignature = Convert.ToBase64String(hash);
            return computedSignature.Equals(receivedSignature);
        }
    }
}
#endregion