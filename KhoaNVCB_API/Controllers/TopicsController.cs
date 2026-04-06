using KhoaNVCB_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

[Route("api/[controller]")]
[ApiController]
public class TopicsController : ControllerBase
{
    private readonly KhoaNvcbBlogDbContext _context; // Đảm bảo đây là AppDbContext của bạn

    public TopicsController(KhoaNvcbBlogDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Topic>>> GetTopics()
    {
        return await _context.Topics.ToListAsync();
    }
    [HttpPost]

    public async Task<IActionResult> Create(Topic topic)
    {
        // Nếu dòng dưới đây vẫn lỗi, hãy chắc chắn bạn đã Save file AppDbContext.cs
        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();
        return Ok(topic);
    }
}