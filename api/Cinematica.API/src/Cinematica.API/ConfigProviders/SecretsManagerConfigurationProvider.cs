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

    public override async void Load()
    {
        Data = await GetSecretsAsync();
    }

    private async Task<Dictionary<string, string>> GetSecretsAsync()
    {
        using var secretsClient = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(_region));
        var secretMap = new Dictionary<string, string>();

        foreach (var secretKey in _secretKeys)
        {
            var request = new GetSecretValueRequest { SecretId = secretKey, VersionStage = "AWSCURRENT" };
            var secretResponse = await secretsClient.GetSecretValueAsync(request);
            
            secretMap.Add(secretKey, secretResponse.SecretString);
        }

        return secretMap;
    }
}