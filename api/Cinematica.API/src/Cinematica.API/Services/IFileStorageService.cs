using Amazon.S3.Model;
using Amazon.S3;

namespace Cinematica.API.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;

    public LocalFileStorageService(string uploadPath)
    {
        _uploadPath = uploadPath;
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var filePath = Path.Combine(_uploadPath, file.FileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return filePath;
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

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = file.FileName,
            InputStream = file.OpenReadStream()
        };
        await _s3Client.PutObjectAsync(putRequest);
        return $"https://{_bucketName}.s3.amazonaws.com/{file.FileName}";
    }
}
