using Microsoft.AspNetCore.Mvc;

namespace Cinematica.API.Services;

public interface IHelperService
{
    Task<string> UploadFile(IFormFile file, string savePath);
    Task<string> DownloadFile(string url, string savePath);
}

public class HelperService : IHelperService
{
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

            await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

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
}