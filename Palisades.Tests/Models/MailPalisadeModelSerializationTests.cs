using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Palisades.Model;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests.Models
{
    public class MailPalisadeModelSerializationTests
    {
        [Fact]
        public void MailPalisadeModel_RoundTrip_PreservesFields()
        {
            var model = new MailPalisadeModel
            {
                Name = "Mail Test",
                ImapHost = "ssl0.ovh.net",
                ImapPort = 993,
                ImapUsername = "user@domain.com",
                ImapPassword = "encrypted",
                MonitoredFolders = new List<string> { "INBOX", "Sent" },
                DisplayMode = MailDisplayMode.CountAndSubjects,
                PollIntervalMinutes = 5,
                WebmailUrl = "https://webmail.example.com"
            };

            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            using var reader = new StringReader(writer.ToString());
            var d = (MailPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;

            Assert.Equal(model.Name, d.Name);
            Assert.Equal(model.ImapHost, d.ImapHost);
            Assert.Equal(model.ImapPort, d.ImapPort);
            Assert.Equal(model.ImapUsername, d.ImapUsername);
            Assert.Equal(model.ImapPassword, d.ImapPassword);
            Assert.True(d.MonitoredFolders.Count >= 2);
            Assert.Contains("INBOX", d.MonitoredFolders);
            Assert.Contains("Sent", d.MonitoredFolders);
            Assert.Equal(MailDisplayMode.CountAndSubjects, d.DisplayMode);
            Assert.Equal(5, d.PollIntervalMinutes);
            Assert.Equal(model.WebmailUrl, d.WebmailUrl);
        }

        [Fact]
        public void MailPalisadeModel_EmptyFolders_RoundTrip()
        {
            var model = new MailPalisadeModel { Name = "Empty", MonitoredFolders = new List<string>() };
            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            using var reader = new StringReader(writer.ToString());
            var d = (MailPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;
            Assert.NotNull(d.MonitoredFolders);
        }
    }
}
