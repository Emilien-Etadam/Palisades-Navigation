using Palisades.Helpers;
using Xunit;

namespace Palisades.Tests.Helpers
{
    public class ZimbraOvhDetectionTests
    {
        [Fact]
        public void SuggestFromEmail_WithEmail_ReturnsImapAndCaldav()
        {
            var (imap, caldav) = ZimbraOvhDetection.SuggestFromEmail("user@domain.com");
            Assert.Equal("mail.domain.com", imap);
            Assert.Contains("user@domain.com", caldav);
            Assert.StartsWith("https://", caldav);
            Assert.Contains("/dav/", caldav);
        }

        [Fact]
        public void SuggestFromEmail_Empty_ReturnsDefault()
        {
            var (imap, caldav) = ZimbraOvhDetection.SuggestFromEmail("");
            Assert.Equal("ssl0.ovh.net", imap);
            Assert.Equal("", caldav);
        }
    }
}
