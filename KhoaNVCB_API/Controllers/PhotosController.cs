using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KhoaNVCB_API.Services;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // BẢO MẬT: Chỉ có Giảng viên/Admin đã đăng nhập mới được phép tải ảnh lên
    [Authorize(Roles = "Admin")]
    public class PhotosController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        [HttpPost]
        public async Task<IActionResult> AddPhoto(IFormFile file)
        {
            // 1. Kiểm tra xem có file gửi lên không
            if (file == null || file.Length == 0)
            {
                return BadRequest("Không tìm thấy file ảnh hợp lệ.");
            }

            // 2. Gọi Service đẩy ảnh lên mây
            var result = await _photoService.AddPhotoAsync(file);

            // 3. Nếu Cloudinary báo lỗi (ví dụ sai API Key, sai định dạng ảnh...)
            if (result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }

            // 4. Thành công: Trả về link ảnh xịn xò có HTTPS
            return Ok(new
            {
                url = result.SecureUrl.AbsoluteUri,
                publicId = result.PublicId
            });
        }
    }
}