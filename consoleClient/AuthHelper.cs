using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;

using Console = Colorful.Console;

namespace consoleClient;

public class AuthHelper
{
    #region Token Cache    
    // Cache settings from https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/main/sample/ManualTestApp/Config.cs
    public const string CacheFileName = "myapp_msal_cache.txt";
    public readonly static string CacheDir = MsalCacheHelper.UserRootDirectory;

    public const string KeyChainServiceName = "myapp_msal_service";
    public const string KeyChainAccountName = "myapp_msal_account";

    public const string LinuxKeyRingSchema = "com.contoso.devtools.tokencache";
    public const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public const string LinuxKeyRingLabel = "MSAL token cache for all Contoso dev tool apps.";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");
    #endregion

    public IConfiguration Configuration { get; }
    public string[] ApiScopes { get; }

    public AuthHelper(IConfiguration configuration)
    {
        Configuration = configuration;
        ApiScopes = Configuration.GetSection("DownstreamApi:Scopes").Get<string[]>();
    }

    private async Task SetupTokenCache(ITokenCache tokenCache)
    {
        // Building StorageCreationProperties
        // Based on https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        var storageProperties =
              new StorageCreationPropertiesBuilder(AuthHelper.CacheFileName, AuthHelper.CacheDir)
              .WithCacheChangedEvent(Configuration["AzureAd:ClientId"])
              .WithLinuxKeyring(
                 AuthHelper.LinuxKeyRingSchema,
                 AuthHelper.LinuxKeyRingCollection,
                 AuthHelper.LinuxKeyRingLabel,
                 AuthHelper.LinuxKeyRingAttr1,
                 AuthHelper.LinuxKeyRingAttr2)
              .WithMacKeyChain(
                 AuthHelper.KeyChainServiceName,
                 AuthHelper.KeyChainAccountName)
              .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(tokenCache);
    }

    // public app is not capable of maintaining secrets (e.g. angular app, mobile app, desktop app)
    private async Task<IPublicClientApplication> BuildPublicClientApp(bool useTokenCache = false, Action<PublicClientApplicationBuilder> configureBuilder = null)
    {
        // build the AAd authority Url
        var instance = Configuration["AzureAd:Instance"];
        var tenantId = Configuration["AzureAd:TenantId"];
        string authority = string.Concat(instance, tenantId);
        var clientId = Configuration["AzureAd:ClientId"];

        // Initialize the MSAL library by building a public client application
        var applicationBuilder = PublicClientApplicationBuilder.Create(clientId)
                                                .WithAuthority(authority)
                                                .WithDefaultRedirectUri();

        configureBuilder?.Invoke(applicationBuilder);

        var application = applicationBuilder.Build();

        // This hooks up the cross-platform cache into MSAL
        if (useTokenCache)
        {
            await SetupTokenCache(application.UserTokenCache);
        }

        return application;
    }

    public static async Task<AuthenticationResult> ErrorHandledCall(Func<Task<AuthenticationResult>> func)
    {
        try
        {
            return await func();
        }
        catch (MsalUiRequiredException ex)
        {
            // MsalUiRequiredException: AADSTS65001: The user or administrator has not consented to use the application
            // with ID '{appId}' named '{appName}'.Send an interactive authorization request for this user and resource.

            // you need to get user consent first. This can be done, if you are not using .NET Core (which does not have any Web UI)
            // by doing (once only) an AcquireToken interactive.

            // If you are using .NET core or don't want to do an AcquireTokenInteractive, you might want to suggest the user to navigate
            // to a URL to consent: https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&scope=user.read

            // AADSTS50079: The user is required to use multi-factor authentication.
            // There is no mitigation - if MFA is configured for your tenant and AAD decides to enforce it,
            // you need to fallback to an interactive flows such as AcquireTokenInteractive or AcquireTokenByDeviceCode
            Console.WriteLine($"MsalUiRequiredException Error Acquiring Token:{Environment.NewLine}{ex}", Color.Red);
        }
        catch (MsalServiceException ex)
        {
            // Kind of errors you could have (in ex.Message)

            // MsalServiceException: AADSTS90010: The grant type is not supported over the /common or /consumers endpoints. Please use the /organizations or tenant-specific endpoint.
            // you used common.
            // Mitigation: as explained in the message from Azure AD, the authority needs to be tenanted or otherwise organizations

            // MsalServiceException: AADSTS70002: The request body must contain the following parameter: 'client_secret or client_assertion'.
            // Explanation: this can happen if your application was not registered as a public client application in Azure AD
            // Mitigation: in the Azure portal, edit the manifest for your application and set the `allowPublicClient` to `true`
            Console.WriteLine($"MsalServiceException Error Acquiring Token: {Environment.NewLine}{ex}", Color.Red);
        }
        catch (MsalClientException ex)
        {
            // Error Code: unknown_user Message: Could not identify logged in user
            // Explanation: the library was unable to query the current Windows logged-in user or this user is not AD or AAD
            // joined (work-place joined users are not supported).

            // Mitigation 1: on UWP, check that the application has the following capabilities: Enterprise Authentication,
            // Private Networks (Client and Server), User Account Information

            // Mitigation 2: Implement your own logic to fetch the username (e.g. john@contoso.com) and use the
            // AcquireTokenByIntegratedWindowsAuth form that takes in the username

            // Error Code: integrated_windows_auth_not_supported_managed_user
            // Explanation: This method relies on a protocol exposed by Active Directory (AD). If a user was created in Azure
            // Active Directory without AD backing ("managed" user), this method will fail. Users created in AD and backed by
            // AAD ("federated" users) can benefit from this non-interactive method of authentication.
            // Mitigation: Use interactive authentication
            Console.WriteLine($"MsalClientException Error Acquiring Token:{Environment.NewLine}{ex}", Color.Red);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Acquiring Token:{Environment.NewLine}{ex}", Color.Red);
        }

        return null;
    }

