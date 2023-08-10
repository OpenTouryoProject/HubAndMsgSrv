using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Text;

// ADDED --------------------------------------------------
using System.Collections.Generic;     // For KeyValuePair<>
using Microsoft.Azure.Devices.Shared; // For TwinCollection
using Newtonsoft.Json;                // For JsonConvert
// --------------------------------------------------------

// CHANGED ------------------------------------------------
namespace filtermodule{
// --------------------------------------------------------

    // ADDED --------------------------------------------------
    class MessageBody
    {
        public Machine machine {get;set;}
        public Ambient ambient {get; set;}
        public string timeCreated {get; set;}
    }
    class Machine
    {
        public double temperature {get; set;}
        public double pressure {get; set;}
    }
    class Ambient
    {
        public double temperature {get; set;}
        public int humidity {get; set;}
    }
    // --------------------------------------------------------

    internal class ModuleBackgroundService : BackgroundService
    {
        private int _counter;
        private ModuleClient? _moduleClient;
        private CancellationToken _cancellationToken;
        private readonly ILogger<ModuleBackgroundService> _logger;
        
        // ADDED --------------------------------------------------
        static int temperatureThreshold { get; set; } = 25;
        // --------------------------------------------------------

        public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            MqttTransportSettings mqttSetting = new(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

            // Reconnect is not implented because we'll let docker restart the process when the connection is lost
            _moduleClient.SetConnectionStatusChangesHandler((status, reason) => 
                _logger.LogWarning("Connection changed: Status: {status} Reason: {reason}", status, reason));

            await _moduleClient.OpenAsync(cancellationToken);

            _logger.LogInformation("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module

            // CHANGED ------------------------------------------------
            //await _moduleClient.SetInputMessageHandlerAsync("input1", ProcessMessageAsync, null, cancellationToken);
            // --------------------------------------------------------
            
            // ADDED --------------------------------------------------
            // Module TwinからTemperatureThreshold値を読み取る。
            var moduleTwin = await _moduleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, _moduleClient);

            // Module TwinのDesiredPropertyUpdateCallbackのdelegateを登録。
            await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Moduleの受信message（inputFromSensor）をハンドルするInputMessageHandlerのdelegateを登録。
            await _moduleClient.SetInputMessageHandlerAsync("inputFromSensor", FilterMessages, _moduleClient);
            // --------------------------------------------------------
        }

        async Task<MessageResponse> ProcessMessageAsync(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref _counter);

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            _logger.LogInformation("Received message: {counterValue}, Body: [{messageString}]", counterValue, messageString);

            if (!string.IsNullOrEmpty(messageString))
            {
                using var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                await _moduleClient!.SendEventAsync("output1", pipeMessage, _cancellationToken);

                _logger.LogInformation("Received message sent");
            }
            return MessageResponse.Completed;
        }

        // ADDED --------------------------------------------------
        // Module TwinのDesired Propertiesの更新を受け取り、temperatureThreshold 変数を更新
        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties["TemperatureThreshold"]!=null)
                    temperatureThreshold = desiredProperties["TemperatureThreshold"];

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }

        // Edge moduleが IoT Hubからmessage（inputFromSensor）を受け取るたびに呼び出される。
        async Task<MessageResponse> FilterMessages(Message message, object userContext)
        {
            var counterValue = Interlocked.Increment(ref _counter);
            try
            {
                ModuleClient moduleClient = (ModuleClient)userContext;
                var messageBytes = message.GetBytes();
                var messageString = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"Received message {counterValue}: [{messageString}]");

                // Get the message body.
                var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

                if (messageBody != null && messageBody.machine.temperature > temperatureThreshold)
                {
                    Console.WriteLine($"Machine temperature {messageBody.machine.temperature} " +
                        $"exceeds threshold {temperatureThreshold}");
                    using (var filteredMessage = new Message(messageBytes))
                    {
                        foreach (KeyValuePair<string, string> prop in message.Properties)
                        {
                            filteredMessage.Properties.Add(prop.Key, prop.Value);
                        }

                        filteredMessage.Properties.Add("MessageType", "Alert");
                        await moduleClient.SendEventAsync("output1", filteredMessage);
                    }
                }

                // Indicate that the message treatment is completed.
                return MessageResponse.Completed;
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in sample: {0}", exception);
                }
                // Indicate that the message treatment is not completed.
                var moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
                // Indicate that the message treatment is not completed.
                ModuleClient moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
        }

        // --------------------------------------------------------
    }
}