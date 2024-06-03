using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace webClient.Pages;

public class RolesModel : PageModel
{
    private readonly ILogger<RolesModel> _logger;
    private readonly IDownstreamApi _downstreamApi;
    // protected MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler { get; set; }

    public RolesModel(ILogger<RolesModel> logger, IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

    public async Task OnGet()
    {
        try
        {
            using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi", options =>
            {
                options.RelativePath = "roles";

            }).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ViewData["ApiResult"] = apiResult;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ViewData["ApiResult"] = error;

                // throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
            }
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

