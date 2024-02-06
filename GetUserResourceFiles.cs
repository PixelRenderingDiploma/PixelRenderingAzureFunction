using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace PixelRenderer_AzureFunctions
{
    public class GetUserResourceFiles
    {
        private readonly ILogger<GetUserResourceFiles> _logger;

        public GetUserResourceFiles(ILogger<GetUserResourceFiles> logger)
        {
            _logger = logger;
        }

        [Function("GetUserResourceFiles")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger GetUserResourceFiles function processed a request.");

            var user = await Helper.ValidateAndExtractUserAsync(req);
            var containerName = $"userfiles-{user.Subject}";

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            var blobServiceClient = new BlobServiceClient(connectionString);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var directories = new Dictionary<string, (string Path, List<BlobItem> Files)>();
            var files = new List<BlobItem>();

            var blobPrefix = req.Query["blobPrefix"];
            await foreach (var blob in containerClient.GetBlobsAsync(prefix: blobPrefix))
            {
                var pathSegments = blob.Name.Split('/');

                if (pathSegments.Length > 1)
                {
                    var virtualDir = string.Join("/", pathSegments, 0, pathSegments.Length - 1);
                    if (!directories.ContainsKey(virtualDir))
                        directories[virtualDir] = (virtualDir, new List<BlobItem>());

                    directories[virtualDir].Files.Add(blob);
                }
                else
                {
                    files.Add(blob);
                }
            }

            var responseObject = new
            {
                directories = directories.Values.Select(d => new { d.Path, d.Files }).ToList(),
                files
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(responseObject));
            return response;
        }
    }
}
