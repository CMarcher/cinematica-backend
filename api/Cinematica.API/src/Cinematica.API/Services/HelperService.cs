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
    Task<string> UploadFile(string file, string savePath, string fileExt);
    Task<string> DownloadFile(string url, string savePath);
    string GetExtension(string contentType);
    Task<UserType?> FindUserByEmailAddress(string emailAddress);
    Task<UserType?> GetCognitoUser(string id);
    Tuple<bool, string> CheckTokenSub(string tokenString, string userId);
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
            var stream = await response.Content.ReadAsStreamAsync();
            IFormFile file = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(url));

            // Return the new filename
            return await _fileStorageService.SaveFileAsync(file, savePath);
        }
        else
        {
            throw new Exception("Failed to download file.");
        }
    }

    public async Task<string> UploadFile(string file, string savePath, string fileExt)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded.");
        }

        // Convert base64 string to byte array
        var imageBytes = Convert.FromBase64String(file);

        // Save the byte array to a memory stream
        await using var memoryStream = new MemoryStream(imageBytes);

        // Convert MemoryStream to IFormFile
        var savefile = new FormFile(memoryStream, 0, memoryStream.Length, null, "image" + fileExt)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        Console.WriteLine($"Size of file being uploaded is {file.Length / 1024} KiB.");

        // Return the new filename
        return await _fileStorageService.SaveFileAsync(savefile, savePath);
    }

    // Helper function to find a user by email address (assuming that email is unique)
    public async Task<UserType?> FindUserByEmailAddress(string emailAddress)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = _config["UserPoolId"],
            Filter = "email = \"" + emailAddress + "\""
        };

        var listUsersResponse = await _cognitoClient.ListUsersAsync(listUsersRequest);

        if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            var users = listUsersResponse.Users;
            return users.FirstOrDefault();
        }
        
        return null;
    }

    // Helper function to find a cognito user by id
    public async Task<UserType?> GetCognitoUser(string id)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = _config["UserPoolId"],
            Filter = "sub = \"" + id + "\""
        };

        var listUsersResponse = await _cognitoClient.ListUsersAsync(listUsersRequest);

        if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            var users = listUsersResponse.Users;
            return users.FirstOrDefault();
        }
        
        return null;
    }

    public Tuple<bool, string> CheckTokenSub(string tokenString, string userId)
    {
        var token = new JwtSecurityToken(jwtEncodedString: tokenString.Split(" ")[1]);
        string sub = token.Claims.First(c => c.Type == "sub").Value;

        if (sub.Equals(userId))
            return Tuple.Create(true, "");
        
        return Tuple.Create(false, "Sub in the IdToken doesn't match user id in request body.");
    }

    public string GetExtension(string contentType)
    {
        switch (contentType)
        {
            case "image/jpeg":
                return ".jpg";
            case "image/png":
                return ".png";
            case "image/gif":
                return ".gif";
            default:
                throw new ArgumentException("Content type not recognized.", nameof(contentType));
        }
    }
}