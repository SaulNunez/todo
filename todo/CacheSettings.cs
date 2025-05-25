using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Extensions.Msal;

internal static class CacheSettings
{
    public static readonly string CacheFileName = "todo.txt";
    public static readonly string LinuxKeyRingSchema = "com.saulnunez.todo.credentialcache";
    public static readonly string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public static readonly string LinuxKeyRingLabel = "User credentials for todo: unofficial To-Do CLI tool";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new("ProductGroup", "MyApps");

    public static readonly string KeyChainServiceName = "todo_msal_service";
    public static readonly string KeyChainAccountName = "todo_msal_account";

    private static bool IsRunningInSnap()
    {
        var snapEnv = Environment.GetEnvironmentVariable("SNAP");
        return !string.IsNullOrEmpty(snapEnv);
    }

    public static string GetCacheDir() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IsRunningInSnap())
        {
            var snapCommonUserData = Environment.GetEnvironmentVariable("SNAP_USER_COMMON");
            if (string.IsNullOrEmpty(snapCommonUserData))
            {
                throw new FileNotFoundException(
                    "Error finding location to save credentials. " +
                    "Login will not be persisted. Please try again" +
                    "Sorry for the incovenience!"
                );
            }
            return $"{snapCommonUserData}/todo_unofficial_cli";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
             return $"{Environment.GetEnvironmentVariable("HOME")}/.config/todo_unofficial_cli";
        }

        return MsalCacheHelper.UserRootDirectory;
    }
}