using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Palisades.Services
{
    public interface ICalDAVClient : System.IDisposable
    {
        Task<XDocument> PropfindAsync(string href, int depth, string? requestBody = null);
        Task<XDocument> ReportAsync(string href, string requestBody);
        Task<string?> PutAsync(string href, string icalData, string? etag = null);
        Task DeleteAsync(string href, string? etag = null);
        Task<List<CalDAVCalendarInfo>> DiscoverCalendarsAsync();
    }
}
