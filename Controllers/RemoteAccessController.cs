using System;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwinApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DigitalTwinApi.Interfaces;


namespace DigitalTwinApi.Controllers {
    [Microsoft.AspNetCore.Mvc.Route("RemoteAccess")]
    [ApiController]
    public class RemoteAccessController : Microsoft.AspNetCore.Mvc.ControllerBase {
        private readonly ILogger<RemoteAccessController> _logger;
        private readonly IRemoteAccess vehicleCommandService;

        public RemoteAccessController (ILogger<RemoteAccessController> logger, IRemoteAccess vehicleCommandService) {
            _logger = logger;
            this.vehicleCommandService = vehicleCommandService;
        }

        /// <summary>
        /// Sends a subscription command to the vehicle. 
        /// </summary>
        /// <remarks>
        /// This command subscribes a topic onboard.
        /// To receive the respective values for the subscribed topic from onboard, you need to subscribe to your vehicle at the MQTT-Broker.
        /// The response is sent periodically until you send an unsubscribe command with the same "consumerName" you used for the subscribe command.<br />
        /// MQTT-Broker address: ditwin-mqtt-telemetry.germanywestcentral.azurecontainer.io
        /// Topic to subscribe: "vehicles/&lt; YourVehicleID &gt;"<br />
        /// For further information visit: https://dev.azure.com/vwac/Digital%20Twin/_wiki/wikis/Digital-Twin.wiki/59480/Remote-Access
        ///
        /// Subscription sample request:
        ///
        ///     {
        ///         "consumerName": "BatteryStatus",
        ///         "priority": 10,
        ///         "telemetry": [
        ///             {
        ///                 "subscribe": "cso/v0/vehicle/battery/highVoltage/chargingLevel0",
        ///                 "TTL": 200
        ///             }
        ///         ]
        ///     }
        ///
        /// Unsubscription sample request:
        ///
        ///     {
        ///         "consumerName": "BatteryStatus",
        ///         "priority": 10,
        ///         "telemetry": [
        ///             {
        ///                 "unsubscribe": "cso/v0/vehicle/battery/highVoltage/chargingLevel0"
        ///             }
        ///         ]
        ///     }
        ///
        /// </remarks>
        /// <response code="202">Accepted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Enqueuing error</response>
        [HttpPost]
        [Route("subscribe/{vehicleId}")] // e.g. https://localhost:5001/vehicles/TeamConnectVehicle01/VehicleCommand
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SendSubscriptionPayloadToVehicle (string vehicleId, JsonElement payload) {
            Console.WriteLine("PAYLOAD: " + payload.ToString());
            System.Net.HttpStatusCode result = await vehicleCommandService.SendCommandToVehicle(vehicleId, payload.ToString());

            if (result != System.Net.HttpStatusCode.Accepted) {
                return NotFound(string.Format("Enqueuing command for vehicle '{0}' failed: {1}", vehicleId, result));
            }

            return Accepted(string.Format("Succesfully enqueued command for vehicle '{0}'!", vehicleId));
        }


        /// <summary>
        /// Sends a command to the vehicle. Used to send a request to trigger some onboard function as soon as some other topic is published from onboard to EdgeTwin. 
        /// </summary>
        /// <remarks>
        /// This command is used to send a request to trigger some onboard function every time EdgeTwin receives the specified notification topic from onboard. If supported by the function, you will receive the function result subsequently.
        /// To receive the function result from onboard, you need to subscribe to your vehicle at the MQTT-Broker.
        /// The response is sent periodically until you send an unnotify command with the same "consumerName" you used for the notification command.<br />
        /// MQTT-Broker address: ditwin-mqtt-telemetry.germanywestcentral.azurecontainer.io
        /// Topic to subscribe: "vehicles/&lt; YourVehicleID &gt;"<br />
        /// For further information visit: https://dev.azure.com/vwac/Digital%20Twin/_wiki/wikis/Digital-Twin.wiki/59480/Remote-Access
        ///
        /// Notification/Request sample:
        ///
        ///     {
        ///         "consumerName": "BatteryStatus",
        ///         "priority": 10,
        ///         "service": [
        ///             {
        ///                 "notify": "cso/v0/vehicle/battery/highVoltage/chargingLevel0",
        ///                 "request": "cso/v0/vehicle/battery/highVoltage/chargingLevel0/requestFunction",
        ///                 "TTL": 200
        ///             }
        ///         ]
        ///     }
        ///
        /// Unnotification sample:
        ///
        ///     {
        ///         "consumerName": "BatteryStatus",
        ///         "priority": 10,
        ///         "service": [
        ///             {
        ///                 "unnotify": "cso/v0/vehicle/battery/highVoltage/chargingLevel0",
        ///                 "request": "cso/v0/vehicle/battery/highVoltage/chargingLevel0/requestFunction"
        ///             }
        ///         ]
        ///     }
        ///
        /// </remarks>
        /// <response code="202">Accepted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Enqueuing error</response>
        [HttpPost]
        [Route("trigger/{vehicleId}")] // e.g. https://localhost:5001/vehicles/TeamConnectVehicle01/VehicleCommand
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SendServicePayloadToVehicle (string vehicleId, JsonElement payload) {
            Console.WriteLine("PAYLOAD: " + payload.ToString());
            System.Net.HttpStatusCode result = await vehicleCommandService.SendCommandToVehicle(vehicleId, payload.ToString());

            if (result != System.Net.HttpStatusCode.Accepted) {
                return NotFound(string.Format("Enqueuing command for vehicle '{0}' failed: {1}", vehicleId, result));
            }

            return Accepted(string.Format("Succesfully enqueued command for vehicle '{0}'!", vehicleId));
        }


