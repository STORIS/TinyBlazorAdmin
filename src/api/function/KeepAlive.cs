using Cloud5mins.domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace UrlShortner.function
{
    public class KeepAlive
    {
        private const string WebsiteHostname = "WEBSITE_HOSTNAME";

        private NLogWrapper _logger;

        public KeepAlive(ILoggerFactory loggerFactory, AdminApiSettings settings)
        {
            _logger = new NLogWrapper(LoggerType.UrlPurger, settings);
        }

        [Function("KeepAlive")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] MyInfo myTimer)
        {
            var hostName = Environment.GetEnvironmentVariable(WebsiteHostname);
            if (string.IsNullOrEmpty(hostName))
            {
                _logger.Log(NLog.LogLevel.Error, "Environment variable {WebsiteHostname} does not exist or has no value.", WebsiteHostname);
                return;
            }

            var protocol = "https";
#if DEBUG
            protocol = "http";
#endif
            var urlShortenerEndpoint = $"{protocol}://{hostName}/api/urlshortener/";
            var httpClient = new HttpClient();

            try
            {
                var result = await httpClient.GetAsync(urlShortenerEndpoint);
                if (result.IsSuccessStatusCode)
                {
                    _logger.Log(NLog.LogLevel.Info, "Keep alive for {urlShortenerEndpoint} was successfull", urlShortenerEndpoint);
                }
                else
                {
                    _logger.Log(NLog.LogLevel.Warn, "Keep alive for {urlShortenerEndpoint} failed. HTTP Status Code = {statusCode}",
                        urlShortenerEndpoint, result.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.Log(NLog.LogLevel.Error, "An unexpected error was encountered: {message}", ex.Message);
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
