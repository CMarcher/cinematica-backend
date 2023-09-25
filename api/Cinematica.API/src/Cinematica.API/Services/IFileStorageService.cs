using Amazon.S3.Model;
using Amazon.S3;

namespace Cinematica.API.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string path);
}

public class LocalFileStorageService : IFileStorageService
{

    private readonly ImageSettings _imageSettings;

    public LocalFileStorageService(ImageSettings imageSettings)
    {
        _imageSettings = imageSettings;
    }



    public async Task<string> SaveFileAsync(IFormFile file, string path)
    {
        // Generate a unique filename
        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

        // Combine the savePath and the unique filename
        var fullPath = Path.Combine(path, fileName);
        fullPath = Path.Combine(_imageSettings.UploadLocation, fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        
        await file.CopyToAsync(stream);
        
        return fileName;
    }
}

public class S3FileStorageService : IFileStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(AmazonS3Client s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string path)
    {
        // Generate a unique filename
        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

        // Combine the savePath and the unique filename
        var fullPath = Path.Combine(path, fileName);

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fullPath,
            InputStream = file.OpenReadStream()
        };
        await _s3Client.PutObjectAsync(putRequest);
        return fileName;
    }
}
