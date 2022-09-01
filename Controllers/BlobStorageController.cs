using System.Threading.Tasks;
using DigitalTwinApi.Model;
using DigitalTwinApi.Services;
using DigitalTwinApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DigitalTwinApi.Interfaces;


namespace DigitalTwinApi.Controllers {
    [Route("[controller]")]
    [ApiController]
    public class BlobStorageController : Microsoft.AspNetCore.Mvc.ControllerBase {
        private readonly ILogger<BlobStorageController> _logger;
        private readonly IBlobStorage _blobstorage;

        public BlobStorageController (ILogger<BlobStorageController> logger, IBlobStorage blobstorage) {
            _logger = logger;
            _blobstorage = blobstorage;
        }

        /// <summary>
        /// getBlob:
        /// retrieves blob item from blob storage
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("{storageAccount}/{containerName}/{vehicleId}/{serviceId}/{blobId}/largefiledownload")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetFile (string storageAccount, string containerName, string vehicleId, string serviceId, string blobId) {          
            var blobStorageFile = await _blobstorage.GetFileFromBlobStorage (storageAccount, containerName, vehicleId, serviceId, blobId);
            if(blobStorageFile == null) {
                return BadRequest();
            }
            else if(blobStorageFile.FileContent.ToString() == "") {
                return NoContent();
            }
            else{
                return Ok(blobStorageFile.FileContent);
            }
        }
    }
}