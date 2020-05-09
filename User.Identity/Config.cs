using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity
{
    public class Config
    {
        public static IEnumerable<Client> GetClients()
        {
            //返回给客户端携带的token的信息
            return new List<Client>()
            {
                new Client()
            {
                ClientId = "android",
                ClientSecrets = new List<Secret>()
                {
                    new Secret("secret".Sha256())
                },
                RefreshTokenExpiration = TokenExpiration.Sliding,
                AllowOfflineAccess = true,
                RequireClientSecret = false,
                AllowedGrantTypes = new List<string>()
                {
                    "sms_auth_code"
                },
                AlwaysIncludeUserClaimsInIdToken = true,
                AllowedScopes = new List<string>
                {
                    "gateway_api",
                    "user_api",
                    "contact_api",
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.OfflineAccess

                }
            },
        };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("gateway_api"),
                new ApiResource("user_api"),
                new ApiResource("contact_api")
            };
        }
    }
}
