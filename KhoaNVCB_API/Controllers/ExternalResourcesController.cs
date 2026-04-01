using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;

using KhoaNVCB_API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalResourcesController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public ExternalResourcesController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<IEnumerable<ExternalResourceDto>>> GetResourcesByPost(int postId)
        {
            var resources = await _context.ExternalResources
                .Where(e => e.PostId == postId)
                .Select(e => new ExternalResourceDto
                {
                    ResourceId = e.ResourceId,
                    PostId = (int)e.PostId,
                    ResourceType = e.ResourceType,
                    SourceUrl = e.SourceUrl
                })
                .ToListAsync();

            return Ok(resources);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExternalResourceDto>> PostResource(ExternalResourceDto resourceDto)
        {
            var resource = new ExternalResource
            {
                PostId = resourceDto.PostId,
                ResourceType = resourceDto.ResourceType,
                SourceUrl = resourceDto.SourceUrl
            };

            _context.ExternalResources.Add(resource);
            await _context.SaveChangesAsync();

            resourceDto.ResourceId = resource.ResourceId;

            return CreatedAtAction(nameof(GetResourcesByPost), new { postId = resource.PostId }, resourceDto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.ExternalResources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            _context.ExternalResources.Remove(resource);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}