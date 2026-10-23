using System;
using System.Text;
namespace GameTools.Encryption
{
    public static class SHA256
    {
        public static string ComputeHash(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            byte[] inputBytes = Encoding.UTF8.GetBytes(text);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}