using Amazon.S3.Model;
using Amazon.S3;

namespace Cinematica.API.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
}

public class LocalFileStorageService : IFileStorageService
{
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        await using var stream = new FileStream(file.FileName, FileMode.Create);
        await file.CopyToAsync(stream);
        return file.FileName;
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
        return file.FileName;
    }
}
