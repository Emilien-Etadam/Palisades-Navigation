using Palisades.Model;
using Xunit;

namespace Palisades.Tests.Models
{
    public class PalisadeModelMigrationTests
    {
        [Fact]
        public void ToConcreteModel_StandardType_ReturnsStandardPalisadeModel()
        {
            var legacy = new PalisadeModel { Name = "Old", Type = PalisadeType.Standard, Width = 400, Height = 300 };
            var result = PalisadeModelMigration.ToConcreteModel(legacy);
            Assert.IsType<StandardPalisadeModel>(result);
            Assert.Equal("Old", result.Name);
            Assert.Equal(400, result.Width);
        }

        [Fact]
        public void ToConcreteModel_FolderPortalType_ReturnsFolderPortalModel()
        {
            var legacy = new PalisadeModel { Name = "Folder", Type = PalisadeType.FolderPortal, RootPath = @"C:\Test" };
            var result = PalisadeModelMigration.ToConcreteModel(legacy);
            Assert.IsType<FolderPortalModel>(result);
            Assert.Equal(@"C:\Test", ((FolderPortalModel)result).RootPath);
        }

        [Fact]
        public void ToConcreteModel_TaskType_ReturnsTaskPalisadeModel()
        {
            var legacy = new PalisadeModel { Name = "Tasks", Type = PalisadeType.TaskPalisade, CalDAVUrl = "https://example.com/dav/" };
            var result = PalisadeModelMigration.ToConcreteModel(legacy);
            Assert.IsType<TaskPalisadeModel>(result);
            Assert.Equal("https://example.com/dav/", ((TaskPalisadeModel)result).CalDAVUrl);
        }

        [Fact]
        public void ToConcreteModel_PreservesCommonProperties()
        {
            var legacy = new PalisadeModel
            {
                Identifier = "test-id",
                Name = "Test",
                FenceX = 100,
                FenceY = 200,
                Width = 500,
                Height = 400
            };
            var result = PalisadeModelMigration.ToConcreteModel(legacy);
            Assert.Equal("test-id", result.Identifier);
            Assert.Equal(100, result.FenceX);
            Assert.Equal(200, result.FenceY);
            Assert.Equal(500, result.Width);
            Assert.Equal(400, result.Height);
        }
    }
}
