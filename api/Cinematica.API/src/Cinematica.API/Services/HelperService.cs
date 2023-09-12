using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using System.Net;
using Amazon;

namespace Cinematica.API.Services;

public interface IHelperService
{
    Task<string> UploadFile(IFormFile file, string savePath);
    Task<string> DownloadFile(string url, string savePath);
    Task<UserType?> FindUserByEmailAddress(string emailAddress);
    Task<UserType?> GetCognitoUser(string id);
}

public class HelperService : IHelperService
{
    private readonly IConfiguration APP_CONFIG;
    private AmazonCognitoIdentityProviderClient cognitoIdClient;

    public HelperService(IConfiguration config)
    {
        APP_CONFIG = config.GetSection("AWS");

        cognitoIdClient = new AmazonCognitoIdentityProviderClient
        (
            APP_CONFIG["AccessKeyId"],
            APP_CONFIG["AccessSecretKey"],
            RegionEndpoint.GetBySystemName(APP_CONFIG["Region"])
        );
    }

    public async Task<string> DownloadFile(string url, string savePath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Generate a unique filename with the original file extension
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(url);

            // Combine the savePath and the unique filename
            var fullPath = Path.Combine(savePath, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes);

            // Return the new filename
            return fileName;
        }
        else
        {
            throw new Exception("Failed to download file.");
        }
    }

    public async Task<string> UploadFile(IFormFile file, string savePath)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded.");
        }

        // Generate a unique filename
        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

        // Combine the savePath and the unique filename
        string fullPath = Path.Combine(savePath, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return the new filename
        return fileName;
    }

    // Helper function to find a user by email address (assuming that email is unique)
    public async Task<UserType?> FindUserByEmailAddress(string emailAddress)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = APP_CONFIG["UserPoolId"],
            Filter = "email = \"" + emailAddress + "\""
        };

        var listUsersResponse = await cognitoIdClient.ListUsersAsync(listUsersRequest);

        if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            var users = listUsersResponse.Users;
            return users.FirstOrDefault();
        }
        else
        {
            return null;
        }
    }

    // Helper function to find a cognito user by id
    public async Task<UserType?> GetCognitoUser(string id)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = APP_CONFIG["UserPoolId"],
            Filter = "sub = \"" + id + "\""
        };

        var listUsersResponse = await cognitoIdClient.ListUsersAsync(listUsersRequest);

        if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            var users = listUsersResponse.Users;
            return users.FirstOrDefault();
        }
        else
        {
            return null;
        }
    }
}