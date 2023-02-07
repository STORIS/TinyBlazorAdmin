using Cloud5mins.AzShortener;
using Cloud5mins.domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace UrlPurger.function
{

    public class UrlPurger
    {
        private readonly ILogger _logger;
        private readonly AdminApiSettings _adminApiSettings;

        public UrlPurger(ILoggerFactory loggerFactory, AdminApiSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlPurger>();
            _adminApiSettings = settings;
        }

        [Function("UrlPurger")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] MyInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            var storageTableHelper = new StorageTableHelper(_adminApiSettings.UlsDataStorage);
            var urlsToPurge = await storageTableHelper.GetShortUrlEntitiesToPurge(7);
            foreach (var urlToPurge in urlsToPurge)
            {

                _logger.LogInformation($"Deleting ShortUrl: {urlToPurge.ShortUrl} | Title: {urlToPurge.Title} | CreatedAt: {urlToPurge.Timestamp}");

                await storageTableHelper.DeleteShortUrlEntity(urlToPurge);
            }
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
