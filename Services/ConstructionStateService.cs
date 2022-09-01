using DigitalTwinApi.Model;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using System.Text;
using Microsoft.Extensions.Configuration;
using DigitalTwinApi.Utils;

namespace DigitalTwinApi.Services
{
    public class ConstructionState : Interfaces.IConstructionState
    {
        private readonly ILogger<ConstructionState> _logger;
        private readonly VehicleManagerModel _vehicleManagerModel;
        public static IMongoCollection<State> _collection;

        public static IConfiguration _configuration;

        public ConstructionState(ILogger<ConstructionState> logger, VehicleManagerModel vehicleManagerModel, IConfiguration conf) {
            _collection = new MongodbService().getDatabase().GetCollection<State>("constructionState");  // get the states collection DB
            _logger = logger;
            _vehicleManagerModel = vehicleManagerModel;
            _configuration = conf;
        }

        public async Task<ConstructionStateModel> GetConstructionStateForVehicleFromMongoDb (string vehicleId, bool returnStructuredJson) {
            var dbResult = await _collection.FindSync(obj => obj.vehicleId == vehicleId).FirstOrDefaultAsync();
            if(dbResult == null) {
                return ( new ConstructionStateModel(JsonDocument.Parse("{}")) );
            }
            
            BsonDocument bson = dbResult.state.ToBsonDocument();
            var dotNetObj = BsonTypeMapper.MapToDotNetValue(bson);
            var serializedObj = Newtonsoft.Json.JsonConvert.SerializeObject(dotNetObj);
            JObject json = JObject.Parse(serializedObj); 
            JsonDocument doc = JsonDocument.Parse(json.ToString());

            if (returnStructuredJson){
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(json.ToString());
                JObject newDoc = new JObject();
                try {

                    foreach (dynamic element in dictionary) {
                        JObject jObject = new JObject();

                        if (!element.Key.Contains("/")) {
                            var value = doc.RootElement.GetProperty(element.Key).GetProperty("value").GetString();
                            newDoc[element.Key] = value;

                        } else {

                            string[] parts = element.Key.Split('/');
                            List<string> partsList = new List<string>(parts);
                            partsList.Reverse();

                            JObject jValue = new JObject();
                            jValue[partsList[0]] = doc.RootElement.GetProperty(element.Key).GetProperty("value").GetString();

                            int iter = 0;

                            foreach (dynamic part in partsList.Skip(1)) {
                                JObject jElem = new JObject();
                                if (iter == 0) {
                                    jElem[part] = jValue;
                                } else {
                                    jElem[part] = jObject;

                                }

                                jObject = new JObject();
                                jObject = jElem;
                                iter++;
                            }
                        }

                        newDoc.Merge(jObject, new JsonMergeSettings {
                            // union array values together to avoid duplicates
                            MergeArrayHandling = MergeArrayHandling.Union
                        });
                    }

                    doc = JsonDocument.Parse(newDoc.ToString());
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e);
                }
            }
            return (new ConstructionStateModel(doc));
        }

