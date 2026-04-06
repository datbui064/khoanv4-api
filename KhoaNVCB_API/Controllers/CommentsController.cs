using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public CommentsController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        [HttpGet("post/{postId}")]
        [AllowAnonymous] // Mở cửa tự do
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByPost(int postId)
        {
            var comments = await _context.Comments
                // Đã xóa cái dòng .Include(c => c.Account) gây lỗi ở đây
                .Where(c => c.PostId == postId && c.IsHidden == false)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = (int)c.PostId,
                    // Lấy thẳng tên người dùng từ bảng Comments, không cần móc nối sang Accounts nữa
                    FullName = c.FullName,
                    Content = c.Content,
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost]
        [AllowAnonymous] // Mở cửa tự do
        public async Task<ActionResult<CommentDto>> PostComment(CreateCommentDto createDto)
        {
            var comment = new Comment
            {
                PostId = createDto.PostId,
                FullName = createDto.FullName,
                Email = createDto.Email,
                Website = createDto.Website,
                Content = createDto.Content,
                CreatedDate = DateTime.Now,
                IsHidden = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var commentDto = new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = (int)comment.PostId,
                FullName = comment.FullName,
                Content = comment.Content,
                CreatedDate = comment.CreatedDate
            };

            return CreatedAtAction(nameof(GetCommentsByPost), new { postId = comment.PostId }, commentDto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Xóa thì vẫn bắt buộc phải là Admin
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}