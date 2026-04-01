using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KhoaNVCB_API.Helpers;
using Microsoft.Extensions.Options;

namespace KhoaNVCB_API.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(IOptions<CloudinarySettings> config)
        {
            // Lấy chìa khóa từ appsettings.json để mở khóa Cloudinary
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                // Đọc file ảnh dưới dạng Stream
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Tự động cắt cúp ảnh về tỉ lệ chuẩn, tập trung vào khuôn mặt/chi tiết chính
                    Transformation = new Transformation().Height(600).Width(800).Crop("fill").Gravity("face"),
                    Folder = "KhoaNVCB" // Lưu vào một thư mục riêng trên mây cho gọn
                };

                // Đẩy lên mây
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}