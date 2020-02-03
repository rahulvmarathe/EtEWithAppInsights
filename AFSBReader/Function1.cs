using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AFSBReader
{
    public static class Function1
    {
        [FunctionName("ABReader")]
        public static void Run([QueueTrigger("subscribtions", Connection = "queconnectinstring")]string myQueueItem, ILogger log)
        {
            Task.Delay(5000).Wait();
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
