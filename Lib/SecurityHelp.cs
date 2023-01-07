using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Lib;

public class SecurityHelp
{
    #region 摘要

    /// <summary>
    /// MD5摘要,返回32位长度小写16进制字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Hex32_Md5(string str)
    {
        return PlainToDigest(str, 0);
    }

    /// <summary>
    /// Sha1摘要,返回40位长度小写16进制字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Hex40_SHA1(string str)
    {
        return PlainToDigest(str, 1);
    }

    /// <summary>
    /// Sha256摘要,返回64位长度小写16进制字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Hex64_SHA256(string str)
    {
        return PlainToDigest(str, 2);
    }
    /// <summary>
    /// Sha384摘要,返回96位长度小写16进制字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Hex96_SHA384(string str)
    {
        return PlainToDigest(str, 3);
    }
    /// <summary>
    /// Sha512摘要,返回128位长度小写16进制字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Hex128_SHA512(string str)
    {
        return PlainToDigest(str, 4);
    }

    /// <summary>
    /// 生产明文的摘要,空字符串或者null时不生成并返回null
    /// </summary>
    /// <returns></returns>
    private static string PlainToDigest(string plain, int type)
    {
        if (string.IsNullOrEmpty(plain))
            return null;
        byte[] buffer = Encoding.UTF8.GetBytes(plain);
        byte[] data;
        if (type == 0)
        {
            data = MD5.Create().ComputeHash(buffer);
        }
        else if (type == 1)
        {
            data = SHA1.Create().ComputeHash(buffer);
        }
        else if (type == 2)
        {
            data = SHA256.Create().ComputeHash(buffer);
        }
        else if (type == 3)
        {
            data = SHA384.Create().ComputeHash(buffer);
        }
        else if (type == 4)
        {
            data = SHA512.Create().ComputeHash(buffer);
        }
        else
            return null;
        // Console.WriteLine(data.Length);
        return Convert.ToHexString(data).ToLower();
    }
    #endregion

    #region AES
    // doc AES https://docs.microsoft.com/zh-cn/dotnet/api/system.security.cryptography.aes?view=net-6.0

    /// <summary>
    /// 用于 AES 算法的初始化向量
    /// </summary>
    private static readonly byte[] IVaes = { 10, 37, 9, 56, 87, 116, 108, 52, 72, 44, 123, 40, 51, 14, 99, 51 };


    /// <summary>
    /// aes 明文字符串转密文字符串,密文字节转换成16进制字符串
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static string PlainToCipherHex_Aes(string plainText, string keyStr)
    {
        var cipherBytes = EncryptStringToBytes_Aes(plainText, keyStr);
        if (cipherBytes == null) return null;
        return Convert.ToHexString(cipherBytes);
    }

    /// <summary>
    /// aes 密文16进制字符串转明文字符串
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static string CipherHexToPlain_Aes(string cipherHexText, string keyStr)
    {
        byte[] cipherBytes = Convert.FromHexString(cipherHexText);
        return DecryptStringFromBytes_Aes(cipherBytes, keyStr);
    }

    /// <summary>
    /// AES字符串转byte[],16位长度或者倍数密钥
    /// </summary>
    /// <param name="txt">明文字符串</param>
    /// <param name="keyStr">密钥字符串:16位</param>
    /// <returns>返回加密后的密文字节数组</returns>
    public static byte[] EncryptStringToBytes_Aes(string plainText, string keyStr)
    {
        if (plainText == null || plainText.Length <= 0)
            return null;
        if (keyStr == null || keyStr.Length % 16 != 0)
            return null;

        // Create an Aes object
        // with the specified key and IV.
        byte[] encrypted;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(keyStr);
            aesAlg.IV = IVaes;

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for encryption.
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new(csEncrypt))
            {
                //Write all data to the stream.
                swEncrypt.Write(plainText);
            }

            encrypted = msEncrypt.ToArray();
        }
        // Return the encrypted bytes from the memory stream.
        return encrypted;
    }


    /// <summary>
    /// AES密文字符串的字节数组转明文,16位长度或者倍数密钥
    /// </summary>
    /// <param name="cipherText">密文字符串</param>
    /// <param name="keyStr"></param>
    /// <returns></returns>
    public static string DecryptStringFromBytes_Aes(byte[] cipherText, string keyStr)
    {
        // Check arguments.
        if (cipherText == null || cipherText.Length <= 0)
            return null;
        if (keyStr == null || keyStr.Length % 16 != 0)
            return null;

        // Declare the string used to hold
        // the decrypted text.
        string plaintext = null;

        // Create an Aes object
        // with the specified key and IV.
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(keyStr);
            aesAlg.IV = IVaes;

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption.
            using MemoryStream msDecrypt = new(cipherText);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);

            // Read the decrypted bytes from the decrypting stream
            // and place them in a string.
            plaintext = srDecrypt.ReadToEnd();
        }

        return plaintext;
    }
    #endregion

}
