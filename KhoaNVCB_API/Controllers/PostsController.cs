using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public PostsController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
        {
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Content = p.Content,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                    SourceType = p.SourceType,
                    OriginalUrl = p.OriginalUrl,
                    ImageUrl = p.ImageUrl, // MỚI THÊM: Lấy ảnh bìa ra
                    Status = p.Status,
                    CreatedDate = p.CreatedDate
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<PostDto>> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.PostId == id)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Content = p.Content,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                    SourceType = p.SourceType,
                    OriginalUrl = p.OriginalUrl,
                    ImageUrl = p.ImageUrl, // MỚI THÊM: Lấy ảnh bìa ra
                    Status = p.Status,
                    CreatedDate = p.CreatedDate
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetRecentPosts(int count)
        {
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.Status == "Published") // Chỉ lấy bài đã xuất bản
                .OrderByDescending(p => p.CreatedDate) // Mới nhất lên đầu
                .Take(count) // Giới hạn số lượng (ví dụ: 6)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Content = p.Content,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Mới",
                    SourceType = p.SourceType,
                    OriginalUrl = p.OriginalUrl,
                    ImageUrl = p.ImageUrl,
                    Status = p.Status,
                    CreatedDate = p.CreatedDate
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpPost("extract-word")]
        [Authorize(Roles = "Admin")]
        public IActionResult ExtractWordText(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File không tồn tại.");
            if (!file.FileName.EndsWith(".docx")) return BadRequest("Chỉ hỗ trợ định dạng .docx");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var converter = new Mammoth.DocumentConverter();
                    var result = converter.ConvertToHtml(stream);
                    return Ok(new { text = result.Value });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi đọc file Word: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutPost(int id, CreatePostDto updateDto)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            post.Title = updateDto.Title;
            post.Slug = updateDto.Slug;
            post.Summary = updateDto.Summary;
            post.Content = updateDto.Content;
            post.CategoryId = updateDto.CategoryId;
            post.SourceType = updateDto.SourceType;
            post.OriginalUrl = updateDto.OriginalUrl;
            post.ImageUrl = updateDto.ImageUrl;

            // ---> 2 DÒNG MỚI ĐƯỢC THÊM VÀO ĐỂ NHẬN TRẠNG THÁI DUYỆT BÀI <---
            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                post.Status = updateDto.Status;
                if (updateDto.Status == "Published" && post.PublishedDate == null)
                {
                    post.PublishedDate = DateTime.Now;
                }
            }

            post.UpdatedDate = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var post = await _context.Posts
                        .Include(p => p.Tags) // Bao gồm Tags để xóa liên kết
                        .FirstOrDefaultAsync(p => p.PostId == id);

                    if (post == null) return NotFound();

                    // 1. Xóa các bình luận liên quan
                    var comments = _context.Comments.Where(c => c.PostId == id);
                    _context.Comments.RemoveRange(comments);

                    // 2. Xóa các tài nguyên bên ngoài (ExternalResources)
                    var resources = _context.ExternalResources.Where(r => r.PostId == id);
                    _context.ExternalResources.RemoveRange(resources);

                    // 3. Xóa liên kết trong bảng trung gian PostTags (EF Core tự làm nếu dùng .Include)
                    post.Tags.Clear();

                    // 4. Xóa chính bài viết
                    _context.Posts.Remove(post);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Lỗi xóa bài viết: {ex.Message}");
                }
            });
        }
    }
}