using System;
using System.Security.Cryptography;
using System.Text;

namespace Converge.Data.Services
{
    public static class CryptoUtils
    {
        public static string GenerateSalt(int length = 32)
        {
            var saltBytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static byte[] DeriveKey(string password, string base64Salt, int iterations = 100_000)
        {
            var saltBytes = Convert.FromBase64String(base64Salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }

        public static string Encrypt(string plainText, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var combined = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        public static string Decrypt(string encryptedText, byte[] key)
        {
            try
            {
                var combined = Convert.FromBase64String(encryptedText);
                var iv = new byte[16];
                var cipher = new byte[combined.Length - iv.Length];

                Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(combined, iv.Length, cipher, 0, cipher.Length);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Decryption failed due to incorrect password or corrupted data.");
            }
        }
    }
}
