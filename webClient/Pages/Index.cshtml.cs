using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;

namespace webClient.Pages;

[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly IDownstreamApi _downstreamApi;

    public IndexModel(ILogger<IndexModel> logger,
                        IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

    public async Task OnGet()
    {
        using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi", options =>
        {
            options.RelativePath = "/weatherforecast";
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
}
