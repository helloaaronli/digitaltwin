using System;

namespace DigitalTwinApi.Model {
    public class BlobStorageModel {
        public Guid Id { get; set; }
        public string FileContent { get; set; }

        public BlobStorageModel (string fileContent) {
            Id = new Guid();
            FileContent = fileContent;
        }
    }
}