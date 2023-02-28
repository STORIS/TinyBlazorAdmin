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
        private NLogWrapper logger;
        private readonly AdminApiSettings _adminApiSettings;

        public UrlPurger(ILoggerFactory loggerFactory, AdminApiSettings settings)
        {
            logger = new NLogWrapper(LoggerType.UrlPurger, settings);
            _adminApiSettings = settings;
        }

        [Function("UrlPurger")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] MyInfo myTimer)
        {
            logger.Log(NLog.LogLevel.Info, "C# Timer trigger function executed at {trigger.current}", DateTime.Now.ToString());
            logger.Log(NLog.LogLevel.Info, "Next timer scheduled for {trigger.next}", myTimer.ScheduleStatus.Next.ToString());

            var storageTableHelper = new StorageTableHelper(_adminApiSettings.UlsDataStorage);
            var urlsToPurge = await storageTableHelper.GetShortUrlEntitiesToPurge(7);
            foreach (var urlToPurge in urlsToPurge)
            {

                logger.Log(NLog.LogLevel.Info, "Deleting ShortUrl: {urlToPurge.ShortUrl} | Title: {urlToPurge.Title} | CreatedAt: {urlToPurge.Timestamp}", urlToPurge.RowKey, urlToPurge.Title, urlToPurge.Timestamp.ToString());

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
