using Microsoft.Identity.Client;

namespace consoleClient;

public static class CustomerCode
{
    public static string GetUsername()
    {
        return "VM@domain.karpala.org";
    }

    public static async Task<AuthenticationResult> TokenAcquisitionWorker(string[] scopes, string clientId, string tenantId, string clientSecret, string? tokenFromCLient)
    {
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token")
            .WithClientSecret(clientSecret)
            .Build();


        var accounts = await app.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.Username == GetUsername());

        AuthenticationResult result;
        try
        {
            result = await app.AcquireTokenSilent(scopes, account)
            .ExecuteAsync();
            return result;
        }
        catch (MsalUiRequiredException)
        {
            try
            {
                if (tokenFromCLient != null)
                {
                    var userAssertion = new UserAssertion(tokenFromCLient, "urn:ietf:params:oauth:grant-type:jwt-bearer");
                    result = await app.AcquireTokenOnBehalfOf(scopes, userAssertion)
                        .ExecuteAsync();
                }

                var interactiveApp = PublicClientApplicationBuilder.Create(clientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .WithDefaultRedirectUri()
                    .Build();

                result = await interactiveApp.AcquireTokenInteractive(scopes)
                    .WithAccount(account)
                    .ExecuteAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        catch (Exception ex)
        {
            throw;
        }

    }
}
