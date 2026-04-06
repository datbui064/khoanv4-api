using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public CategoriesController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Slug = c.Slug,
                    ParentId = c.ParentId,
                    ImageUrl = c.ImageUrl // Ánh xạ ảnh khi hiển thị danh sách
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Slug = c.Slug,
                    ParentId = c.ParentId,
                    ImageUrl = c.ImageUrl // Ánh xạ ảnh khi xem chi tiết
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryDto>> PostCategory(CategoryDto categoryDto)
        {
            var category = new Category
            {
                CategoryName = categoryDto.CategoryName,
                Slug = categoryDto.Slug,
                ParentId = categoryDto.ParentId,
                ImageUrl = categoryDto.ImageUrl // LƯU VÀO DB: Bắt link ảnh khi tạo mới
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Trả lại ID vừa được tự động sinh ra
            categoryDto.CategoryId = category.CategoryId;

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, categoryDto);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCategory(int id, CategoryDto categoryDto)
        {
            if (id != categoryDto.CategoryId)
            {
                return BadRequest("Dữ liệu ID không khớp.");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy chuyên mục.");
            }

            // Cập nhật toàn bộ các trường (Bao gồm cả ảnh)
            category.CategoryName = categoryDto.CategoryName;
            category.Slug = categoryDto.Slug;
            category.ParentId = categoryDto.ParentId;
            category.ImageUrl = categoryDto.ImageUrl; // CẬP NHẬT: Ghi đè link ảnh mới

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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

        private bool CategoryExists(int id)
        {
            throw new NotImplementedException();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null) return NotFound("Không tìm thấy chuyên mục.");

                    // Tìm danh sách ID chuyên mục con
                    var childIds = await _context.Categories
                        .Where(c => c.ParentId == id)
                        .Select(c => c.CategoryId)
                        .ToListAsync();

                    // Tổng hợp tất cả ID cần xóa (Cha + Con)
                    var allIds = new List<int> { id };
                    allIds.AddRange(childIds);

                    // 1. Xóa Questions (Dùng Try-Catch riêng để tránh lỗi nếu bảng chưa có)
                    try
                    {
                        var questions = await _context.Questions.Where(q => allIds.Contains(q.CategoryId)).ToListAsync();
                        if (questions.Any()) _context.Questions.RemoveRange(questions);
                    }
                    catch { /* Bỏ qua nếu bảng Questions chưa tồn tại */ }

                    // 2. Xóa Posts và Comments
                    var posts = await _context.Posts.Where(p => p.CategoryId.HasValue && allIds.Contains(p.CategoryId.Value)).ToListAsync();
                    var postIds = posts.Select(p => p.PostId).ToList();

                    var comments = await _context.Comments.Where(c => c.PostId.HasValue && postIds.Contains(c.PostId.Value)).ToListAsync();

                    if (comments.Any()) _context.Comments.RemoveRange(comments);
                    if (posts.Any()) _context.Posts.RemoveRange(posts);

                    // 3. Xóa chuyên mục con
                    var childCats = await _context.Categories.Where(c => childIds.Contains(c.CategoryId)).ToListAsync();
                    if (childCats.Any()) _context.Categories.RemoveRange(childCats);

                    // 4. Xóa chuyên mục cha
                    _context.Categories.Remove(category);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Đây là nơi trả về lỗi 400 kèm nội dung lỗi
                    return BadRequest($"Lỗi: {ex.Message} - {ex.InnerException?.Message}");
                }
            });
        }
    }
}