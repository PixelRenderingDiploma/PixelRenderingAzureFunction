using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Azure.Functions.Worker.Http;
using System.Runtime.Intrinsics.Arm;


namespace PixelRenderer_AzureFunctions
{
    internal static class Helper
    {
        private static readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        private static readonly String jwksUri = "https://hlebushek.b2clogin.com/hlebushek.onmicrosoft.com/B2C_1_susi/discovery/v2.0/keys";
        static Helper()
        {
            var documentRetriever = new HttpDocumentRetriever { RequireHttps = jwksUri.StartsWith("https://") };
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(jwksUri, new OpenIdConnectConfigurationRetriever(), documentRetriever);
        }

        private static async Task<SecurityKey> GetSigningKeyAsync(string kid)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync("https://hlebushek.b2clogin.com/hlebushek.onmicrosoft.com/B2C_1_susi/discovery/v2.0/keys");
                var jsonWebKeySet = new JsonWebKeySet(response);
                var signingKey = jsonWebKeySet.Keys.FirstOrDefault(k => k.KeyId == kid);
                return signingKey;
            }

            /* var discoveryDocument = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
            if (discoveryDocument.AdditionalData.TryGetValue("keys", out var keys))
            {
                var jsonKeys = keys.ToString();
                try {
                    var jsonStr = discoveryDocument.AdditionalData.ToString();
                    var jsonWebKeySet = new JsonWebKeySet(jsonStr);
                    var signingKey = jsonWebKeySet.Keys.FirstOrDefault(k => k.KeyId == kid);
                    return signingKey;
                } catch(Exception ex) {
                    Console.Out.WriteLine(ex.Message);
                }
            }

            return nil*/
        }

        public static async Task<JwtSecurityToken> ValidateAndExtractUserAsync(Microsoft.Azure.Functions.Worker.Http.HttpRequestData req)
        {
            string authorizationHeader = req.Headers.GetValues("Authorization").FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                throw new Exception("No token provided in request headers");
            }

            string token = authorizationHeader.Substring("Bearer ".Length).Trim();

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token) ?? throw new Exception("Invalid token");

            var signingKey = await GetSigningKeyAsync(jwtToken.Header.Kid);

            TokenValidationParameters validationParameters = new()
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                IssuerSigningKey = signingKey
            };

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return (JwtSecurityToken)validatedToken;
            }
            catch (Exception)
            {

                throw new Exception("Token validation failed");
            }
        }
    }
}
