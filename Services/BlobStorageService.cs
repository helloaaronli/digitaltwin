using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DigitalTwinApi.Model;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Security.KeyVault.Secrets;
using NUnit.Framework;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Configuration;

namespace DigitalTwinApi.Services {
    public class BlobStorage : Interfaces.IBlobStorage {

        public BlobStorage (IConfiguration configuration) {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration = null;

        public async Task<BlobStorageModel> GetFileFromBlobStorage (string storageAccount, string containerName, string vehicleId, string serviceId, string blobId) {
            string blobEndpoint = "https://" + storageAccount + ".blob.core.windows.net/" + containerName + "/" + vehicleId + "/" + serviceId + "/" + blobId;
            if(_configuration == null) {
                Console.WriteLine("Configuration is null");
            }
            string sasToken = _configuration["DigitalTwinApi-BlobStorage-Key"];

            // Create a new Blob service client with Azure AD credentials.
            AzureSasCredential sasCredential = new AzureSasCredential(sasToken);
            BlobClient blobClient = new BlobClient(new Uri(blobEndpoint), sasCredential);

            // Download and read the contents of the blob.
            try {
                // Download blob contents to a stream and read the stream.
                BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();
                string file = "";
                using (StreamReader reader = new StreamReader(blobDownloadInfo.Content, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        file += line;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Read operation succeeded");
                Console.WriteLine();
                return(new BlobStorageModel(file));
            }
            catch (RequestFailedException e) {
                // Check for a 403 (Forbidden) error. If the SAS is invalid, 
                // Azure Storage returns this error.
                if (e.Status == 403)
                {
                    Console.WriteLine("Read operation failed");
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
                return null;
            }
        } 
    }
}