        private Boolean CheckForCompareOperator(string op) {
            Boolean foundOperator = false;
            if( op == "or" || op == "and" || op == "nor"  || op == "mod"   || op == "where" || op == "in"|| op == "nin" ||  op == "all" || op == "eq" ||
                op == "gt" || op == "regex" || op == "gte" || op == "lt" || op == "lte" || op == "ne" || op == "exists" || op == "type" || op == "size" ||
                op == "not" || op == "expr" || op == "elemMatch") {
                foundOperator = true;
            }
            return foundOperator;
        }
        private Object WalkNode(JToken node) {
            if(node.Children().Count() > 0) {
                if(node.Type.Equals(JTokenType.Property)) {
                    JProperty property = (JProperty)node;
                    JToken valueToWalk = property.Value;
                    _logger.LogDebug("CHILD: " + property);

                    string key = null;
                    if(property.Name.Contains("lastModified")) {
                        key = property.Name;
                    }
                    else if(CheckForCompareOperator(property.Name)) {
                        key = "$" + property.Name;
                    }
                    else{
                        var propertyValues = property.Values();
                        if(propertyValues.Count() > 1) {
                            key = "$and";
                            JArray arr = new JArray();
                            foreach(JProperty token in propertyValues) {
                                string newKey;
                                JObject valueObj;
                                if(token.Name.Equals("lastModified")) {
                                    newKey = "state." + property.Name + ".lastModified";
                                    valueObj = (JObject)token.Value;
                                }
                                else{
                                    newKey = property.Name;
                                    valueObj = new JObject((JProperty)token);
                                }
                                JObject jobj = new JObject(new JProperty(newKey, valueObj));
                                arr.Add(jobj);
                                valueToWalk = arr;
                            }
                            _logger.LogDebug("BUILTOBJ: " + arr.ToString());
                        }
                        else{
                            key = "state." + property.Name + ".value";
                        }
                    }
                    _logger.LogDebug("NEWKEY: " + key);

                    BsonElement bsonElement = new BsonElement(key, BsonValue.Create(WalkNode(valueToWalk)));
                    _logger.LogDebug("ELEMENTPROP: " + bsonElement);

                    return bsonElement;   
                }
                else if(node.Type.Equals(JTokenType.Object)) {
                    string otherPropName = "";
                    JArray arr = new JArray();
                    JProperty saveLastModifiedProp = null;
                    Boolean hasLastModified = false;

                    if(node.Children().Count() == 2) {   
                        foreach(JProperty token in node.Children()) {
                            if(token.Name.Equals("lastModified")) {
                                saveLastModifiedProp = token;
                                hasLastModified = true;
                            }
                            else{
                                otherPropName = token.Name;
                                JObject jobj = new JObject(token);
                                arr.Add(jobj);
                            }
                        }
                    }

                    if(hasLastModified) {    
                        JProperty newLastModifiedProp = new JProperty("state." + otherPropName + ".lastModified", saveLastModifiedProp.Value);
                        arr.Add(new JObject(newLastModifiedProp));
                        JObject builtObj = new JObject(new JProperty("and", arr));
                        _logger.LogDebug("BUILTOBJJ: " + builtObj.ToString());
                        return(WalkNode(builtObj));
                    }
                    else{
                        BsonDocument doc = new BsonDocument();
                        foreach(var item in node) {
                            doc.Add((BsonElement)WalkNode(item));
                            _logger.LogDebug("OBJECTTEMP: " + doc);
                        }
                        _logger.LogDebug("OBJECTRET: " + doc);
                        return doc;
                    }   
                }
                else if(node.Type.Equals(JTokenType.Array)) {
                    BsonArray bArray = new BsonArray();
                    foreach(var item in node) {
                        bArray.Add(BsonValue.Create(WalkNode(item)));
                        _logger.LogDebug("ARRAYTEMP: " + bArray);
                    }
                    _logger.LogDebug("ARRAYRET: " + bArray);
                    return bArray;
                }
                else {
                    _logger.LogDebug("TYPE OTHER");
                    return BsonValue.Create(node.ToString());
                }
            }
            else {
                if(node.Type.Equals(JTokenType.Date)) {
                    return node.ToObject<DateTime>();
                }
                return BsonValue.Create(node.ToString()); //return value
            }   
        }

        public async Task<JsonElement> QueryVehiclesList(string key, string value)
        {
            JsonDocument vehicleListResult = null;
            List<State> queryResult = await QueryByKeyValuePairs(key, value);
            List<string> vehicleList = queryResult.Select(obj => obj.vehicleId).ToList();

            foreach (var vehicle in vehicleList)
            {
            _logger.LogInformation($"Vehicle found in mongodb = {vehicle}");
                
            }

            vehicleListResult = JsonDocument.Parse("{\"vehicleList\":" + JsonSerializer.Serialize(vehicleList) + "}");
            return vehicleListResult.RootElement;
        }

        public async Task<JsonElement> QueryVehiclesCount(string key, string value)
        {
            JsonDocument vehicleCountResult = null;
            List<State> queryResult = await QueryByKeyValuePairs(key, value);
            vehicleCountResult = JsonDocument.Parse("{\"vehicleCount\":" + queryResult.Count + "}");
            return vehicleCountResult.RootElement;
        }

