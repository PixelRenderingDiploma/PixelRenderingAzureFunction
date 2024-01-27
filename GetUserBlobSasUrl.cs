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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function GetUserBlobSasUrl processed a request.");
            
            var user = await Helper.ValidateAndExtractUserAsync(req);
            var containerName = $"userfiles-{user.Subject}";

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            var blobServiceClient = new BlobServiceClient(connectionString);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobPath = req.Query["blobPath"];
            var startDate = DateTimeOffset.UtcNow.AddMinutes(-1);
            var expiryDate = DateTimeOffset.UtcNow.AddMinutes(10);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobPath,
                Resource = "b", // "b" for blob, "c" for container
                StartsOn = startDate,
                ExpiresOn = expiryDate
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write | BlobSasPermissions.Delete);

            var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(blobServiceClient.AccountName, "Unp8Muly8GPMmN24Oc61wbCwBCv+EpObRuhUf9mAUiPHCYnm9+ws12HVnTmlkTRo5WQPDYqNZ6MT+AStLFlKzQ==")).ToString();

            var frontDoorUrl = Environment.GetEnvironmentVariable("STORAGE_URL");
            var sasUrl = $"{frontDoorUrl}/{containerName}/{blobPath}?{sasToken}";

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(sasUrl);
            return response;
        }
    }
}
