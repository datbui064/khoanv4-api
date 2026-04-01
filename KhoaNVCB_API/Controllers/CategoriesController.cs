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

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // 1. Tìm chuyên mục gốc (thằng Cha)
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy chuyên mục.");
            }

            // 2. Gom tất cả ID chuyên mục (Cha + Con)
            var childCategories = await _context.Categories.Where(c => c.ParentId == id).ToListAsync();
            var categoryIdsToDelete = childCategories.Select(c => c.CategoryId).ToList();
            categoryIdsToDelete.Add(id); // Thêm ID của thằng Cha vào danh sách

            // 3. Gom tất cả Bài viết thuộc các chuyên mục này
            var postsToDelete = await _context.Posts
                .Where(p => p.CategoryId.HasValue && categoryIdsToDelete.Contains(p.CategoryId.Value))
                .ToListAsync();
            var postIdsToDelete = postsToDelete.Select(p => p.PostId).ToList();

            // 4. Gom tất cả Bình luận thuộc về các Bài viết sắp bị xóa
            var commentsToDelete = await _context.Comments
                .Where(c => postIdsToDelete.Contains((int)c.PostId))
                .ToListAsync();

            // 5. Gom tất cả Câu hỏi trắc nghiệm thuộc các chuyên mục này (Nếu có)
            var questionsToDelete = await _context.Questions
                .Where(q => categoryIdsToDelete.Contains(q.CategoryId))
                .ToListAsync();

            // KÍCH HOẠT GIAO DỊCH (TRANSACTION)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // THÚC ĐẨY QUÁ TRÌNH "NHỔ CỎ TẬN GỐC" (Phải xóa theo thứ tự TỪ DƯỚI LÊN TRÊN)

                // Tầng 1: Xóa râu ria (Bình luận + Câu hỏi)
                if (commentsToDelete.Any()) _context.Comments.RemoveRange(commentsToDelete);
                if (questionsToDelete.Any()) _context.Questions.RemoveRange(questionsToDelete);

                // Tầng 2: Xóa Bài viết
                if (postsToDelete.Any()) _context.Posts.RemoveRange(postsToDelete);

                // Tầng 3: Xóa Chuyên mục con
                if (childCategories.Any()) _context.Categories.RemoveRange(childCategories);

                // Tầng 4: Xóa Chuyên mục cha (Trùm cuối)
                _context.Categories.Remove(category);

                // Chốt sổ
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, hoàn tác mọi thứ như chưa có chuyện gì xảy ra
                await transaction.RollbackAsync();
                return BadRequest($"Hủy diệt thất bại do lỗi CSDL: {ex.Message}");
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}