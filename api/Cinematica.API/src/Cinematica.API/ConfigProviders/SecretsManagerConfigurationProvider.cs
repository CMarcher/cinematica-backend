using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Cinematica.API.ConfigProviders;

public class SecretsManagerConfigurationProvider : ConfigurationProvider
{
    private readonly string _region;
    private readonly string[] _secretKeys;
    
    public SecretsManagerConfigurationProvider(string region, params string[] secrets)
    {
        _region = region;
        _secretKeys = secrets;
    }

    public override void Load()
    {
        Data = GetSecrets();
    }

    private Dictionary<string, string> GetSecrets()
    {
        using var secretsClient = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(_region));
        var secretMap = new Dictionary<string, string>();

        foreach (var secretKey in _secretKeys)
        {
			Console.WriteLine($"Getting secret for {secretKey}");
            var request = new GetSecretValueRequest { SecretId = secretKey };
            var secretResponse = secretsClient.GetSecretValueAsync(request).Result; 
            // Must use Result here, otherwise config won't load in time because of the yielding nature of async
            
            secretMap.Add(secretKey, secretResponse.SecretString);
            Console.WriteLine($"Successfully got secret for {secretKey}");
        }

        return secretMap;
    }
}