using System;
using System.Threading.Tasks;
using Xunit;

namespace Palisades.Services.Tests
{
    public class CalDAVServiceTests
    {
        [Fact]
        public async Task GetTaskListsAsync_WithValidCredentials_ReturnsTaskLists()
        {
            // Arrange
            var service = new CalDAVService(
                "https://caldav.example.com/caldav.php",
                "testuser",
                "testpassword");

            // Act
            var taskLists = await service.GetTaskListsAsync();

            // Assert
            Assert.NotNull(taskLists);
            // Note: This test would need a mock CalDAV server for real testing
        }

        [Fact]
        public async Task CreateTaskAsync_WithValidTask_ReturnsTaskWithCalDAVId()
        {
            // Arrange
            var service = new CalDAVService(
                "https://caldav.example.com/caldav.php",
                "testuser",
                "testpassword");
            var task = new Model.CalDAVTask("Test Task")
            {
                Description = "Test Description",
                DueDate = DateTime.Today.AddDays(1)
            };

            // Act
            var result = await service.CreateTaskAsync("personal", task);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.CalDAVId);
            // Note: This test would need a mock CalDAV server for real testing
        }

        [Fact]
        public async Task EncryptDecrypt_Credentials_RoundTrip()
        {
            // Arrange
            var originalPassword = "MySecretPassword123!";
            var encryptionKey = "TestEncryptionKey2024";

            // Act
            var encrypted = Helpers.CredentialEncryptor.Encrypt(originalPassword, encryptionKey);
            var decrypted = Helpers.CredentialEncryptor.Decrypt(encrypted, encryptionKey);

            // Assert
            Assert.NotEqual(originalPassword, encrypted); // Should be encrypted
            Assert.Equal(originalPassword, decrypted); // Should decrypt back to original
        }

        [Fact]
        public void Encrypt_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var emptyString = string.Empty;
            var encryptionKey = "TestEncryptionKey2024";

            // Act
            var result = Helpers.CredentialEncryptor.Encrypt(emptyString, encryptionKey);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Decrypt_InvalidData_ReturnsEmptyString()
        {
            // Arrange
            var invalidData = "InvalidBase64Data!!!";
            var encryptionKey = "TestEncryptionKey2024";

            // Act
            var result = Helpers.CredentialEncryptor.Decrypt(invalidData, encryptionKey);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}