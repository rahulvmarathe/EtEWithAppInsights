using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace BatchFunc
{
    public static class SBAPI
    {
        const string ServiceBusConnectionString = "Endpoint=sb://sbsftest.servicebus.windows.net/;SharedAccessKeyName=allinone;SharedAccessKey=AynwWBDh9c1FbhKsvdvcrhOxBmsplI3DIrsLlE5QGxA=;";
        static IQueueClient queueClient;
        static string QueueName = "subscribtions";
        
        static SBAPI()
        {
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

        }

        [FunctionName("SBAPI")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var message = new Message(Encoding.UTF8.GetBytes(requestBody));
            // Send the message to the queue

            await queueClient.SendAsync(message);
            return;
        }

        [FunctionName("ABReader")]
        public static void RunQueueTrigger([ServiceBusTrigger("subscribtions", Connection = "queueconnectionstring")]string myQueueItem, ILogger log)
        {
            Task.Delay(5000).Wait();
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }


    }
}
