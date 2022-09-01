using DigitalTwinApi.Model;
using System.Text.Json;
using System.Threading.Tasks;

namespace DigitalTwinApi.Interfaces
{
    public interface IConstructionState
    {
        //public Task<ConstructionStateModel> GetConstructionStateForVehicleFromOracle(string vehicleId, bool returnStructuredJson);
        public Task<ConstructionStateModel> GetConstructionStateForVehicleFromMongoDb(string vehicleId, bool returnStructuredJson);

        public Task<JsonElement> QueryVehiclesList(string key, string value);

        public Task<JsonElement> QueryVehiclesCount(string key, string value);

        public Task<JsonElement> ExecuteCustomQueryList(JsonElement query);

        public Task<JsonElement> ExecuteCustomQueryCount(JsonElement query);

        public Task<System.Net.HttpStatusCode> InsertConstructionStateForVehicle (string account, string vehicleId, JsonElement payload, bool transformIntoKeyValue);

        public Task<System.Net.HttpStatusCode> FlushConstructionStateForVehicle (string vehicleId);
    }
}