using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SBReader
{
    
    
        class Program
        {
        // Connection String for the namespace can be obtained from the Azure portal under the 
        // 'Shared Access policies' section.
            const string ServiceBusConnectionString = "Endpoint=sb://sbsftest.servicebus.windows.net/;SharedAccessKeyName=allinone;SharedAccessKey=AynwWBDh9c1FbhKsvdvcrhOxBmsplI3DIrsLlE5QGxA=;";
            const string QueueName = "subscribtions";
            static IQueueClient queueClient;
            static HttpClient httpClient;

        static IServiceProvider serviceProvider;

            static void Main(string[] args)
            {
                IServiceCollection services = new ServiceCollection();
                services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Category", LogLevel.Information));
                services.AddApplicationInsightsTelemetryWorkerService("4bbf1eba-8d47-45ff-ad8f-8d7f4690d7b8");
                serviceProvider = services.BuildServiceProvider();

            httpClient = new HttpClient();

                MainAsync().GetAwaiter().GetResult();
            }

            static async Task MainAsync()
            {
                queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

                Console.WriteLine("======================================================");
                Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
                Console.WriteLine("======================================================");

                // Register QueueClient's MessageHandler and receive messages in a loop
                RegisterOnMessageHandlerAndReceiveMessages();

                Console.ReadKey();

                await queueClient.CloseAsync();
            }

            static void RegisterOnMessageHandlerAndReceiveMessages()
            {
                // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
                var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
                {
                    // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                    // Set it according to how many messages the application wants to process in parallel.
                    MaxConcurrentCalls = 10,
                    

                    // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                    // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                    AutoComplete = false
                };

                // Register the function that will process messages
                queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            }

            static async Task ProcessMessagesAsync(Message message, CancellationToken token)
            {
                var telemetryClient = serviceProvider.GetRequiredService<
                                                            TelemetryClient>();
                // Process the message
                var activity = message.ExtractActivity();

                using (var operation = telemetryClient.StartOperation<RequestTelemetry>(activity))
                {
                

                    await Task.Delay(5000);


                    try
                    {
                        await httpClient.GetAsync(@"https://localhost:44358/api/values");
                    }
                    catch (Exception ex)
                    {
                        telemetryClient.TrackException(ex);
                        throw;
                    }

                var metricTelemetry = new MetricTelemetry();
                //metricTelemetry.
                //telemetryClient.TrackMetric("CustomersProcessed",1,)

                // Complete the message so that it is not received again.
                // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
                await queueClient.CompleteAsync(message.SystemProperties.LockToken);

                telemetryClient.TrackTrace($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}"
                                           , SeverityLevel.Information);

                //Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

                // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
                // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
                // to avoid unnecessary exceptions.
            }
            }

            static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
            {
                Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
                var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
                Console.WriteLine("Exception context for troubleshooting:");
                Console.WriteLine($"- Endpoint: {context.Endpoint}");
                Console.WriteLine($"- Entity Path: {context.EntityPath}");
                Console.WriteLine($"- Executing Action: {context.Action}");
                return Task.CompletedTask;
            }
        }
    
}
