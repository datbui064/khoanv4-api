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
        public async Task<ActionResult<IEnumerable<PostListItemDto>>> GetPosts()
        {
            return await _context.Posts
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new PostListItemDto // Sử dụng DTO rút gọn
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Summary = p.Summary,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                    ImageUrl = p.ImageUrl,
                    Status = p.Status,
                    CreatedDate = p.CreatedDate
                })
                .ToListAsync();
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
        public async Task<ActionResult<PostDto>> PostPost(CreatePostDto createDto)
        {
            var post = new Post
            {
                Title = createDto.Title,
                Slug = createDto.Slug,
                Summary = createDto.Summary,
                Content = createDto.Content,
                CategoryId = createDto.CategoryId,
                OriginalUrl = createDto.OriginalUrl,
                ImageUrl = createDto.ImageUrl, // MỚI THÊM: Lưu ảnh bìa vào CSDL
                SourceType = createDto.SourceType ?? "Manual", // Sửa lại một chút để nó nhận đúng loại Video/Image
                Status = "Published",
                CreatedDate = DateTime.Now
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var postDto = new PostDto
            {
                PostId = post.PostId,
                Title = post.Title,
                Slug = post.Slug,
                Summary = post.Summary,
                Content = post.Content,
                CategoryName = "",
                SourceType = post.SourceType,
                OriginalUrl = post.OriginalUrl,
                ImageUrl = post.ImageUrl, // MỚI THÊM
                Status = post.Status,
                CreatedDate = post.CreatedDate
            };

            return CreatedAtAction(nameof(GetPost), new { id = post.PostId }, postDto);
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
            throw new NotImplementedException();
        }
        [HttpGet("recent/{count}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetRecentPosts(int count)
        {
            return await _context.Posts
                .AsNoTracking() // Tăng tốc độ đọc
                .Where(p => p.Status == "Published")
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    ImageUrl = p.ImageUrl, // Giả sử bạn có trường này
                    CreatedDate = p.CreatedDate
                    // TUYỆT ĐỐI KHÔNG select trường Content ở đây
                })
                .ToListAsync();
        }
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPagedPosts(
    int page = 1,
    int pageSize = 10,
    int? categoryId = null,
    string? sourceType = null) // Thêm lọc theo loại
        {
            var query = _context.Posts.AsNoTracking()
                .Where(p => p.Status == "Published");

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Lọc theo Video hoặc Article ngay tại SQL
            if (!string.IsNullOrEmpty(sourceType))
            {
                if (sourceType == "videos")
                    query = query.Where(p => p.SourceType == "Video");
                else
                    query = query.Where(p => p.SourceType != "Video");
            }

            var totalCount = await query.CountAsync();

            var posts = await query
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Summary = p.Summary,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                    ImageUrl = p.ImageUrl,
                    OriginalUrl = p.OriginalUrl, // Để lấy ảnh nếu ImageUrl trống
                    CreatedDate = p.CreatedDate,
                    SourceType = p.SourceType
                    // TUYỆT ĐỐI KHÔNG lấy Content ở đây để giảm dung lượng tải
                })
                .ToListAsync();

            // Giữ nguyên phần Header Pagination của bạn
            var metadata = new { totalCount, pageSize, currentPage = page };
            Response.Headers.Add("X-Pagination", System.Text.Json.JsonSerializer.Serialize(metadata));
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Pagination");

            return Ok(posts);
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