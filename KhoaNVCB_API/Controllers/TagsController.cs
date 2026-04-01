using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public TagsController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags()
        {
            var tags = await _context.Tags
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName
                })
                .ToListAsync();

            return Ok(tags);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TagDto>> PostTag(TagDto tagDto)
        {
            var tag = new Tag
            {
                TagName = tagDto.TagName
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            tagDto.TagId = tag.TagId;

            return CreatedAtAction(nameof(GetTags), new { id = tag.TagId }, tagDto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}