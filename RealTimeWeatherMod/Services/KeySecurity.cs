using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChillWithYou.EnvSync.Services
{
  /// <summary>
  /// API Key 加密/解密工具类
  /// 防君子不防小人，不要滥用key，不然我会直接停了
  /// </summary>
  internal static class KeySecurity
  {
    private static readonly byte[] _k = { 0x43, 0x68, 0x69, 0x6C, 0x6C, 0x57, 0x69, 0x74, 0x68, 0x59, 0x6F, 0x75, 0x32, 0x30, 0x32, 0x35 };
    private static readonly byte[] _i = { 0x57, 0x65, 0x61, 0x74, 0x68, 0x65, 0x72, 0x4D, 0x6F, 0x64, 0x49, 0x56, 0x38, 0x38, 0x38, 0x38 };

    /// <summary>
    /// 解密 Base64 编码的加密字符串
    /// </summary>
    public static string Decrypt(string encryptedText)
    {
      if (string.IsNullOrEmpty(encryptedText)) return null;

      try
      {
        using (Aes aes = Aes.Create())
        {
          aes.Key = _k;
          aes.IV = _i;
          ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

          byte[] buffer = Convert.FromBase64String(encryptedText);
          using (MemoryStream ms = new MemoryStream(buffer))
          using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
          using (StreamReader sr = new StreamReader(cs))
          {
            return sr.ReadToEnd();
          }
        }
      }
      catch (Exception ex)
      {
        ChillEnvPlugin.Log?.LogError($"[KeySecurity] 解密失败: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// 加密明文字符串为 Base64 编码
    /// </summary>
    public static string Encrypt(string plainText)
    {
      if (string.IsNullOrEmpty(plainText)) return null;

      try
      {
        using (Aes aes = Aes.Create())
        {
          aes.Key = _k;
          aes.IV = _i;
          ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

          using (MemoryStream ms = new MemoryStream())
          {
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs))
            {
              sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
          }
        }
      }
      catch (Exception ex)
      {
        ChillEnvPlugin.Log?.LogError($"[KeySecurity] 加密失败: {ex.Message}");
        return null;
      }
    }
  }
}
