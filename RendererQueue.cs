using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.InteropServices;

namespace PixelRenderer_AzureFunctions
{
    public class RendererQueue
    {
        private readonly ILogger<RendererQueue> _logger;

        public RendererQueue(ILogger<RendererQueue> logger)
        {
            _logger = logger;
        }

        [Function("RendererQueuePost")]
        public async Task<HttpResponseData> RunPost([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "RendererQueue")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP POST trigger function RendererQueue processed a request.");

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            string message = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                string queueName = req.Query["queue"] switch
                {
                    "release" => "renderingqueue",
                    "debug" => "renderingqueuedebug",
                    _ => throw new ArgumentException("Invalid queue name"),
                };

                QueueServiceClient queueServiceClient = new QueueServiceClient(connectionString);
                QueueClient queueClient = queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();

                var queueResponse = await queueClient.SendMessageAsync(message);

                var response = req.CreateResponse((HttpStatusCode)queueResponse.GetRawResponse().Status);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending message to queue");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(ex.Message);
                return response;
            }
        }

        [Function("RendererQueueGet")]
        public async Task<HttpResponseData> RunGet([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RendererQueue")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP GET trigger function RendererQueue processed a request.");

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);

            try
            {
                string queueName = req.Query["queue"] switch
                {
                    "release" => "renderingqueue",
                    "debug" => "renderingqueuedebug",
                    _ => throw new ArgumentException("Invalid queue name"),
                };

                QueueServiceClient queueServiceClient = new QueueServiceClient(connectionString);
                QueueClient queueClient = queueServiceClient.GetQueueClient(queueName);
                if (!await queueClient.ExistsAsync())
                {
                    throw new ArgumentException("Queue does not exist");
                }

                var queueResponse = await queueClient.ReceiveMessageAsync();
                string message = queueResponse.Value.Body.ToString();

                await queueClient.DeleteMessageAsync(queueResponse.Value.MessageId, queueResponse.Value.PopReceipt);

                var response = req.CreateResponse((HttpStatusCode)queueResponse.GetRawResponse().Status);
                response.WriteString(message);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending message to queue");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(ex.Message);
                return response;
            }
        }
    }
}
