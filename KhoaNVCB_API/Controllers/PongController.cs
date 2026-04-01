using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KhoaNVCB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Thêm api/ cho chuẩn form RESTful
    public class PingController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous] // Phải có cái này để bot không bị chặn
        public IActionResult Get()
        {
            return Ok("Pong! Server đang thức."); // Trả về mã 200 OK
        }
    }
}