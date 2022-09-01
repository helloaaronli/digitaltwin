
using System.Text.Json;
using System.Threading.Tasks;


namespace DigitalTwinApi.Interfaces {
    public interface IVehicleManager {
       public  Task<JsonElement> GetVehicleList ();
        public  Task<JsonElement> GetTopicListForVehicle (string vehicleId) ;
        public  Task<JsonElement> GetTopicForVehicle (string vehicleId, string topicName);
        public  Task<bool> AddVehicle (string vehicleId);
        public  Task<bool> RemoveVehicle (string vehicleId) ;

        public  Task<bool> AddTopicForVehicle (string vehicleId, string topicName, string topic = null, int priority = 0, int ttl = 0);
        public  Task<bool> UpdateTopicForVehicle (string vehicleId, string topicName, string topic = null, int priority = 0, int ttl = 0);

        public  Task<bool> RemoveTopicForVehicle (string vehicleId, string topicName);

        public  Task<bool> UpdateLists ();
    }
}