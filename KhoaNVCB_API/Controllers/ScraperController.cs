using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KhoaNVCB_API.Services;
using KhoaNVCB_API.Models; // Thêm dòng này để dùng ScraperService

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScraperController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;
        private readonly ScraperService _scraperService;

        public ScraperController(KhoaNvcbBlogDbContext context, ScraperService scraperService)
        {
            _context = context;
            _scraperService = scraperService;
        }

        // ==========================================
        // 1. API RA LỆNH CÀO TIN TỪ VNEXPRESS
        // ==========================================
        [HttpPost("run-vnexpress")]
        public async Task<IActionResult> RunVnExpressScraper()
        {
            int count = await _scraperService.ScrapeVnExpressPhapLuat();
            return Ok(new { Message = $"Đã cào thành công {count} bài viết mới chờ duyệt." });
        }
        // ==========================================
        // 3. API RA LỆNH CÀO TIN TỪ BÁO CAND
        // ==========================================
        [HttpPost("run-cand")]
        public async Task<IActionResult> RunCandScraper()
        {
            // Cào RSS mảng Chống diễn biến hòa bình của CAND
            string rssUrl = "https://cand.com.vn/rss/Chong-dien-bien-hoa-binh/";
            int count = await _scraperService.ScrapeBaoCAND(rssUrl);
            return Ok(new { Message = $"Đã cào thành công {count} bài viết mới từ Báo CAND." });
        }
        // ==========================================
        // 4. API BÁO TUỔI TRẺ
        // ==========================================
        [HttpPost("run-tuoitre")]
        public async Task<IActionResult> RunTuoiTreScraper()
        {
            string rssUrl = "https://tuoitre.vn/rss/phap-luat.rss";
            // Tọa độ bài viết Tuổi Trẻ thường nằm ở class 'detail-c'
            string xpath = "//div[contains(@class, 'detail-c')]";

            int count = await _scraperService.ScrapeGenericNews(rssUrl, xpath);
            return Ok(new { Message = $"Đã cào thành công {count} bài viết mới từ Báo Tuổi Trẻ." });
        }

        // ==========================================
        // 5. API BÁO THANH NIÊN
        // ==========================================
        [HttpPost("run-thanhnien")]
        public async Task<IActionResult> RunThanhNienScraper()
        {
            string rssUrl = "https://thanhnien.vn/rss/phap-luat.rss";
            // Tọa độ bài viết Thanh Niên thường nằm ở class 'detail-content'
            string xpath = "//div[contains(@class, 'detail-content') or contains(@class, 'detail__cmain')]";

            int count = await _scraperService.ScrapeGenericNews(rssUrl, xpath);
            return Ok(new { Message = $"Đã cào thành công {count} bài viết mới từ Báo Thanh Niên." });
        }
        // ==========================================
        // 6. API CÀO VIDEO YOUTUBE
        // ==========================================
        [HttpPost("run-youtube")]
        public async Task<IActionResult> RunYouTubeScraper()
        {
            var result = await _scraperService.ScrapeTargetedYouTubeVideos();
            int count = result.Item1;
            string log = result.Item2;

            if (!string.IsNullOrEmpty(log))
            {
                // Nếu có lỗi ngầm, nó sẽ khai ra hết ở đây
                return Ok(new { Message = $"Cào được {count} video. Chi tiết lỗi ngầm: {log}" });
            }

            return Ok(new { Message = $"Tuyệt vời! Đã quét 5 kênh và gắp thành công {count} Video về Kho." });
        }
        // ==========================================
        // 2. API LẤY DANH SÁCH BÀI "CHỜ DUYỆT"
        // ==========================================
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPosts()
        {
            var posts = await _context.Posts
                .Where(p => p.Status == "Pending")
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new {
                    p.PostId,
                    p.Title,
                    p.Summary,
                    p.ImageUrl,
                    p.OriginalUrl,
                    p.CreatedDate
                })
                .ToListAsync();

            return Ok(posts);
        }
    }
}