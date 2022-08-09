using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using todo;

public class Auth
{
    static string[] scopes = new[] { "User.Read", "User.Write" }; 
    public readonly GraphServiceClient graphServiceClient;

    public Auth(AzureADOauth config)
    {
        var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions()
        {
            TenantId = config.TenantId,
            ClientId = config.ClientId,
        });
        graphServiceClient = new GraphServiceClient(credential, scopes);
    }
}