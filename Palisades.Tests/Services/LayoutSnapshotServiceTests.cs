using Palisades.Model;
using Xunit;

namespace Palisades.Tests.Services
{
    public class LayoutSnapshotTests
    {
        [Fact]
        public void LayoutSnapshot_DefaultValues()
        {
            var snapshot = new LayoutSnapshot();
            Assert.NotNull(snapshot.Id);
            Assert.NotNull(snapshot.Entries);
            Assert.Empty(snapshot.Entries);
        }

        [Fact]
        public void SnapshotEntry_StoresFields()
        {
            var entry = new SnapshotEntry
            {
                PalisadeIdentifier = "abc",
                GroupId = "grp",
                TabOrder = 1
            };
            Assert.Equal("abc", entry.PalisadeIdentifier);
            Assert.Equal("grp", entry.GroupId);
            Assert.Equal(1, entry.TabOrder);
        }
    }
}
