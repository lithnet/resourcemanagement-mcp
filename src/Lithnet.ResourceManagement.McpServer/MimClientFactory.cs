using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.McpServer;

public class MimClientFactory
{
    private readonly Lazy<ResourceManagementClient> lazy;

    public MimClientFactory()
    {
        lazy = new Lazy<ResourceManagementClient>(CreateClient);
    }

    public ResourceManagementClient GetClient()
    {
        return lazy.Value;
    }

    private static ResourceManagementClient CreateClient()
    {
        var options = new ResourceManagementClientOptions();

        string baseAddress = Environment.GetEnvironmentVariable("MIM_BASE_ADDRESS");
        if (!string.IsNullOrEmpty(baseAddress))
        {
            options.BaseUri = baseAddress;
        }
        else
        {
            options.BaseUri = "http://localhost:5725";
        }

        string connectionMode = Environment.GetEnvironmentVariable("MIM_CONNECTION_MODE");
        if (!string.IsNullOrEmpty(connectionMode))
        {
            if (!Enum.TryParse<ConnectionMode>(connectionMode, ignoreCase: true, out var parsedMode))
            {
                throw new InvalidOperationException(
                    $"Invalid value '{connectionMode}' for MIM_CONNECTION_MODE. " +
                    $"Valid values are: {string.Join(", ", Enum.GetNames(typeof(ConnectionMode)))}.");
            }

            options.ConnectionMode = parsedMode;
        }

        string username = Environment.GetEnvironmentVariable("MIM_USERNAME");
        if (!string.IsNullOrEmpty(username))
        {
            options.Username = username;

            string password = Environment.GetEnvironmentVariable("MIM_PASSWORD");
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }
        }

        string spn = Environment.GetEnvironmentVariable("MIM_SPN");
        if (!string.IsNullOrEmpty(spn))
        {
            options.Spn = spn;
        }

        string rmcHostExe = Environment.GetEnvironmentVariable("MIM_RMC_HOST_EXE");
        if (!string.IsNullOrEmpty(rmcHostExe))
        {
            options.RmcHostExe = rmcHostExe;
        }

        return new ResourceManagementClient(options);
    }
}
