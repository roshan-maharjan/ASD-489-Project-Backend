namespace ExpenseSplitBackend.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string fileName);
        string GetPreSignedUrl(string objectKey, TimeSpan expiry);
    }
}