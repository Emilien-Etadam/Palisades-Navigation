using System;
using System.Security.Cryptography;
using System.Text;

namespace Palisades.Helpers
{
    /// <summary>
    /// Chiffrement des secrets (mots de passe) via DPAPI (Phase 3.2).
    /// Déchiffrable uniquement par la session Windows du même utilisateur.
    /// </summary>
    public static class CredentialEncryptor
    {
        private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("Palisades.v1");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                var protectedBytes = ProtectedData.Protect(bytes, OptionalEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
            try
            {
                var protectedBytes = Convert.FromBase64String(cipherText);
                var bytes = ProtectedData.Unprotect(protectedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Conservé pour compatibilité des signatures ; le second paramètre est ignoré (DPAPI n'utilise pas de clé utilisateur).
        /// </summary>
        [Obsolete("Utiliser Encrypt(string) avec DPAPI.")]
        public static string Encrypt(string plainText, string _) => Encrypt(plainText);

        /// <summary>
        /// Conservé pour compatibilité des signatures ; le second paramètre est ignoré.
        /// </summary>
        [Obsolete("Utiliser Decrypt(string) avec DPAPI.")]
        public static string Decrypt(string cipherText, string _) => Decrypt(cipherText);
    }
}
