using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using todo;

public static class Auth
{
    static readonly string[] scopes = new[] { "User.Read", "User.Write" };
    static readonly string MultiTenantId = "common";

    public static GraphServiceClient LoginUser(AzureADOauth config)
    {
        var options = new TokenCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };

        Func<DeviceCodeInfo, CancellationToken, Task> callback = (code, cancellation) => {
            Console.WriteLine("To continue, enter code below to https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential:");
            Console.WriteLine(code.Message);
            return Task.FromResult(0);
        };

        var credential = new DeviceCodeCredential(callback, MultiTenantId, config.ClientId, options);
        return new GraphServiceClient(credential, scopes);
    }
}