        public async Task<JsonElement> ExecuteCustomQueryList(JsonElement query)
        {
            JsonDocument res = null;
            string json = JsonSerializer.Serialize(query);
            JToken node = JToken.Parse(json);
            var jObject = WalkNode(node);
            _logger.LogDebug(jObject.ToString());
            BsonDocument doc = BsonDocument.Parse(jObject.ToString());

            _logger.LogInformation($"Executing custom query list..");
            var dbResult = await _collection.Find(doc).ToListAsync();
            var vehicleList = dbResult.Select(obj => obj.vehicleId).ToList();

            res = JsonDocument.Parse("{\"vehicleList\":" + JsonSerializer.Serialize(vehicleList) + "}");
            return res.RootElement;
        }

        public async Task<JsonElement> ExecuteCustomQueryCount(JsonElement query)
        {
            JsonDocument res = null;
            string json = JsonSerializer.Serialize(query);
            JToken node = JToken.Parse(json);
            var jObject = WalkNode(node);
            _logger.LogDebug(jObject.ToString());
            BsonDocument doc = BsonDocument.Parse(jObject.ToString());

            _logger.LogInformation($"Executing custom query count..");
            var dbResult = await _collection.Find(doc).ToListAsync();
            var vehicleCount = dbResult.Count;

            res = JsonDocument.Parse("{\"vehicleCount\":" + vehicleCount + "}");
            return res.RootElement;
        }

        private async Task<List<State>> QueryByKeyValuePairs(string key, string value)
        {
            List<State> queryResult = new List<State>();

            try {
            string adaptedKey = AdaptKeyForQuery(key);
            _logger.LogInformation($"Executing Query using key: {adaptedKey} and value {value}");
            var filter = Builders<State>.Filter.Eq(adaptedKey, value);
            queryResult = await _collection.Find(filter).ToListAsync();
            _logger.LogInformation("Finished Querying DB!");
            return queryResult;

            } catch(Exception ex) {
                _logger.LogError("Exception occured: ", ex);
                return queryResult;    
            }
        }
    
        private string AdaptKeyForQuery(string key) {
            
            _logger.LogInformation("Adapting key for the query!!");

            string queryKeyStructure = "state.{0}.value";
            if (key.Contains("%")) // check if key has been automatically encoded
            {   
                _logger.LogInformation("Decoding URL...");
                return System.Web.HttpUtility.UrlDecode(string.Format(queryKeyStructure, key));
            }
                
            if (key.Contains('.'))
            {
                _logger.LogInformation("replacing . with /");
                return string.Format(queryKeyStructure, key.Replace('.', '/'));
            }
            
            _logger.LogInformation("Returning key without modification");
            return string.Format(queryKeyStructure, key);
        }


