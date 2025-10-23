using Amazon.S3;
using Amazon.S3.Model;

namespace ExpenseSplitBackend.Services
{
    public class StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public StorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:S3BucketName"]
                ?? throw new ArgumentNullException("AWS:S3BucketName not configured.");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = $"qrcodes/{fileName}",
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_bucketName}.s3.amazonaws.com/qrcodes/{fileName}";
        }

        public string GetPreSignedUrl(string objectKey, TimeSpan expiry)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expiry)
            };
            return _s3Client.GetPreSignedURL(request);
        }
    }
}