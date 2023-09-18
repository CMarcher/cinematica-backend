namespace Cinematica.API.ConfigProviders;

public class AmazonSecretManagerConfigurationSource : IConfigurationSource
{
    private readonly string _region;
    private readonly string[] _secretKeys;

    public AmazonSecretManagerConfigurationSource(string region, params string[] secretKeys)
    {
        _region = region;
        _secretKeys = secretKeys;
    }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretsManagerConfigurationProvider(_region, _secretKeys);
    }
}

public static class ConfigurationExtensions
{
    public static void AddSecretsManager(this IConfigurationBuilder configurationBuilder, string region, params string[] secretKeys)
    {
        var configurationSource = new AmazonSecretManagerConfigurationSource(region, secretKeys);

        configurationBuilder.Add(configurationSource);
    }
}