        /// <summary>
        /// Sends a command to the vehicle. Used to trigger an onboard function.
        /// </summary>
        /// <remarks>
        /// This is a fire and forget command used to call an onboard function. There will be no response from onboard whether the function was started successfully or not.<br />
        /// For further information visit: https://dev.azure.com/vwac/Digital%20Twin/_wiki/wikis/Digital-Twin.wiki/59480/Remote-Access
        ///
        /// Command with payload sample request:
        /// 
        ///     {
        ///         "topic": "cso/customTopic",
        ///         "commandPayload": "customCommandPayload"
        ///     }
        ///
        /// </remarks>
        /// <response code="202">Accepted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Enqueuing error</response>
        [HttpPost]
        [Route("call/{vehicleId}")] // e.g. https://localhost:5001/vehicles/TeamConnectVehicle01/VehicleCommand
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SendCommandPayloadToVehicle (string vehicleId, JsonElement payload) {
            Console.WriteLine("PAYLOAD: " + payload.ToString());
            System.Net.HttpStatusCode result = await vehicleCommandService.SendCommandToVehicle(vehicleId, payload.ToString());

            if (result != System.Net.HttpStatusCode.Accepted) {
                return NotFound(string.Format("Enqueuing command for vehicle '{0}' failed: {1}", vehicleId, result));
            }

            return Accepted(string.Format("Succesfully enqueued command for vehicle '{0}'!", vehicleId));
        }


        /// <summary>
        /// Sends a request to the vehicle. Used to send a request to trigger some onboard function immediately and receive a response from the function.
        /// </summary>
        /// <remarks>
        /// This is a request command used to call an onboard function and, if supported by the function, receive a response from onboard. 
        /// To receive the response for the called topic from onboard, you need to subscribe to your vehicle at the MQTT-Broker prior to sending the request
        /// because the response is only sent once.<br />
        /// MQTT-Broker address: ditwin-mqtt-telemetry.germanywestcentral.azurecontainer.io
        /// Topic to subscribe: "vehicles/&lt; YourVehicleID &gt;"<br />
        /// For further information visit: https://dev.azure.com/vwac/Digital%20Twin/_wiki/wikis/Digital-Twin.wiki/59480/Remote-Access
        ///
        /// Request with payload:
        /// 
        ///     {
        ///         "topic": "viwi/requesttest",
        ///         "requestPayload": "customRequestPayload"
        ///     }
        /// 
        /// </remarks>
        /// <response code="202">Accepted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Enqueuing error</response>
        [HttpPost]
        [Route("request/{vehicleId}")] // e.g. https://localhost:5001/vehicles/TeamConnectVehicle01/VehicleCommand
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SendRequestPayloadToVehicle (string vehicleId, JsonElement payload) {
            Console.WriteLine("PAYLOAD: " + payload.ToString());
            System.Net.HttpStatusCode result = await vehicleCommandService.SendCommandToVehicle(vehicleId, payload.ToString());

            if (result != System.Net.HttpStatusCode.Accepted) {
                return NotFound(string.Format("Enqueuing command for vehicle '{0}' failed: {1}", vehicleId, result));
            }

            return Accepted(string.Format("Succesfully enqueued command for vehicle '{0}'!", vehicleId));
        }
    }
}