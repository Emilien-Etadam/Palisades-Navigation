using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Palisades.Helpers
{
    public static class CredentialEncryptor
    {
        private static readonly byte[] Salt = Encoding.ASCII.GetBytes("PalisadesSalt2024");
        private static readonly int Iterations = 10000;
        private static readonly int KeySize = 256;
        
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
                
            using (var deriveBytes = new Rfc2898DeriveBytes(password, Salt, Iterations))
            {
                var key = deriveBytes.GetBytes(KeySize / 8);
                var iv = deriveBytes.GetBytes(16);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        using (var streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        
                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
        }
        
        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
                
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                
                using (var deriveBytes = new Rfc2898DeriveBytes(password, Salt, Iterations))
                {
                    var key = deriveBytes.GetBytes(KeySize / 8);
                    var iv = deriveBytes.GetBytes(16);
                    
                    using (var aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;
                        
                        using (var memoryStream = new MemoryStream(cipherBytes))
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(aes.Key, aes.IV), CryptoStreamMode.Read))
                        using (var streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                // En cas d'échec du déchiffrement, retourner une chaîne vide
                return string.Empty;
            }
        }
        
        public static string GenerateEncryptionKey()
        {
            // Générer une clé de chiffrement aléatoire pour l'utilisateur
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[32]; // 256 bits
                rng.GetBytes(keyBytes);
                return Convert.ToBase64String(keyBytes);
            }
        }
    }
}