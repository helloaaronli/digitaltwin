using System;
using System.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwinApi.Model;
using DigitalTwinApi.Utils;
using Microsoft.Extensions.Logging;
using DigitalTwinApi.Interfaces;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;



namespace DigitalTwinApi.Services {
    public class VehicleManagerService : Interfaces.IVehicleManager {
        private readonly VehicleManagerModel vehicleManagerModel;

        private IConstructionState _constructionstate;

        public static IConfiguration Configuration;

        private ILogger<ConstructionState> _logger;

        public VehicleManagerService (VehicleManagerModel vehicleManagerModel, ILogger<ConstructionState> logger, IConstructionState constructionstate) {
            this.vehicleManagerModel = vehicleManagerModel;
            _logger = logger;
            _constructionstate = constructionstate;
        }

        /// <summary>
        /// Get the list with all vehicleIds.
        /// </summary>
        /// <returns></returns>
        public async Task<JsonElement> GetVehicleList () {
            JsonDocument vehicleListDocument = null;
            await Task.Run(() => {
                vehicleListDocument = JsonDocument.Parse("{\"vehicleList\":" + JsonSerializer.Serialize(vehicleManagerModel.vehicleList) + "}");
            });

            return vehicleListDocument.RootElement;
        }

        public async Task<JsonElement> GetTopicListForVehicle (string vehicleId) {
            JsonDocument topicListDocument = null;
            await Task.Run(() => {
                topicListDocument = JsonDocument.Parse("{\"topicList\":" + JsonSerializer.Serialize(vehicleManagerModel.topicList[vehicleId]) + "}");
            });

            return topicListDocument.RootElement;
        }

        public async Task<JsonElement> GetTopicForVehicle (string vehicleId, string topicName) {
            JsonDocument topicDocument = null;
            await Task.Run(() => {
                topicDocument = JsonDocument.Parse(JsonSerializer.Serialize(vehicleManagerModel.topicList[vehicleId].Find(x => x.name == topicName)));
            });

            return topicDocument.RootElement;
        }

        /// <summary>
        /// Add vehicle to the global vehicle list and publish a MQTT message with current list.
        /// </summary>
        /// <param name="vehicleId">vehicleId to add</param>
        /// <returns></returns>
        public async Task<bool> AddVehicle (string vehicleId) {
            if (String.IsNullOrWhiteSpace(vehicleId) || vehicleManagerModel.vehicleList.Contains(vehicleId)) {
                return false;
            }
            Console.WriteLine("Adding vehicle {0}", vehicleId);
            await Task.Run(() => {
                vehicleManagerModel.vehicleList.Add(vehicleId);
                vehicleManagerModel.topicList.Add(vehicleId, new List<TopicObject>());
            });

            return true;
        }

        /// <summary>
        /// Remove a vehicle from the global vehicle list and publish a MQTT message with current list.
        /// </summary>
        /// <param name="vehicleId">vehicleId to remove</param>
        /// <returns></returns>
        public async Task<bool> RemoveVehicle (string vehicleId) {
            if (String.IsNullOrWhiteSpace(vehicleId) || !vehicleManagerModel.vehicleList.Contains(vehicleId)) {
                return false;
            }
            Console.WriteLine("Removing vehicle {0}", vehicleId);
            await Task.Run(() => {
                vehicleManagerModel.vehicleList.Remove(vehicleId);
                vehicleManagerModel.topicList.Remove(vehicleId);
            });

            return true;
        }

        public async Task<bool> AddTopicForVehicle (string vehicleId, string topicName, string topic = null, int priority = 0, int ttl = 60) {
            if (String.IsNullOrWhiteSpace(vehicleId) || String.IsNullOrWhiteSpace(topicName)) {
                return false;
            }

            if (!vehicleManagerModel.topicList.ContainsKey(vehicleId) || vehicleManagerModel.topicList[vehicleId].Exists(x => x.name == topicName)) {
                return false;
            }

            TopicObject topicObject = new TopicObject();
            topicObject.name = topicName;
            topicObject.topic = topic;
            topicObject.priority = priority;

            if (ttl == 0) {
                topicObject.ttl = ConstantsGlobal.DEFAULT_TTL_VALUE;
            } else {
                topicObject.ttl = ttl;
            }

            await Task.Run(() => {
                vehicleManagerModel.topicList[vehicleId].Add(topicObject);
            });

            return true;
        }

        public async Task<bool> UpdateTopicForVehicle (string vehicleId, string topicName, string topic = null, int priority = 0, int ttl = 0) {
            if (String.IsNullOrWhiteSpace(vehicleId) || String.IsNullOrWhiteSpace(topicName)) {
                return false;
            }

            if (!vehicleManagerModel.topicList.ContainsKey(vehicleId) || !vehicleManagerModel.topicList[vehicleId].Exists(x => x.name == topicName)) {
                return false;
            }

            await Task.Run(() => {
                TopicObject topicObject = vehicleManagerModel.topicList[vehicleId].Find(x => x.name == topicName);

                if (topic != null) {
                    topicObject.topic = topic;
                }

                if (priority != 0) {
                    topicObject.priority = priority;
                }

                if (ttl == 0) {
                    topicObject.ttl = ConstantsGlobal.DEFAULT_TTL_VALUE;
                } else {
                    topicObject.ttl = ttl;
                }
            });

            return true;
        }

