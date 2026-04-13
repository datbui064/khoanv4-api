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

        // 1. Thay đổi kiểu trả về từ ImageUploadResult thành UploadResult
        public async Task<UploadResult> AddPhotoAsync(IFormFile file)
        {
            if (file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName).ToLower();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            bool isImage = imageExtensions.Contains(extension);

            // 2. Khai báo biến kết quả chung
            UploadResult result;

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Height(600).Width(800).Crop("fill").Gravity("face"),
                    Folder = "KhoaNVCB"
                };
                result = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "KhoaNVCB"
                };
                result = await _cloudinary.UploadAsync(uploadParams);
            }

            // 3. Trả về result (không cần ép kiểu thủ công nữa)
            return result;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}