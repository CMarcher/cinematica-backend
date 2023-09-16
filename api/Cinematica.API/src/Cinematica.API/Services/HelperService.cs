using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Amazon;

namespace Cinematica.API.Services;

public interface IHelperService
{
    Task<string> UploadFile(IFormFile file, string savePath);
    Task<string> DownloadFile(string url, string savePath);
    Task<UserType?> FindUserByEmailAddress(string emailAddress);
    Task<UserType?> GetCognitoUser(string id);
    Task<bool> CheckTokenSub(string tokenString, string userId);
}

public class HelperService : IHelperService
{
    private readonly IConfiguration _config;
    private AmazonCognitoIdentityProviderClient _cognitoClient;
    private readonly IFileStorageService _fileStorageService;

    public HelperService(IConfiguration config, AmazonCognitoIdentityProviderClient client, IFileStorageService fileStorageService)
    {
        _config = config;
        _cognitoClient = client;
        _fileStorageService = fileStorageService;
    }

    public async Task<string> DownloadFile(string url, string savePath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            // Generate a unique filename with the original file extension
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(url);

            // Combine the savePath and the unique filename
            var fullPath = Path.Combine(savePath, fileName);

            var stream = await response.Content.ReadAsStreamAsync();
            IFormFile file = new FormFile(stream, 0, stream.Length, null, fullPath);

            //save file using IFileStorageService
            await _fileStorageService.SaveFileAsync(file);

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

        var fileStream = file.OpenReadStream();
        IFormFile newFile = new FormFile(fileStream, 0, file.Length, null, fullPath);

        //save file using IFileStorageService
        await _fileStorageService.SaveFileAsync(newFile);

        // Return the new filename
        return fileName;
    }

    // Helper function to find a user by email address (assuming that email is unique)
    public async Task<UserType?> FindUserByEmailAddress(string emailAddress)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = _config["AWS:UserPoolId"],
            Filter = "email = \"" + emailAddress + "\""
        };

        var listUsersResponse = await _cognitoClient.ListUsersAsync(listUsersRequest);

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
            UserPoolId = _config["AWS:UserPoolId"],
            Filter = "sub = \"" + id + "\""
        };

        var listUsersResponse = await _cognitoClient.ListUsersAsync(listUsersRequest);

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

    public async Task<bool> CheckTokenSub(string tokenString, string userId)
    {
        var token = new JwtSecurityToken(jwtEncodedString: tokenString);
        string sub = token.Claims.First(c => c.Type == "sub").Value;

        return sub.Equals(userId);
    }
}