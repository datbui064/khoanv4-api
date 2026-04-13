using CloudinaryDotNet.Actions;

namespace KhoaNVCB_API.Services
{
    public interface IPhotoService
    {
        // Đổi từ Task<ImageUploadResult> thành Task<UploadResult>
        Task<UploadResult> AddPhotoAsync(IFormFile file);

        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}