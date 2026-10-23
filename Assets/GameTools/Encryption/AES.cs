using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GameTools.Encryption
{
    public static class AES
    {
        private const int saltSize = 16;
        private const int iterations = 1000;

        public static string Encrypt(string text, string password)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (password == null) throw new ArgumentNullException(nameof(password));
           
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
    
            var salt = new byte[saltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            GenerateMaterials(password, salt, out byte[] encKey, out byte[] iv, out byte[] hmacKey);
           
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var encryptor = aes.CreateEncryptor(encKey, iv);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(textBytes, 0, textBytes.Length);
            cryptoStream.FlushFinalBlock();
           
            var ciphertext = memoryStream.ToArray();
            var headerAndCiphertext = ConcatBytes(salt, ciphertext);
            
            using var hmac = new HMACSHA256(hmacKey);
            byte[] tag = hmac.ComputeHash(headerAndCiphertext);

            byte[] output = ConcatBytes(headerAndCiphertext, tag);
            CryptographicOperations.ZeroMemory(encKey);
            CryptographicOperations.ZeroMemory(hmacKey);

            return Convert.ToBase64String(output);
        }

        public static string Decrypt(string text, string password)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (password == null) throw new ArgumentNullException(nameof(password));

            var allBytes = Convert.FromBase64String(text);
            if (allBytes.Length < saltSize + 32)
                throw new ArgumentException("Invalid encrypted data.");

            int signedLength = allBytes.Length - 32;
            var salt = new byte[saltSize];
            var ciphertext = new byte[allBytes.Length - saltSize - 32];
            var actualTag = new byte[32];

            Buffer.BlockCopy(allBytes, 0, salt, 0, saltSize);
            Buffer.BlockCopy(allBytes, saltSize, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(allBytes, signedLength, actualTag, 0, 32);

            GenerateMaterials(password, salt, out byte[] encKey, out byte[] iv, out byte[] macKey);

            using var hmac = new HMACSHA256(macKey);
            byte[] expectedTag = hmac.ComputeHash(allBytes, 0, signedLength);
            bool valid = CryptographicOperations.FixedTimeEquals(expectedTag, actualTag);
            CryptographicOperations.ZeroMemory(expectedTag);
            CryptographicOperations.ZeroMemory(macKey);

            if (!valid)
            {
                CryptographicOperations.ZeroMemory(encKey);
                throw new CryptographicException("Authentication failed.");
            }

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var decryptor = aes.CreateDecryptor(encKey, iv);
            using var memoryStream = new MemoryStream(ciphertext);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            CryptographicOperations.ZeroMemory(encKey);
            return result;
        }

        private static void GenerateMaterials(string password, byte[] salt, out byte[] encKey, out byte[] iv, out byte[] hmacKey)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            encKey = kdf.GetBytes(32);
            iv = kdf.GetBytes(16);
            hmacKey = kdf.GetBytes(32);
        }

        private static byte[] ConcatBytes(byte[] first, byte[] second)
        {
            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }
        
        private static byte[] ConcatBytes(byte[] first, byte[] second, byte[] third)
        {
            byte[] result = new byte[first.Length+second.Length+third.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, result, first.Length+second.Length, third.Length);
            return result;
        }
    }
}