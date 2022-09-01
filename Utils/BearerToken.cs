using System;
using System.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using DigitalTwinApi.Model;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;


namespace DigitalTwinApi.Utils {
    public class BearerToken_Request {

        private readonly IConfiguration _configuration = null;
        
        public BearerToken_Request(IConfiguration configuration){
            _configuration = configuration;
        }
        private static string bearer_token_user;
        private static string bearer_token_command;

        private static bool firstUser = true;
        private static bool firstCommand = true;

        public async Task<string> getUserToken (bool newtoken = false) {
            
            if( _configuration == null) {
                Console.WriteLine("Configuration is null");
                return null;
            } else if (firstUser || newtoken) {
                Console.WriteLine("firstUser bearertoken access");
                firstUser = false;
                string domainName = Constants.domainName;
                string tenantId = _configuration["tenantId"];
                string clientId = _configuration["clientId"];
                string clientSecret = _configuration["clientSecret"];
                string scope = Constants.scope_user;
                string grantType = Constants.grantType;
                string bearerEndpoint = $"https://{domainName}/{tenantId}/oauth2/v2.0/token";

                var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("client_id", clientId));
                nvc.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                nvc.Add(new KeyValuePair<string, string>("scope", scope));
                nvc.Add(new KeyValuePair<string, string>("grant_type", grantType));

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, bearerEndpoint) { Content = new FormUrlEncodedContent(nvc) };        
                HttpResponseMessage responseMessage = await httpClient.SendAsync(message);

                if (!(responseMessage.Content is object)) {
                    Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
                    return null;
                }

                string response = await responseMessage.Content.ReadAsStringAsync();
                // Console.WriteLine("Response from VWAC API: {0}", response);
                
                try {
                    JObject obj = JObject.Parse(response);
                    bearer_token_user = (string) obj["access_token"];
                } catch (Exception ex) {
                    Console.WriteLine("EXCEPTION for response from VWAC API while creating user token: {0}", ex.Message);
                    return null;
                }
                return bearer_token_user;

            } else {
                Console.WriteLine("Just have bearertoken");
                return bearer_token_user;
            }
            
        }

        public async Task<string> getCommandToken (bool newtoken = false) {
            
            if( _configuration == null) {
                Console.WriteLine("Configuration is null");
                return null;
            } else if (firstCommand || newtoken) {
                Console.WriteLine("firstCommand bearertoken access");
                firstCommand = false;
                string domainName = Constants.domainName;
                string tenantId = _configuration["tenantId"];
                string clientId = _configuration["clientId"];
                string clientSecret = _configuration["clientSecret"];
                string scope = Constants.scope_command;
                string grantType = Constants.grantType;
                string bearerEndpoint = $"https://{domainName}/{tenantId}/oauth2/v2.0/token";

                var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("client_id", clientId));
                nvc.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                nvc.Add(new KeyValuePair<string, string>("scope", scope));
                nvc.Add(new KeyValuePair<string, string>("grant_type", grantType));

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, bearerEndpoint) { Content = new FormUrlEncodedContent(nvc) };        
                HttpResponseMessage responseMessage = await httpClient.SendAsync(message);

                if (!(responseMessage.Content is object)) {
                    Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
                    return null;
                }

                string response = await responseMessage.Content.ReadAsStringAsync();
                // Console.WriteLine("Response from VWAC API: {0}", response);
                
                try {
                    JObject obj = JObject.Parse(response);
                    bearer_token_command = (string) obj["access_token"];
                } catch (Exception ex) {
                    Console.WriteLine("EXCEPTION for response from VWAC API: {0}", ex.Message);
                    return null;
                }
                return bearer_token_command;

            } else {
                Console.WriteLine("Just have bearertoken");
                return bearer_token_command;
            }
            
        }
    }

}
