using System.Threading.Tasks;
using DigitalTwinApi.Model;


namespace DigitalTwinApi.Interfaces {
    public interface IBlobStorage {
        public Task<BlobStorageModel> GetFileFromBlobStorage (string storageAccount, string containerName, string vehicleId, string serviceId, string blobId);
    }    
}