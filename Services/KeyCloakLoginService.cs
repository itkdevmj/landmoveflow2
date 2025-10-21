using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using LMFS.Models;
using LMFS.Utils;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;

namespace LMFS.Services
{
    public class KeyCloakLoginService
    {

        private OidcClient _oidcClient;
        private HttpClient _apiClient;
        private string _accessToken;

        private static Logger _logger = LogManager.GetCurrentClassLogger();


        private readonly OidcClientOptions _options = new OidcClientOptions()
        {
            Authority =
                $"{LMFS.Properties.Settings.Default.KeyCloakUrl}/auth/realms/{LMFS.Properties.Settings.Default.RealmId}",
            ClientId = LMFS.Properties.Settings.Default.ClientId,
            Scope = "openid",
            RedirectUri = LMFS.Properties.Settings.Default.RedirectUri,
            Browser = new SystemBrowser(),
            DisablePushedAuthorization = true,

            Policy = new Policy
            {
                Discovery =
                {
                    RequireHttps = false,
                }
            },
        };


        public KeyCloakLoginService()
        {
            _oidcClient = new OidcClient(_options);
            _apiClient = new HttpClient();
        }


        public async Task<User> LoginTask()
        {
            try
            {
                // 2. 로그인 요청 시작 -> 브라우저가 열림
                var loginResult = await _oidcClient.LoginAsync(new LoginRequest());

                if (loginResult.IsError)
                {
                    _logger.Debug($"로그인 실패: {loginResult.Error}");
                }

                _accessToken = loginResult.AccessToken;
                _apiClient.SetBearerToken(_accessToken);

                User user = new User
                {
                    name = loginResult.User.Identity.Name,
                    isAuthenticated = loginResult.User.Identity.IsAuthenticated,
                    token = _accessToken,
                    areaCd = loginResult.User.FindFirst("areaCd")?.Value
                };
                return user;
            }
            catch (Exception ex)
            {
                _logger.Debug($"예외 발생: {ex.Message}");
            }

            return null;
        }
    }
}
