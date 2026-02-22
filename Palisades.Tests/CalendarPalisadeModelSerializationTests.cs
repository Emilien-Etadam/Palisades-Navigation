using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Palisades.Model;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests
{
    public class CalendarPalisadeModelSerializationTests
    {
        [Fact]
        public void CalendarPalisadeModel_RoundTrip_PreservesCalendarIds()
        {
            var model = new CalendarPalisadeModel
            {
                Name = "Test Calendar",
                CalDAVBaseUrl = "https://zimbra1.mail.ovh.net/dav/user@domain/",
                CalDAVUsername = "user@domain",
                CalDAVPassword = "encrypted-data",
                CalendarIds = new List<string>
                {
                    "/dav/user@domain/Calendar",
                    "/dav/user@domain/Work"
                },
                DaysToShow = 14
            };

            var serializer = new XmlSerializer(typeof(CalendarPalisadeModel));
            using var writer = new StringWriter();
            serializer.Serialize(writer, model);
            var xml = writer.ToString();

            using var reader = new StringReader(xml);
            var deserialized = (CalendarPalisadeModel)serializer.Deserialize(reader)!;

            Assert.Equal(model.Name, deserialized.Name);
            Assert.Equal(model.CalDAVBaseUrl, deserialized.CalDAVBaseUrl);
            Assert.Equal(model.CalDAVUsername, deserialized.CalDAVUsername);
            Assert.Equal(model.CalDAVPassword, deserialized.CalDAVPassword);
            Assert.Equal(model.CalendarIds.Count, deserialized.CalendarIds.Count);
            Assert.Equal(model.CalendarIds[0], deserialized.CalendarIds[0]);
            Assert.Equal(model.CalendarIds[1], deserialized.CalendarIds[1]);
            Assert.Equal(model.DaysToShow, deserialized.DaysToShow);
        }

        [Fact]
        public void CalendarPalisadeModel_RoundTrip_EmptyCalendarIds()
        {
            var model = new CalendarPalisadeModel
            {
                Name = "Empty",
                CalendarIds = new List<string>()
            };

            var serializer = new XmlSerializer(typeof(CalendarPalisadeModel));
            using var writer = new StringWriter();
            serializer.Serialize(writer, model);

            using var reader = new StringReader(writer.ToString());
            var deserialized = (CalendarPalisadeModel)serializer.Deserialize(reader)!;

            Assert.NotNull(deserialized.CalendarIds);
            Assert.Empty(deserialized.CalendarIds);
        }

        /// <summary>Vérifie que le round-trip avec le sérialiseur partagé (production) préserve CalendarIds.</summary>
        [Fact]
        public void CalendarPalisadeModel_SharedSerializer_RoundTrip_PreservesCalendarIds()
        {
            var model = new CalendarPalisadeModel
            {
                Name = "Test Calendar",
                CalDAVBaseUrl = "https://example.com/dav/",
                CalendarIds = new List<string> { "/dav/cal1", "/dav/cal2" },
                DaysToShow = 7
            };

            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            var xml = writer.ToString();

            using var reader = new StringReader(xml);
            var deserialized = (CalendarPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;

            Assert.Equal(model.Name, deserialized.Name);
            Assert.Equal(model.CalendarIds.Count, deserialized.CalendarIds.Count);
            Assert.Equal(model.CalendarIds[0], deserialized.CalendarIds[0]);
            Assert.Equal(model.CalendarIds[1], deserialized.CalendarIds[1]);
        }
    }

    public class TaskPalisadeModelSerializationTests
    {
        [Fact]
        public void TaskPalisadeModel_RoundTrip_PreservesFields()
        {
            var model = new TaskPalisadeModel
            {
                Name = "Test Tasks",
                CalDAVUrl = "https://zimbra1.mail.ovh.net/dav/user@domain/Tasks",
                CalDAVUsername = "user@domain",
                CalDAVPassword = "encrypted-data",
                TaskListId = "Tasks"
            };

            var serializer = new XmlSerializer(typeof(TaskPalisadeModel));
            using var writer = new StringWriter();
            serializer.Serialize(writer, model);

            using var reader = new StringReader(writer.ToString());
            var deserialized = (TaskPalisadeModel)serializer.Deserialize(reader)!;

            Assert.Equal(model.Name, deserialized.Name);
            Assert.Equal(model.CalDAVUrl, deserialized.CalDAVUrl);
            Assert.Equal(model.CalDAVUsername, deserialized.CalDAVUsername);
            Assert.Equal(model.CalDAVPassword, deserialized.CalDAVPassword);
            Assert.Equal(model.TaskListId, deserialized.TaskListId);
        }
    }

    public class StandardPalisadeModelSerializationTests
    {
        [Fact]
        public void StandardPalisadeModel_SharedSerializer_RoundTrip_PreservesShortcuts()
        {
            var model = new StandardPalisadeModel
            {
                Name = "Standard",
                Width = 400,
                Height = 300
            };
            model.Shortcuts.Add(new LnkShortcut("Link1", "", "C:\\Target\\app.exe"));
            model.Shortcuts.Add(new UrlShortcut("Url1", "", "https://example.com"));

            using var writer = new StringWriter();
            ViewModelBase.SharedSerializer.Serialize(writer, model);
            var xml = writer.ToString();

            using var reader = new StringReader(xml);
            var deserialized = (StandardPalisadeModel)ViewModelBase.SharedSerializer.Deserialize(reader)!;

            Assert.Equal(model.Name, deserialized.Name);
            Assert.Equal(2, deserialized.Shortcuts.Count);
            Assert.IsType<LnkShortcut>(deserialized.Shortcuts[0]);
            Assert.IsType<UrlShortcut>(deserialized.Shortcuts[1]);
            Assert.Equal("Link1", deserialized.Shortcuts[0].Name);
            Assert.Equal("Url1", deserialized.Shortcuts[1].Name);
        }
    }
}
