using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure.Storage;

namespace PixelRenderer_AzureFunctions
{
    public class GetUserBlobSasUrl
    {
        private readonly ILogger _logger;

        public GetUserBlobSasUrl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetUserBlobSasUrl>();
        }

        [Function("GetUserBlobSasUrl")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function GetUserBlobSasUrl processed a request.");
            
            var user = await Helper.ValidateAndExtractUserAsync(req);
            var containerName = $"userfiles-{user.Subject}";

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            var blobServiceClient = new BlobServiceClient(connectionString);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobPath = req.Query["blobPath"];
            var expiryDate = DateTimeOffset.UtcNow.AddMinutes(10);

            var blobClient = containerClient.GetBlobClient(blobPath);
            var sasUrl = blobClient.GenerateSasUri(
                BlobSasPermissions .Read | BlobSasPermissions .Write | BlobSasPermissions .Delete,
                expiryDate
            );

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(sasUrl.ToString());
            return response;
        }
    }
}
