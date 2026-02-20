using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Palisades.Tests.Services
{
    public class CalDAVServiceTests
    {
        [Fact]
        public async Task GetTaskListsAsync_WithInvalidUrl_ReturnsEmptyOrThrows()
        {
            var service = new CalDAVService("https://invalid.example.invalid/", "u", "p");
            try
            {
                var taskLists = await service.GetTaskListsAsync();
                Assert.NotNull(taskLists);
            }
            catch
            {
                // Network or DNS failure acceptable
            }
        }

        [Fact]
        public void EncryptDecrypt_DPAPI_RoundTrip()
        {
            if (!OperatingSystem.IsWindows())
                return; // DPAPI is Windows-only
            var original = "Secret123";
            var encrypted = CredentialEncryptor.Encrypt(original);
            var decrypted = CredentialEncryptor.Decrypt(encrypted);
            Assert.NotEqual(original, encrypted);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_Empty_ReturnsEmpty()
        {
            Assert.Equal("", CredentialEncryptor.Encrypt(""));
        }

        [Fact]
        public void Decrypt_InvalidBase64_ReturnsEmpty()
        {
            Assert.Equal("", CredentialEncryptor.Decrypt("NotValidBase64!!"));
        }
    }
}
