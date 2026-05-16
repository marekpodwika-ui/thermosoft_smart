using System.Collections.ObjectModel;
using thermosoft.Models;
using thermosoft.RestClient;
using Newtonsoft.Json;

namespace thermosoft.Services
{
    public class HistoryService
    {
        /// <returns>(punkty historii, kod b³êdu: 0=OK, 1=brak po³¹czenia, 2=z³y format/endpoint)</returns>
        public async Task<(List<HistoryPoint> Points, int Error)> GetHistoryAsync(bool useWebSocket, Employee employee, int dzien)
        {
            RestClient<Employee> restClient = new RestClient<Employee>();

            var query = new Employee
            {
                Id = employee.Id,
                Za = employee.Za,
                St = employee.St,
                Te = employee.Te,
                Na = $"#{dzien}"
            };

            List<HistoryRecord> records = null;

            if (useWebSocket)
                records = await restClient.GetHistoryWs(query);
            else
                records = await restClient.GetHistoryTcp(query);

            int error = RestClient<Employee>.ErrorWebsocket;

            if (records == null)
                return (new List<HistoryPoint>(), error);

            var points = records
                .Select(r => HistoryPoint.Parse(r))
                .Where(p => p != null)
                .ToList();

            return (points, error);
        }
    }
}
