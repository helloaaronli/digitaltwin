using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;


using System;
using System.Threading.Tasks;
using System.Threading;

namespace DigitalTwinApi.Mqtt {
    public class MqttHandler {

        protected ManagedMqttClient managedClient;
        protected IManagedMqttClientOptions managedConnectionOptions;
        public IManagedMqttClientOptions ManagedConnectionOptions { get => managedConnectionOptions; private set { managedConnectionOptions = value; } }


        public MqttHandler (string brokerAddress, string clientId) {
            managedConnectionOptions = new ManagedMqttClientOptionsBuilder()
                     .WithAutoReconnectDelay(TimeSpan.FromMilliseconds(3000))
                     .WithClientOptions(new MqttClientOptionsBuilder()
                         .WithClientId(clientId)
                         .WithTcpServer(brokerAddress, 1883)
                         .WithCleanSession()
                         .WithProtocolVersion((MqttProtocolVersion)5)
                         .WithRequestResponseInformation()
                         .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(30000))
                         .WithKeepAliveSendInterval(TimeSpan.FromMilliseconds(30000 * 0.5)) // prevent connection lost
                         .Build())
                     .Build();

            // create a new managed MQTT client, register to events & connect
            try {
                managedClient = (ManagedMqttClient)new MqttFactory().CreateManagedMqttClient();
                Console.WriteLine("Starting MQTHandler");
                managedClient.StartAsync(managedConnectionOptions).GetAwaiter().GetResult();

                managedClient.UseConnectedHandler(e => OnConnectionEstablished(e)); // triggered if connection was established
                managedClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(e => OnConnectingFailed(e)); // triggered if connection was not established
                managedClient.UseDisconnectedHandler(e => { Console.WriteLine("Disconnected handler triggered"); });
                managedClient.UseApplicationMessageReceivedHandler(e => OnMessageReceived(e));

            } catch (System.Net.Sockets.SocketException ex) {
                Console.WriteLine("Websocket: Cannot establish the connection to the broker. {0}", ex.Message);
            } catch (Exception ex) {
                Console.WriteLine("Can't create new MQTT managed client: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Subscribe to a topic.
        /// </summary>
        /// <param name="topic">Topic to subscribe</param>
        /// <param name="qos">Quality of Service</param>
        /// <returns></returns>
        public async Task<bool> SubscribeAsync (string topic, int qos = 0) {
            if (managedClient == null) {
                Console.WriteLine("Couldn't subscribe. MQTT Client is null.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(topic)) {
                Console.WriteLine("Couldn't subscribe. Topic is invalid (null, empty or whitespace).");
                return false;
            }

            while (!managedClient.IsConnected) {
                Console.WriteLine("MQTT Client is connected, waiting....");
                Thread.Sleep(1000);
            }

            Console.Write("Subscribing to topic {0} ", topic);
            try {
                TopicFilter topicFilter = new TopicFilterBuilder()
                        .WithTopic(topic)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                        .Build();


                await managedClient.SubscribeAsync(topicFilter);
                Console.WriteLine("... topic {0} subscribed", topic);
                return true;

            } catch (Exception ex) {
                Console.WriteLine("Couldn't subscribe. Error: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Unsubscribe from a topic.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<bool> UnsubscribeAsync (string topic) {
            if (managedClient == null || !managedClient.IsConnected) {
                Console.WriteLine(" Couldn't unsubscribe. MQTT Client is null or not connected.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(topic)) {
                Console.WriteLine("Couldn't unsubscribe. Topic is invalid (null, empty or whitespace).");
                return false;
            }

            Console.Write("Unsubscribing from topic {0} ", topic);
            try {
                await managedClient.UnsubscribeAsync(topic);
                Console.WriteLine("... topic {0} unsubscribed", topic);
                return true;

            } catch (Exception ex) {
                Console.WriteLine("Couldn't unsubscribe. Error: {0}", ex.Message);
                return false;
            }

        }

        /// <summary>
        /// Publish a message to a topic.
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="payload">Payload</param>
        /// <param name="retainFlag">Retain flag</param>
        /// <param name="qos">Quality of Service</param>
        /// <returns></returns> 
        public async Task<bool> PublishAsync (string topic, string payload, bool retainFlag = false, int qos = 0) {
            if (managedClient == null || !managedClient.IsConnected) {
                Console.WriteLine("Couldn't publish. MQTT Client is null or not connected.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(topic)) {
                Console.WriteLine("Couldn't publish. Topic is invalid (null, empty or whitespace).");
                return false;
            }

            Console.Write("Publishing a message to topic {0} (payload size {1}) ", topic, payload.Length);
            try {

                ManagedMqttApplicationMessage applicationMessage = new ManagedMqttApplicationMessageBuilder()
                    .WithId(Guid.NewGuid())
                    .WithApplicationMessage(
                        new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                        .WithRetainFlag(retainFlag)
                        .Build()).Build();

                await managedClient.PublishAsync(applicationMessage);
                Console.WriteLine("... message published");
                return true;

            } catch (System.Exception ex) {
                Console.WriteLine("Couldn't publish: {0}", ex.Message);
                return false;
            }

        }


        // ******************************************************* EVENT HANDLERS *******************************************************
        /// <summary>
        /// Used for event UseConnectedHandler: perform actions when the connection is established.
        /// Show some conncetion parameters.
        /// </summary>
        /// <param name="e"></param>
        private void OnConnectionEstablished (MqttClientConnectedEventArgs e) {
            Console.WriteLine("Connection to {0} established: {1}", managedClient.Options.ClientOptions.ChannelOptions.ToString(), e.AuthenticateResult.ResultCode.ToString());

            Console.WriteLine("IsConnected: " + managedClient.IsConnected.ToString() +
            "\n IsStarted: " + managedClient.IsStarted.ToString() +
            "\n MQTT version:" + managedClient.Options.ClientOptions.ProtocolVersion.ToString() +
            "\n KeepAliveSendInterval: " + managedClient.Options.ClientOptions.KeepAliveSendInterval +
            "\n KeepAlivePeriod: " + managedClient.Options.ClientOptions.KeepAlivePeriod);

        }

        /// <summary>
        /// Used for ApplicationMessageProcessedHandler: perform actions when the connection failed to be established.
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnConnectingFailed (ManagedProcessFailedEventArgs e) {
            Console.WriteLine("OnConnectingFailed triggered : {0}", e.Exception.Message.ToString());
        }


        /// <summary>
        /// Used for event UseApplicationMessageReceivedHandler: parse the message and forward to DMC.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual void OnMessageReceived (MqttApplicationMessageReceivedEventArgs e) {
            throw new NotImplementedException();
        }


    }
}