#nullable disable

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DigitalTwinApi.Utils;
using Microsoft.Extensions.Configuration;

namespace DigitalTwinApi.Services {
    public class RemoteAccessService: Interfaces.IRemoteAccess {

        public static IConfiguration Configuration;

        public RemoteAccessService () { }

        public async Task<System.Net.HttpStatusCode> SendCommandToVehicle (string vehicleId, string command) {
            string domainName = Constants.VWAC_API_DOMAIN_NAME;
            string apiVersion = Constants.VWAC_API_COMMAND_API_VERSION;
            string commandName = Constants.COMMAND_NAME;

            string commandEndpoint = $"https://{domainName}/commands/api/command/{commandName}/vehicle/{vehicleId}?api-version={apiVersion}";

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, commandEndpoint);
            string userKey = Configuration["userKey"];
            message.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
            BearerToken_Request BearerToken_Request = new BearerToken_Request(Configuration);
            string token = "bearer " + await BearerToken_Request.getCommandToken(false);
            message.Headers.Add("Authorization", token);
            message.Content = new StringContent(command, Encoding.UTF8, "application/json");
            
            HttpResponseMessage responseMessage = await httpClient.SendAsync(message);
            Console.WriteLine("[{0}] The status code result of HTTP-Call was: {1}", vehicleId, responseMessage.StatusCode);
            Console.WriteLine("[{0}] The result of HTTP-Call was: {1}", vehicleId, responseMessage);

            if((int)responseMessage.StatusCode == 401) {
                Console.WriteLine("responseMessage.StatusCode of Command = 401");
                HttpClient newHttpClient = new HttpClient();
                HttpRequestMessage newMessage = new HttpRequestMessage(HttpMethod.Post, commandEndpoint);
                BearerToken_Request = new BearerToken_Request(Configuration);
                token = "bearer " + await BearerToken_Request.getCommandToken(true);
                newMessage.Headers.Add("Ocp-Apim-Subscription-Key", userKey);
                newMessage.Headers.Add("Authorization", token);
                newMessage.Content = new StringContent(command, Encoding.UTF8, "application/json");
                responseMessage = await newHttpClient.SendAsync(message);
                if ((int)responseMessage.StatusCode != 200) {
                    Console.WriteLine("Error using vwac paas user management api! Even new token is invalid!");
                }
            }

            return responseMessage.StatusCode;
        }

    }
}