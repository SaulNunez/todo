using Microsoft.Graph;
using todo;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Extensions.Msal;

public static class Auth
{
    static readonly string[] scopes = new[] { "https://graph.microsoft.com/Tasks.ReadWrite" };
    static readonly string MultiTenantId = "common";
    static readonly string GraphApiUrl = "https://graph.microsoft.com/v1.0";

    public enum LoginMethod
    {
        Redirect,
        Code,
        Windows
    }

    public static async Task<string?> Login(AzureADOauth config, LoginMethod method)
    {
        var clientId = config.ClientId;
        var tenantId = "common";

        IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithDefaultRedirectUri()
                    .Build();

        // Building StorageCreationProperties
        var storageProperties =
             new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.CacheDir)
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

        try
        {
            var result = method switch
            {
                LoginMethod.Windows => await app.AcquireTokenByIntegratedWindowsAuth(scopes).ExecuteAsync(),
                LoginMethod.Redirect => await app.AcquireTokenInteractive(scopes).ExecuteAsync(),
                _ => await app.AcquireTokenWithDeviceCode(scopes, deviceCodeResult =>
                {
                    Console.WriteLine(deviceCodeResult.Message);
                    return Task.FromResult(0);
                }).ExecuteAsync()
            };

            return result.AccessToken;
        }
        catch (MsalException ex)
        {
            Console.WriteLine($"Error acquiring token:\n{ex.Message}");
        }

        return null;
    }

    public static async Task<string?> SignInSilently(AzureADOauth config)
    {
        var clientId = config.ClientId;
        var tenantId = "common";

        IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithDefaultRedirectUri()
                    .Build();

        // Building StorageCreationProperties
        var storageProperties =
             new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.CacheDir)
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

        AuthenticationResult? authResult;
        try
        {
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                                                  .ExecuteAsync();
        }
        catch (MsalUiRequiredException ex)
        {
            return await Login(config, LoginMethod.Redirect);
        }

        return authResult.AccessToken;
    }

    public static Task<GraphServiceClient> LoginUserAsync(AzureADOauth config)
    {
        return Task.FromResult(new GraphServiceClient(GraphApiUrl, new DelegateAuthenticationProvider(async (requestMessage) =>
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInSilently(config));
        })));
    }


}