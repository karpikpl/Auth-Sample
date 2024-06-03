using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace consoleClient;

public class ConfidentialAuthHelper
{
    public IConfiguration Configuration { get; }
    public string[] Scopes { get; }

    public ConfidentialAuthHelper(IConfiguration configuration)
    {
        Configuration = configuration;

        // only one scope is supported
        var scopes = Configuration.GetSection("DownstreamApi:Scopes").Get<string[]>().First();
        var split = scopes.Split('/');
        Scopes = new string[] { $"{split[0]}//{split[2]}/.default" };
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // build the AAd authority Url
        var instance = Configuration["AzureAd:Instance"];
        var tenantId = Configuration["AzureAd:TenantId"];
        string authority = string.Concat(instance, tenantId);
        var clientId = Configuration["AzureAd:ClientId"];
        var clientSecret = Configuration["AzureAd:ClientSecret"];

        // Initialize the MSAL library by building a confidential client application (one that can hold a secret)
        var application = ConfidentialClientApplicationBuilder.Create(clientId)
                                                .WithAuthority(authority)
                                                .WithClientSecret(clientSecret)
                                                .Build();


        AuthenticationResult result = await AuthHelper.ErrorHandledCall(() => application.AcquireTokenForClient(Scopes).ExecuteAsync());

        return result.AccessToken;
    }

    public async Task<string> GetAccessTokenOnBehalfOfAsync(string token)
    {
        // build the AAd authority Url
        var instance = Configuration["AzureAd:Instance"];
        var tenantId = Configuration["AzureAd:TenantId"];
        string authority = string.Concat(instance, tenantId);
        var clientId = Configuration["AzureAd:ClientId"];
        var clientSecret = Configuration["AzureAd:ClientSecret"];

        // Initialize the MSAL library by building a confidential client application (one that can hold a secret)
        var application = ConfidentialClientApplicationBuilder.Create(clientId)
                                                .WithAuthority(authority)
                                                .WithClientSecret(clientSecret)
                                                .Build();

        // create user assertion
        var userAssertion = new UserAssertion(token, "urn:ietf:params:oauth:grant-type:jwt-bearer");

        AuthenticationResult result = await AuthHelper.ErrorHandledCall(() => application.AcquireTokenOnBehalfOf(Scopes, userAssertion).ExecuteAsync());

        return result.AccessToken;
    }
}