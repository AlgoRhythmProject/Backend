using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<LectureVideo?> GetVideoInfoAsync(string identifier);
        Task<string> UploadFileAsync(string identifier, Stream content, string contentType);
        Task<(Stream stream, string contentType)> GetFileAsync(string blobName);
        Task<bool> DeleteFileAsync(string blobName);
    }
}
