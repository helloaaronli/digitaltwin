using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace DigitalTwinApi.Utils
{
    public class AuthorizationKeyFilterAttribute : Attribute, IAuthorizationFilter {
        public void OnAuthorization (AuthorizationFilterContext context) {
            const string key = Constants.APIM_AUTHORIZATION_KEY;
            const string keyValue = Constants.APIM_AUTHORIZATION_KEY_VALUE;
            IConfiguration config = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));
            string masterkeyValue = config["ApiMasterKey"];

            var apiKey = context.HttpContext.Request.Headers[Constants.APIM_AUTHORIZATION_HEADER];
            if (apiKey.Any()) {
                var subStrings = apiKey.ToString().Split(" ");
                if (!(subStrings.Length >= 2 && subStrings[0] == key && (subStrings[1] == keyValue || subStrings[1] == masterkeyValue))) {
                    context.Result = new UnauthorizedResult();
                }
            } else {
                context.Result = new UnauthorizedResult();
            }
        }
    }

    public class AuthorizationKeyFilterMasterAttribute : Attribute, IAuthorizationFilter {
        public void OnAuthorization (AuthorizationFilterContext context) {
            const string masterkey = Constants.APIM_AUTHORIZATION_KEY;
            IConfiguration config = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));
            string masterkeyValue = config["ApiMasterKey"];

            var apiKey = context.HttpContext.Request.Headers[Constants.APIM_AUTHORIZATION_HEADER];
            if (apiKey.Any()) {
                var subStrings = apiKey.ToString().Split(" ");
                if (!(subStrings.Length >= 2 && subStrings[0] == masterkey && subStrings[1] == masterkeyValue)) {
                    context.Result = new UnauthorizedResult();
                }
            } else {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}