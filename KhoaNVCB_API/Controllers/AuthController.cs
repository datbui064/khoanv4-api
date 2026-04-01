using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(KhoaNvcbBlogDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(RegisterDto registerDto)
        {
            // 1. Kiểm tra xem tên đăng nhập đã bị ai giành chưa
            if (await _context.Accounts.AnyAsync(a => a.Username == registerDto.Username))
            {
                return BadRequest("Tên đăng nhập này đã có người sử dụng.");
            }

            // 2. Tạo tài khoản mới. 
            // Lưu ý: Tạm thời lưu mật khẩu thô để khớp với hàm Login hiện tại của bạn. 
            // Sau này hệ thống hoàn thiện, bạn nên băm (Hash) mật khẩu này ra nhé!
            var newAccount = new Account
            {
                Username = registerDto.Username,
                PasswordHash = registerDto.Password,
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                Role = "User" // KHÓA CHẾT QUYỀN MẶC ĐỊNH LÀ GUEST/USER, TUYỆT ĐỐI KHÔNG ĐỂ ADMIN
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công" });
        }
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == loginDto.Username && a.PasswordHash == loginDto.Password);

            if (account == null)
            {
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                    new Claim(ClaimTypes.Name, account.Username),
                    new Claim(ClaimTypes.Role, account.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new AuthResponseDto
            {
                Token = tokenHandler.WriteToken(token),
                FullName = account.FullName,
                Role = account.Role,
                AccountId = account.AccountId // MỚI THÊM: Gửi ID về cho Client
            });
        }
    }
}