    public async Task<string> GetTokenSilentlyFromCacheAsync()
    {
        var application = await BuildPublicClientApp(useTokenCache: true);
        AuthenticationResult result = null;

        var accounts = await application.GetAccountsAsync();

        if (accounts.Any())
        {
            // Try to acquire an access token from the cache. If device code is required, Exception will be thrown.

            result = await ErrorHandledCall(() => application.AcquireTokenSilent(ApiScopes, accounts.FirstOrDefault())
                .ExecuteAsync());
        }
        else
        {
            Console.WriteLine($"No accounts found in cache", Color.Red);
        }

        return result?.AccessToken;
    }

    public async Task<string> GetTokenUsingWindowsAuthAsync(bool useTokenCache = false)
    {
        var application = await BuildPublicClientApp(useTokenCache);

        // use windows auth to get the token
        AuthenticationResult result = await ErrorHandledCall(() =>
            application.AcquireTokenByIntegratedWindowsAuth(ApiScopes)
            .WithUsername("VM@domain.karpala.org")
           .ExecuteAsync(CancellationToken.None)
           );

        return result?.AccessToken;
    }

    public async Task<string> GetTokenUsingDeviceCodeAsync(bool useTokenCache = false)
    {
        var application = await BuildPublicClientApp(useTokenCache);
        AuthenticationResult result = await ErrorHandledCall(() => application.AcquireTokenWithDeviceCode(ApiScopes, deviceCodeResult =>
            {
                // This will print the message on the console which tells the user where to go sign-in using
                // a separate browser and the code to enter once they sign in.
                // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                // device code callback to look for the successful login of the user via that browser.
                // This background polling (whose interval and timeout data is also provided as fields in the
                // deviceCodeCallback class) will occur until:
                // * The user has successfully logged in via browser and entered the proper code
                // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                Console.WriteLine(deviceCodeResult.Message, Color.Green);
                return Task.FromResult(0);
            }).ExecuteAsync());

        return result?.AccessToken;
    }

    public async Task<string> GetTokenUsingInteractiveAsync(IEnumerable<string> scopes = null, bool useTokenCache = false)
    {
        var scopesForToken = scopes == null ? ApiScopes : scopes;

        var application = await BuildPublicClientApp(useTokenCache);
        AuthenticationResult result = await ErrorHandledCall(() => application.AcquireTokenInteractive(scopesForToken)
            .ExecuteAsync(CancellationToken.None));

        return result?.AccessToken;
    }

    public async Task<string> GetTokenUsingWAMAsync(IEnumerable<string>? scopes = null, bool useTokenCache = false, IntPtr? handle = default)
    {
        var scopesForToken = scopes == null ? ApiScopes : scopes;

        // https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-acquire-token-wam#wam-calling-pattern
        var application = await BuildPublicClientApp(useTokenCache, builder =>
        {
            builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            builder.WithExperimentalFeatures();
            builder.WithParentActivityOrWindow(()=> handle ?? GetConsoleOrTerminalWindow());
        });

        IAccount? accountToLogin = (await application.GetAccountsAsync()).FirstOrDefault();
        if (accountToLogin == null)
        {
            // 3. No account in the cache; try to log in with the OS account
            // No account in the cache; try to log in with the OS account
            var resultInteractive = await application.AcquireTokenInteractive(scopesForToken)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            return resultInteractive?.AccessToken;
        }

        AuthenticationResult result = await ErrorHandledCall(() =>  // 4. Silent authentication 
            application.AcquireTokenSilent(scopes, accountToLogin)
            .ExecuteAsync(CancellationToken.None));

        return result?.AccessToken;
    }

    enum GetAncestorFlags
    {
        GetParent = 1,
        GetRoot = 2,
        /// <summary>
        /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        /// </summary>
        GetRootOwner = 3
    }

    /// <summary>
    /// Retrieves the handle to the ancestor of the specified window.
    /// </summary>
    /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved.
    /// If this parameter is the desktop window, the function returns NULL. </param>
    /// <param name="flags">The ancestor to be retrieved.</param>
    /// <returns>The return value is the handle to the ancestor window.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    // This is your window handle!
    public IntPtr GetConsoleOrTerminalWindow()
    {
        IntPtr consoleHandle = GetConsoleWindow();
        IntPtr handle = GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

        return handle;
    }

}
