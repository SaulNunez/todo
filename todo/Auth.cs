using Microsoft.Graph;
using todo;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Extensions.Msal;
using Azure.Identity;

public static class Auth
{
    static readonly string[] scopes = new[] { "https://graph.microsoft.com/Tasks.ReadWrite" };
    static readonly string GraphApiUrl = "https://graph.microsoft.com/v1.0";

    public static async Task SetupLoginPersistanceAsync(AzureADOauth config, IPublicClientApplication app)
    {
        // Building StorageCreationProperties
        var storageProperties =
             new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.GetCacheDir())
             .WithLinuxKeyring(
                 CacheSettings.LinuxKeyRingSchema,
                 CacheSettings.LinuxKeyRingCollection,
                 CacheSettings.LinuxKeyRingLabel,
                 CacheSettings.LinuxKeyRingAttr1,
                 CacheSettings.LinuxKeyRingAttr2)
             .WithMacKeyChain(
                 CacheSettings.KeyChainServiceName,
                 CacheSettings.KeyChainAccountName)
             .Build();

        // This hooks up the cross-platform cache into MSAL
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);
    }

    public static async Task<AuthenticationResult> SignInSilently(AzureADOauth config)
    {
        var clientId = config.ClientId;
        var tenantId = "common";

        IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithDefaultRedirectUri()
                    .Build();

        await SetupLoginPersistanceAsync(config, app);

        AuthenticationResult? authResult;
        try
        {
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                                                  .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            return await AcquireByDeviceCodeAsync(app);
        }

        return authResult;
    }

    private static async Task<AuthenticationResult> AcquireByDeviceCodeAsync(IPublicClientApplication app)
    {
        var result = await app.AcquireTokenWithDeviceCode(scopes,
            deviceCodeResult =>
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
                    Console.WriteLine(deviceCodeResult.Message);
                return Task.FromResult(0);
            }).ExecuteAsync();

        //Console.WriteLine(result.Account.Username);
        return result;
    }

    public static GraphServiceClient CreateGraphClient(AuthenticationResult result)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

        return new GraphServiceClient(httpClient);
    }
}