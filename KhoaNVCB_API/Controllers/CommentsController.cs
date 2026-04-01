using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using System.ComponentModel.DataAnnotations;
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
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByPost(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.Account)
                .Where(c => c.PostId == postId && c.IsHidden == false)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = (int)c.PostId,
                    AccountId = (int)c.AccountId,
                    FullName = c.Account != null ? c.Account.FullName : "Ẩn danh",
                    Content = c.Content,
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentDto>> PostComment(CreateCommentDto createDto)
        {
            var comment = new Comment
            {
                PostId = createDto.PostId,
                AccountId = createDto.AccountId,
                Content = createDto.Content,
                CreatedDate = DateTime.Now,
                IsHidden = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var account = await _context.Accounts.FindAsync(createDto.AccountId);

            var commentDto = new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = (int)comment.PostId,
                AccountId = (int)comment.AccountId,
                FullName = account != null ? account.FullName : "Ẩn danh",
                Content = comment.Content,
                CreatedDate = comment.CreatedDate
            };

            return CreatedAtAction(nameof(GetCommentsByPost), new { postId = comment.PostId }, commentDto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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