using System.IO;
using Palisades.Model;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests.Models
{
    public class GroupIdTabOrderSerializationTests
    {
        [Fact]
        public void StandardPalisadeModel_GroupId_Persisted()
        {
            var model = new StandardPalisadeModel { Name = "Grouped", GroupId = "group-abc", TabOrder = 2 };
            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            using var reader = new StringReader(writer.ToString());
            var d = (StandardPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;
            Assert.Equal("group-abc", d.GroupId);
            Assert.Equal(2, d.TabOrder);
        }

        [Fact]
        public void NullGroupId_DeserializesToNull()
        {
            var model = new StandardPalisadeModel { Name = "Solo", GroupId = null, TabOrder = 0 };
            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            using var reader = new StringReader(writer.ToString());
            var d = (StandardPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;
            Assert.Null(d.GroupId);
            Assert.Equal(0, d.TabOrder);
        }
    }
}
