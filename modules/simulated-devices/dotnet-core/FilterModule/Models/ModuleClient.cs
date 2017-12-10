using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace FilterModule.Models
{
    public class ModuleClient
    {
        private string ConnectionString { get; set; }
        private bool BypassCertVerification { get; set; }
        private int _counter;
        private int TemperatureThreshold { get; set; } = 25;
        
        public ModuleClient(string connectionString, bool bypassCertVerification = false)
        {
            ConnectionString = connectionString;
            BypassCertVerification = bypassCertVerification;
        }
        
        public async Task StartClientAsync()
        {
            var mqttSettings = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);

            // During dev you might want to bypass the cert verification. 
            // It is highly recommended to verify certs systematically in production

            if (BypassCertVerification)
            {
                mqttSettings.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }

            ITransportSettings[] settings = { mqttSettings };

            // Open a connection to the Edge runtime

            var ioTHubModuleClient = DeviceClient.CreateFromConnectionString(ConnectionString, settings);

            await ioTHubModuleClient.OpenAsync();

            Console.WriteLine("IoT Hub module client initialized.");

            // Attach callback for Twin desired properties updates
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", FilterMessages, ioTHubModuleClient);
        }

        private Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties["TemperatureThreshold"] != null)
                {
                    TemperatureThreshold = desiredProperties["TemperatureThreshold"];
                }
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

        /// <summary>
        /// Called whenever the module receives a message from the IoT Edge hub. 
        /// Filters out messages that report temperatures below the temperature threshold set via the module twin. 
        /// It also adds the MessageType property to the message with the value set to Alert.
        /// </summary>
        private async Task<MessageResponse> FilterMessages(Message message, object userContext)
        {
            var counterValue = Interlocked.Increment(ref _counter);

            try {
                var deviceClient = (DeviceClient) userContext;
                var messageBytes = message.GetBytes();
                var messageString = Encoding.UTF8.GetString(messageBytes);

                Console.WriteLine($"Received message {counterValue}: [{messageString}]");

                // Get message body
                var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

                if (messageBody != null && messageBody.Machine.Temperature > TemperatureThreshold)
                {
                    Console.WriteLine($"Machine temperature {messageBody.Machine.Temperature} " +
                        $"exceeds threshold {TemperatureThreshold}");

                    var filteredMessage = new Message(messageBytes);
                    
                    foreach (var prop in message.Properties)
                    {
                        filteredMessage.Properties.Add(prop.Key, prop.Value);
                    }

                    // Adding an alert
                    filteredMessage.Properties.Add("MessageType", "Alert");
                    
                    await deviceClient.SendEventAsync("output1", filteredMessage);
                }

                // Indicate that the message treatment is completed
                return MessageResponse.Completed;
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in sample: {0}", exception);
                }
                // Indicate that the message treatment is not completed
                DeviceClient deviceClient = (DeviceClient)userContext;
                return MessageResponse.Abandoned;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
                // Indicate that the message treatment is not completed
                DeviceClient deviceClient = (DeviceClient)userContext;
                return MessageResponse.Abandoned;
            }
        }
    }
}