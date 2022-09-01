using System;

namespace DigitalTwinApi.Utils {
    public static class AuthenticationToken {

        public static string GenerateAuthToken (string verb, string resourceId, string resourceType, string key, string keyType, string tokenVersion, string date) {
            //TODO: check if all variables are set
            if (string.IsNullOrWhiteSpace(key)) {
                Console.WriteLine("Error: Master key is invalid => ", key);
            }

            var hmacSha256 = new System.Security.Cryptography.HMACSHA256 { Key = Convert.FromBase64String(key) };

            string payLoad = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\n{1}\n{2}\n{3}\n{4}\n",
                verb.ToLowerInvariant(),
                resourceType.ToLowerInvariant(),
                resourceId,
                date.ToLowerInvariant(),
                ""
            );

            byte[] hashPayLoad = hmacSha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payLoad));
            string signature = Convert.ToBase64String(hashPayLoad);

            return System.Web.HttpUtility.UrlEncode(String.Format(System.Globalization.CultureInfo.InvariantCulture, "type={0}&ver={1}&sig={2}",
                keyType,
                tokenVersion,
                signature));
        }

    }
}