using Microsoft.Identity.Client.Extensions.Msal;

internal static class CacheSettings
{
    public static readonly string CacheFileName = "todo.txt";
    public static readonly string CacheDir = MsalCacheHelper.UserRootDirectory;
    public static readonly string LinuxKeyRingSchema = "com.saulnunez.todo.credentialcache";
    public static readonly string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public static readonly string LinuxKeyRingLabel = "User credentials for todo: unofficial To-Do CLI tool";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new("ProductGroup", "MyApps");

    public static readonly string KeyChainServiceName = "todo_msal_service";
    public static readonly string KeyChainAccountName = "todo_msal_account";
}