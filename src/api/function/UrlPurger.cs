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
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = false)] MyInfo myTimer)
        {
            logger.Log(NLog.LogLevel.Info, "C# Timer trigger function executed at {0}", DateTime.Now.ToString());
            logger.Log(NLog.LogLevel.Info, "Next timer scheduled for {0}", myTimer.ScheduleStatus.Next.ToString());

            var storageTableHelper = new StorageTableHelper(_adminApiSettings.UlsDataStorage);
            var urlsToPurge = await storageTableHelper.GetShortUrlEntitiesToPurge(7);
            foreach (var urlToPurge in urlsToPurge)
            {

                logger.Log(NLog.LogLevel.Info, "Deleting ShortUrl: {0} | Title: {1} | CreatedAt: {2}",
                    urlToPurge.RowKey, urlToPurge.Title, urlToPurge.Timestamp.ToString());

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
