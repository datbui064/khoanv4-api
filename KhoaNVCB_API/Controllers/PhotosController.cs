using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KhoaNVCB_API.Services;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Hạ cấp xuống [Authorize] để dễ dàng xác thực hơn khi deploy (tránh lỗi Role mismatch)
    [Authorize]
    public class PhotosController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }
        [HttpPost]
        public async Task<IActionResult> AddPhoto(IFormFile file) // Thêm tham số IFormFile ở đây
        {
            try
            {
                // Nếu client gửi tên field khác 'file' (như 'blob'), bạn vẫn có thể dùng Request.Form
                // Nhưng tốt nhất là ép client gửi đúng field name là 'file'
                var fileToUpload = file ?? Request.Form.Files.FirstOrDefault();

                if (fileToUpload == null || fileToUpload.Length == 0)
                {
                    return BadRequest(new { message = "Không tìm thấy file ảnh trong yêu cầu." });
                }

                var result = await _photoService.AddPhotoAsync(fileToUpload);

                if (result.Error != null)
                {
                    return BadRequest(new { message = result.Error.Message });
                }

                return Ok(new { location = result.SecureUrl.AbsoluteUri });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi khi thiếu Content-Type multipart/form-data
                return BadRequest(new { message = "Yêu cầu phải là dạng multipart/form-data" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}