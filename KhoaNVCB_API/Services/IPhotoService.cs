using CloudinaryDotNet.Actions;

namespace KhoaNVCB_API.Services
{
    public interface IPhotoService
    {
        // Hàm nhận file từ Client và trả về kết quả từ Cloudinary
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);

        // Hàm xóa ảnh trên mây (dùng khi xóa bài viết/chuyên mục)
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}