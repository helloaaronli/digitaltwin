
using System.Text.Json;
using System.Threading.Tasks;


namespace DigitalTwinApi.Interfaces {
    public interface IRemoteAccess {
        public Task<System.Net.HttpStatusCode> SendCommandToVehicle (string vehicleId, string command);

    }
}