using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace PixelRenderer_AzureFunctions
{
    public class DeleteUserResourceFile
    {
        private readonly ILogger<DeleteUserResourceFile> _logger;

        public DeleteUserResourceFile(ILogger<DeleteUserResourceFile> logger)
        {
            _logger = logger;
        }

        [Function("DeleteUserResourceFile")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger DeleteUserResourceFile function processed a request.");

            var blobPath = req.Query["blobPath"];

            var user = await Helper.ValidateAndExtractUserAsync(req);
            var containerName = $"userfiles-{user.Subject}";

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            var blobServiceClient = new BlobServiceClient(connectionString);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            await blobClient.DeleteIfExistsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
