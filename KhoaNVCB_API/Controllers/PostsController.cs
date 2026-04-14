using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Math;

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
                    YearType = p.YearType,
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
                    Data = p.Data,
                    YearType = p.YearType,
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
                YearType = createDto.YearType, // Thêm dòng này
                Data = createDto.Data,
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
                Data = post.Data,
                YearType = createDto.YearType, // Thêm dòng này
                CreatedDate = post.CreatedDate
            };

            return CreatedAtAction(nameof(GetPost), new { id = post.PostId }, postDto);
        }


        [HttpPost("extract-word")]
        [Authorize(Roles = "Admin")]
        public IActionResult ExtractFileText(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File không tồn tại.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".docx" && extension != ".pdf")
                return BadRequest("Chỉ hỗ trợ định dạng .docx và .pdf");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // 1. NẾU LÀ FILE WORD (.docx)
                    if (extension == ".docx")
                    {
                        var converter = new Mammoth.DocumentConverter();
                        var result = converter.ConvertToHtml(stream);
                        // Mammoth tự động tạo các thẻ <p>, <strong>,... rất chuẩn
                        return Ok(new { text = result.Value });
                    }
                    // 2. NẾU LÀ FILE PDF (.pdf)
                    else
                    {
                        using (var document = UglyToad.PdfPig.PdfDocument.Open(stream))
                        {
                            var textBuilder = new System.Text.StringBuilder();
                            foreach (var page in document.GetPages())
                            {
                                // DÙNG THUẬT TOÁN ĐỌC TỌA ĐỘ ĐỂ GIỮ NGUYÊN DẤU CÁCH
                                var pageText = ContentOrderTextExtractor.GetText(page);

                                // Tách các đoạn văn bản (xuống dòng) và bọc vào thẻ <p> cho TinyMCE hiểu
                                var paragraphs = pageText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in paragraphs)
                                {
                                    // Loại bỏ những đoạn rác chỉ có khoảng trắng
                                    if (!string.IsNullOrWhiteSpace(p))
                                    {
                                        textBuilder.Append($"<p>{p}</p>");
                                    }
                                }
                            }
                            return Ok(new { text = textBuilder.ToString() });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi đọc file: {ex.Message}");
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
            post.YearType = updateDto.YearType;
            post.Content = updateDto.Content;
            post.ImageUrl = updateDto.ImageUrl;
            post.Data = updateDto.Data;

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
        [HttpGet("recent/{count}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetRecentPosts(int count)
        {
            // Đảm bảo count hợp lệ (ví dụ tối đa 20 bài)
            if (count <= 0) count = 5;
            if (count > 20) count = 20;

            return await _context.Posts
                .AsNoTracking()
                .Where(p => p.Status == "Published")
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    // Đảm bảo ImageUrl khớp với dữ liệu ảnh đại diện bài viết
                    ImageUrl = p.ImageUrl ?? p.OriginalUrl,
                    CreatedDate = p.CreatedDate,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Tin tức" // Thêm nếu cần hiển thị tag
                })
                .ToListAsync();
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

        [HttpGet("admin-paged")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<PostListItemDto>>> GetAdminPagedPosts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,

    [FromQuery] string? searchTerm = null,
    [FromQuery] string? categoryName = null,
    [FromQuery] string? soucreType =null,

    [FromQuery] string sortBy = "newest",

    [FromQuery] string? status = null) // MỚI THÊM
        {
            var query = _context.Posts.Include(p => p.Category).AsNoTracking().AsQueryable();

            // Lọc bỏ Tuyên truyền
            query = query.Where(p => p.Category != null && p.Category.CategoryName != "Tuyên truyền");

            // Lọc theo TRẠNG THÁI (MỚI THÊM)
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // Tìm kiếm từ khóa
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm));
            }

            // Lọc chuyên mục
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(p => p.Category != null && p.Category.CategoryName == categoryName);
            }

            // Sắp xếp
            if (sortBy == "oldest") query = query.OrderBy(p => p.CreatedDate);
            else if (sortBy == "title-az") query = query.OrderBy(p => p.Title);
            else query = query.OrderByDescending(p => p.CreatedDate); // newest

            // Phân trang
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var posts = await query
         .Skip((page - 1) * pageSize)
         .Take(pageSize)
         .Select(p => new PostListItemDto
         {
             PostId = p.PostId,
             Title = p.Title,
             Summary = p.Summary,
             CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
             SourceType = p.SourceType, // THÊM DÒNG NÀY
             ImageUrl = p.ImageUrl,
             Status = p.Status,

             CreatedDate = p.CreatedDate
         }).ToListAsync();

            return Ok(new PagedResultDto<PostListItemDto>
            {
                Items = posts,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages > 0 ? totalPages : 1
            });
        }

        [HttpGet("paged")]
        [AllowAnonymous] // MỞ CỬA CHO TẤT CẢ MỌI NGƯỜI
        public async Task<ActionResult<PagedResultDto<PostListItemDto>>> GetPublicPagedPosts(
        [FromQuery] int page = 1,
    [FromQuery] int pageSize = 5,
    [FromQuery] string? searchTerm = null,
    [FromQuery] string? categoryName = null,
    [FromQuery] string? sourceType = null,
    [FromQuery] int? yearType = null,// THÊM DÒNG NÀY
    [FromQuery] string sortBy = "newest")

        {
            var query = _context.Posts.Include(p => p.Category).AsNoTracking().AsQueryable();

            query = query.Where(p => p.Status == "Published" &&
                                    (p.Category == null || p.Category.CategoryName != "Tuyên truyền"));

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm));
            }
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                // Tìm những bài viết có tên Category khớp với tham số truyền vào
                query = query.Where(p => p.Category != null && p.Category.CategoryName == categoryName);
            }
            // Lọc chuyên mục
            if (!string.IsNullOrWhiteSpace(sourceType))
            {
                // 1. Nếu là Document: Lấy cả Document và Law (Gộp)
                if (sourceType == "Document")
                {
                    query = query.Where(p => p.SourceType == "Document" || p.SourceType == "Law");
                }
                // 2. Nếu là Law: CHỈ lấy Law (Tách riêng)
                else if (sourceType == "Law")
                {
                    query = query.Where(p => p.SourceType == "Law");
                }
                // 3. Các loại khác (Video, Article...): Lấy chính xác loại đó
                
                else if (sourceType == "Video")
                {
                    query = query.Where(p => p.SourceType == "Video");
                }
                else if (sourceType == "Article")
                {
                    query = query.Where(p => p.SourceType == "Article");
                }
            }
            if (yearType.HasValue)
            {
                query = query.Where(p => p.YearType == yearType.Value);
            }
            // Sắp xếp
            if (sortBy == "title-az") query = query.OrderBy(p => p.Title);
            else query = query.OrderByDescending(p => p.CreatedDate);

            // Phân trang
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var posts = await query
     .Skip((page - 1) * pageSize)
     .Take(pageSize)
     .Select(p => new PostListItemDto
     {
         PostId = p.PostId,
         Title = p.Title,
         Summary = p.Summary,
         CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
         SourceType = p.SourceType,
         ImageUrl = p.ImageUrl,
         Status = p.Status,
         YearType = p.YearType, // PHẢI THÊM DÒNG NÀY
         CreatedDate = p.CreatedDate
     }).ToListAsync();

            return Ok(new PagedResultDto<PostListItemDto>
            {
                Items = posts,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
    }
}