        public async Task<bool> RemoveTopicForVehicle (string vehicleId, string topicName) {
            if (String.IsNullOrWhiteSpace(vehicleId) || String.IsNullOrWhiteSpace(topicName)) {
                return false;
            }

            if (!vehicleManagerModel.topicList.ContainsKey(vehicleId)) {
                return false;
            }

            await Task.Run(() => {
                return vehicleManagerModel.topicList[vehicleId].Remove(vehicleManagerModel.topicList[vehicleId].Find(x => x.name == topicName));
            });

            return true;
        }

        public async Task<bool> UpdateLists () {
            Console.WriteLine("Updating lists...");
            string user = Configuration["dtUserId"];

            if(!await GetVehicleListForUser(user)) {
                return false;
            }
            Console.WriteLine("Got #vehicles from VWAC: {0}", vehicleManagerModel.vehicleList.Count);

            foreach (string vehicleId in vehicleManagerModel.topicList.Keys) {
                await AddStartupTopics(vehicleId);
                await AddCsoTopics(vehicleId);
            }
            return true;
        }

        /// <summary>
        /// Get all vehicleIds linked to a specific usedId (DigitalTwin). Store these vehicle in a global list.
        /// </summary>
        /// <param name="userId">userId to query</param>
        /// <returns></returns>
        private async Task<bool> GetVehicleListForUser (string userId) {
            string apiVersion = Constants.VWAC_API_USER_API_VERSION;
            string domainName = Constants.VWAC_API_DOMAIN_NAME;
            string commandEndpointBegin = $"https://{domainName}/users/api/user/{userId}/vehicles";

            string encodedResponseContinuation = "";
            bool firstIteration = true;

            while (encodedResponseContinuation != null) {
                Console.WriteLine("------------------------------------ITERATION----------------------------------------------");
                string commandEndpoint = "";

                if (firstIteration) {
                    commandEndpoint = $"{commandEndpointBegin}?api-version={apiVersion}";
                    firstIteration = false;
                } else {
                    commandEndpoint = $"{commandEndpointBegin}?responseContinuation={encodedResponseContinuation}&api-version={apiVersion}";
                }               
                
                HttpClient httpClient = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, commandEndpoint);
                string userKey = Configuration["userKey"];
                message.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
                BearerToken_Request BearerToken_Request = new BearerToken_Request(Configuration);
                string token = "bearer " + await BearerToken_Request.getUserToken(false);
                message.Headers.Add("Authorization", token);
                HttpResponseMessage responseMessage = await httpClient.SendAsync(message);

                if (!(responseMessage.Content is object)) {
                    Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
                    return false;
                } else if((int)responseMessage.StatusCode == 401) {
                    HttpClient newHttpClient = new HttpClient();
                    HttpRequestMessage newMessage = new HttpRequestMessage(HttpMethod.Get, commandEndpoint);
                    BearerToken_Request = new BearerToken_Request(Configuration);
                    token = "bearer " + await BearerToken_Request.getUserToken(true);
                    newMessage.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
                    newMessage.Headers.Add("Authorization", token);
                    responseMessage = await newHttpClient.SendAsync(message);
                    if ((int)responseMessage.StatusCode != 200) {
                        Console.WriteLine("Error using vwac paas user management api! Even new token is invalid!");
                    }
                }

                JsonDocument responseJson;
                string response = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine("Response from VWAC API: {0}", response);
                try {
                    responseJson = JsonDocument.Parse(response);
                } catch (Exception ex) {
                    Console.WriteLine("EXCEPTION for response from VWAC API: {0}", ex.Message);
                    return false;
                }                
                
                try {
                    JsonElement vehicleIdArray = responseJson.RootElement.GetProperty("Items");
                    foreach (var vehicleIdObject in vehicleIdArray.EnumerateArray()) {
                        string vehicleId = vehicleIdObject.GetString();
                        await AddVehicle(vehicleId);
                        Console.WriteLine("Got '" + vehicleId + "' from VWAC API");
                    }

                    Console.WriteLine("ResponseContinuation: " + responseJson.RootElement.GetProperty("ResponseContinuation").GetString());
                    encodedResponseContinuation = HttpUtility.UrlEncode(responseJson.RootElement.GetProperty("ResponseContinuation").GetString());                    
                } catch (Exception ex) {
                    Console.WriteLine("EXCEPTION for response from VWAC API: {0}, {1}", responseJson.RootElement.ToString(), ex.Message);
                    return false;
                }
            }
            
            return true;
        }

        private async Task<bool> AddStartupTopics (string vehicleId) {
            await AddTopicForVehicle(vehicleId, "Heartbeat", "edgetwin/heartbeat");
            await AddTopicForVehicle(vehicleId, "Battery Charging Level", "cso/v0/vehicle/battery/highVoltage/chargingLevel0");
            await AddTopicForVehicle(vehicleId, "Battery Charging State", "cso/v0/vehicle/battery/highVoltage/chargingState0");
            return true;
        }

        private async Task<bool> AddCsoTopics (string vehicleId) {
            var constructionState = await _constructionstate.GetConstructionStateForVehicleFromMongoDb(vehicleId, false);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(constructionState.CsModel.RootElement);
            JObject newDoc = new JObject();
            try {
                foreach (dynamic element in dictionary) {
                    if (element.Key.Contains("api_cso"))
                    {
                        string name = element.Key.Replace("api_cso/", "");
                        await AddTopicForVehicle(vehicleId, name, name);
                    }
                }
            } catch {
                Console.WriteLine("Error while iterating through constructionState and adding api_cso topics!");
            }
            
            return true;
        }
    }
}