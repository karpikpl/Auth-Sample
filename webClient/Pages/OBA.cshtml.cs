using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace webClient.Pages;

public class ObaModel : PageModel
{
    private readonly ILogger<RolesModel> _logger;
    private readonly IDownstreamApi _downstreamApi;
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
    private readonly IConfiguration _configuration;
    private readonly ITokenAcquisition _tokenAcquisition;

    public ObaModel(ILogger<RolesModel> logger, IDownstreamApi downstreamApi, IAuthorizationHeaderProvider authorizationHeaderProvider, IConfiguration configuration, ITokenAcquisition tokenAcquisition)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
        _authorizationHeaderProvider = authorizationHeaderProvider;
        _configuration = configuration;
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task OnGet()
    {
        try
        {
            var scopes = _configuration.GetSection("DownstreamApi:Scopes").Get<string[]>();
            
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[]{_configuration["AzureAd:ClientId"]}, tenantId: _configuration["AzureAd:TenantId"]);

            ViewData["Received-Token"] = accessToken;

            var result = await consoleClient.CustomerCode.TokenAcquisitionWorker(
                scopes: scopes,
                clientId: _configuration["AzureAd:ClientId"],
                tenantId: _configuration["AzureAd:TenantId"],
                clientSecret: _configuration["AzureAd:ClientSecret"],
                tokenFromCLient: accessToken);

            ViewData["Token-Acquisition-Result"] = result;

            // using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi", options =>
            // {
            //     options.RelativePath = "roles";

            // }).ConfigureAwait(false);
            // if (response.StatusCode == System.Net.HttpStatusCode.OK)
            // {
            //     var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //     ViewData["ApiResult"] = apiResult;
            // }
            // else
            // {
            //     var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //     ViewData["ApiResult"] = error;

            //     // throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
            // }
        }
        catch (MsalUiRequiredException mex)
        {
            // handle the exception with ConsentHandler
            // Log the exception if necessary
            // Then trigger interactive authentication
            var properties = new AuthenticationProperties { RedirectUri = Url.Content("~/") };
            Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

    }
}

