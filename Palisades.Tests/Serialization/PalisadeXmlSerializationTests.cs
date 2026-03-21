using System.IO;
using Palisades.Model;
using Palisades.Serialization;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests.Serialization
{
    public class PalisadeXmlSerializationTests
    {
        [Fact]
        public void SharedSerializer_UsesCentralPalisadeModelSerializer()
        {
            Assert.Same(PalisadeXmlSerialization.PalisadeModelSerializer, ViewModelBase.SharedSerializer);
        }

        [Fact]
        public void StandardPalisadeModel_WithLnkShortcut_RoundTripsThroughCentralSerializer()
        {
            var model = new StandardPalisadeModel { Name = "Test" };
            model.Shortcuts.Add(new LnkShortcut("App", @"C:\icon.ico", @"C:\app.exe"));

            using var writer = new StringWriter();
            PalisadeXmlSerialization.PalisadeModelSerializer.Serialize(writer, model);
            using var reader = new StringReader(writer.ToString());
            var roundTrip = (StandardPalisadeModel)PalisadeXmlSerialization.PalisadeModelSerializer.Deserialize(reader)!;

            Assert.Equal("Test", roundTrip.Name);
            Assert.Single(roundTrip.Shortcuts);
            var sc = Assert.IsType<LnkShortcut>(roundTrip.Shortcuts[0]);
            Assert.Equal("App", sc.Name);
            Assert.Equal(@"C:\app.exe", sc.UriOrFileAction);
        }
    }
}
