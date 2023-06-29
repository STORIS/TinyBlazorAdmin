/*
```c#
Input:

    {
        // [Required] The url you wish to have a short version for
        "url": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio",
        
        // [Optional] Title of the page, or text description of your choice.
        "title": "Quickstart: Create your first function in Azure using Visual Studio"

        // [Optional] the end of the URL. If nothing one will be generated for you.
        "vanity": "azFunc"
    }

Output:
    {
        "shortUrl": "http://c5m.ca/azFunc",
        "longUrl": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio"
    }
*/

using Cloud5mins.AzShortener;
using Cloud5mins.domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud5mins.Function
{

    public class UrlShortener
    {
        private NLogWrapper logger;
        private readonly AdminApiSettings _adminApiSettings;

        public UrlShortener(ILoggerFactory loggerFactory, AdminApiSettings settings)
        {
            logger = new NLogWrapper(LoggerType.UrlShortener, settings);
            _adminApiSettings = settings;
        }

        [Function("UrlShortener")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
            ExecutionContext context
        )
        {
            string userId = string.Empty;
            ShortRequest input;
            var result = new ShortResponse();

            try
            {
                // Validation of the inputs
                if (req == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                using (var reader = new StreamReader(req.Body))
                {
                    var strBody = reader.ReadToEnd();
                    input = JsonSerializer.Deserialize<ShortRequest>(strBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (input == null)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                }

                // If the Url parameter only contains whitespaces or is empty return with BadRequest.
                if (string.IsNullOrWhiteSpace(input.Url))
                {
                    logger.Log(NLog.LogLevel.Warn, "The url parameter can not be empty.");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { message = "The url parameter can not be empty." });
                    return badResponse;
                }

                // Validates if input.url is a valid aboslute url, aka is a complete refrence to the resource, ex: http(s)://google.com
                if (!Uri.IsWellFormedUriString(input.Url, UriKind.Absolute))
                {
                    logger.Log(NLog.LogLevel.Warn, "{0} is not a valid absolute Url. The Url parameter must start with 'http://' or 'https://'.", input.Url);
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { message = $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'https://'." });
                    return badResponse;
                }

                StorageTableHelper stgHelper = new StorageTableHelper(_adminApiSettings.UlsDataStorage);

                string longUrl = input.Url.Trim();
                string vanity = string.IsNullOrWhiteSpace(input.Vanity) ? "" : input.Vanity.Trim();
                string title = string.IsNullOrWhiteSpace(input.Title) ? "" : input.Title.Trim();


                ShortUrlEntity newRow;

                if (!string.IsNullOrEmpty(vanity))
                {
                    newRow = new ShortUrlEntity(longUrl, vanity, title, input.Schedules);
                    if (await stgHelper.IfShortUrlEntityExist(newRow))
                    {
                        logger.Log(NLog.LogLevel.Warn, "The Short URL {0} already exists.", vanity);
                        var badResponse = req.CreateResponse(HttpStatusCode.Conflict);
                        await badResponse.WriteAsJsonAsync(new { message = $"The Short URL {vanity} already exists." });
                        return badResponse;
                    }
                }
                else
                {
                    newRow = new ShortUrlEntity(longUrl, await Utility.GetValidEndUrl(vanity, stgHelper), title, input.Schedules);
                }

                await stgHelper.SaveShortUrlEntity(newRow);

                var host = string.IsNullOrEmpty(_adminApiSettings.customDomain) ? req.Url.Host : _adminApiSettings.customDomain.ToString();
                result = new ShortResponse(host, newRow.Url, newRow.RowKey, newRow.Title);

                logger.Log(NLog.LogLevel.Info, "Short Url {0} for url {1} created", newRow.RowKey, longUrl);
            }
            catch (Exception ex)
            {
                logger.Log(NLog.LogLevel.Error, "An unexpected error was encountered: {0}", ex.Message);
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { message = ex.Message });
                return badResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var serializedResult = JsonSerializer.Serialize<ShortResponse>(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await response.WriteStringAsync(serializedResult);

            return response;
        }
    }
}
