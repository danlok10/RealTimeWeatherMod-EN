# GenerateKey.ps1
# 这是一个 PowerShell 脚本，用于生成加密的 API Key
# 使用方法：
# 1. 在 VS Code 终端中运行: .\GenerateKey.ps1
# 2. 按提示输入您的 API Key
# 3. 脚本会输出加密后的字符串

$code = @"
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class KeyGen
{
    private static readonly byte[] _k = { 0x43, 0x68, 0x69, 0x6C, 0x6C, 0x57, 0x69, 0x74, 0x68, 0x59, 0x6F, 0x75, 0x32, 0x30, 0x32, 0x35 };
    private static readonly byte[] _i = { 0x57, 0x65, 0x61, 0x74, 0x68, 0x65, 0x72, 0x4D, 0x6F, 0x64, 0x49, 0x56, 0x38, 0x38, 0x38, 0x38 };

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;
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
}
"@

Add-Type -TypeDefinition $code -Language CSharp

Write-Host "请输入您的 API Key:" -ForegroundColor Cyan
$apiKey = Read-Host
if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "API Key 不能为空！" -ForegroundColor Red
    exit
}

$encrypted = [KeyGen]::Encrypt($apiKey)
Write-Host "`n----------------------------------------" -ForegroundColor Green
Write-Host "加密成功！请复制下面的字符串：" -ForegroundColor Green
Write-Host "----------------------------------------" -ForegroundColor Green
Write-Host $encrypted -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Green