        private static async void sendToOracle(string oracleEndpoint, HttpRequestMessage message) {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage responseMessage = await httpClient.SendAsync(message);
        }
        public async Task<System.Net.HttpStatusCode> InsertConstructionStateForVehicle (string owner, string vehicleId, JsonElement payload, bool transformIntoKeyValue) {
            string miracleEndpoint = Constants.miracleEndpoint;
            string insertPayload = "";
            
            if(!transformIntoKeyValue) {
                insertPayload = payload.ToString();
                _logger.LogDebug(insertPayload);
            }
            else{
                // var option = new JsonSerializerOptions { WriteIndented = true }; // grouped key value pairs into a column
                var keyPairs = GetFlat(payload.ToString());
                insertPayload = JsonSerializer.Serialize(keyPairs/*,option*/).ToString();
                _logger.LogDebug(insertPayload);
            }
            if (insertPayload.Contains("{\"value\":") || insertPayload.Contains("{\"hasConflict\":") || insertPayload.Contains("{\"lastModified\":") || insertPayload.Contains("\"value\":") || insertPayload.Contains("\"value\" :") || insertPayload.Contains("\"hasConflict\":") || insertPayload.Contains("\"lastModified\":")) {
                throw new Exception("please insert like this format: {\"color\":\"blue\"} where \"blue\" is the value of the key \"color\"\n value, hasConflict and lastModified like in constructionState displayed is carried by the system.");
            }
            try {
                if (!ConstantsGlobal.VALID_OWNERS.Contains(owner)) {
                    var list = _vehicleManagerModel.vehicleList;
                    try {
                        foreach (dynamic vehicle in list) {
                            if (vehicle == owner) {
                                owner = "fleet";
                            }
                        }
                        if (owner != "fleet") {
                            string apiVersion = Constants.VWAC_API_VEHICLE_API_VERSION;
                            string domainName = Constants.VWAC_API_DOMAIN_NAME;
                            string commandEndpoint = $"https://{domainName}/vehicles/api/registration/vehicle/{vehicleId}?api-version={apiVersion}";
                            HttpClient newhttpClient = new HttpClient();
                            HttpRequestMessage newmessage = new HttpRequestMessage(HttpMethod.Get, commandEndpoint);
                            string userKey = _configuration["userKey"];
                            newmessage.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
                            BearerToken_Request BearerToken_Request = new BearerToken_Request(_configuration);
                            string token = "bearer " + await BearerToken_Request.getUserToken(false);
                            newmessage.Headers.Add("Authorization", token);
                            HttpResponseMessage newresponseMessage = await newhttpClient.SendAsync(newmessage);
                            
                            if (!(newresponseMessage.Content is object)) {
                                Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
                            } else if ((int)newresponseMessage.StatusCode == 200){
                                Console.WriteLine("Found vehicle in vwac");
                                owner = "fleet";
                            } else if((int)newresponseMessage.StatusCode == 401) {
                                HttpClient newHttpClient = new HttpClient();
                                HttpRequestMessage newMessage = new HttpRequestMessage(HttpMethod.Get, commandEndpoint);
                                BearerToken_Request = new BearerToken_Request(_configuration);
                                token = "bearer " + await BearerToken_Request.getUserToken(true);
                                newMessage.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
                                newMessage.Headers.Add("Authorization", token);
                                newresponseMessage = await newHttpClient.SendAsync(newmessage);
                                if ((int)newresponseMessage.StatusCode == 200) {
                                    Console.WriteLine("Found vehicle in vwac");
                                    owner = "fleet";
                                } else {
                                    Console.WriteLine("Error using vwac vehicle management api! Even new token is invalid or vehicle not found!");
                                    throw new Exception("INVALID OWNER! Please insert valid owner");
                                }
                            } else {
                                Console.WriteLine("Error using vwac vehicle management api or vehicle not found!");
                                throw new Exception("INVALID OWNER! Please insert valid owner");
                            }
                        }
                    } catch {
                        Console.WriteLine("Error while iterating through Construction State and checking for owner!");
                    }
                }

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage messageMiracle = new HttpRequestMessage(HttpMethod.Post, miracleEndpoint);
                messageMiracle.Headers.Add("VehicleId", vehicleId);
                messageMiracle.Headers.Add("Authorization", "ApiKey 4eab5d27-b265-41a5-94a5-17c883e0be51");//TODO: maybe add a header in telemetry ext to determine if the insert is done via telemetry extension and not accidentially or on purpose by external client
                messageMiracle.Headers.Add("owner", owner);
                messageMiracle.Content = new StringContent(insertPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage responseMessage = await httpClient.SendAsync(messageMiracle);
                
                return(responseMessage.StatusCode);
            }
            catch (RequestFailedException e) {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
            } 
        }

        private object GetFlat(string json)
        {
            IEnumerable<(string Path, JsonProperty P)> GetLeaves(string path, JsonProperty p)
                => p.Value.ValueKind != JsonValueKind.Object
                    ? new[] { (Path: path == null ? p.Name : path + "/" + p.Name, p) }
                    : p.Value.EnumerateObject() .SelectMany(child => GetLeaves(path == null ? p.Name : path + "/" + p.Name, child));

            using (JsonDocument document = JsonDocument.Parse(json)) // Optional JsonDocumentOptions options
                return document.RootElement.EnumerateObject()
                    .SelectMany(p => GetLeaves(null, p))
                    .ToDictionary(k => k.Path, v => v.P.Value.Clone()); //Clone so that we can use the values outside of using
        }


        public async Task<System.Net.HttpStatusCode> FlushConstructionStateForVehicle (string vehicleId) {
            try {
                ConstructionStateModel constructionState = await GetConstructionStateForVehicleFromMongoDb (vehicleId, false);
                JProperty prop = new JProperty("constructionStateChanges", JObject.Parse(constructionState.CsModel.RootElement.ToString()));
                JObject constStateObj = new JObject(prop);

                RemoteAccessService commandService = new RemoteAccessService();
                System.Net.HttpStatusCode responseCode = await commandService.SendCommandToVehicle(vehicleId, constStateObj.ToString(Newtonsoft.Json.Formatting.None));
                return(responseCode);
            }
            catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
            } 
        }
    }
}