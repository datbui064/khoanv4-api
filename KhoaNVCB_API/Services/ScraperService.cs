using HtmlAgilityPack;
using KhoaNVCB_API.Models;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace KhoaNVCB_API.Services
{
    public class ScraperService
    {
        private readonly KhoaNvcbBlogDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly GeminiService _geminiService;

        public ScraperService(KhoaNvcbBlogDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        // ==========================================
        // 1. MODULE CÀO TIN VNEXPRESS
        // ==========================================
        public async Task<int> ScrapeVnExpressPhapLuat()
        {
            int addedCount = 0;
            string rssUrl = "https://vnexpress.net/rss/phap-luat.rss";

            try
            {
                var rssContent = await _httpClient.GetStringAsync(rssUrl);
                var xmlDoc = XDocument.Parse(rssContent);
                var items = xmlDoc.Descendants("item").Take(15);

                foreach (var item in items)
                {
                    string title = item.Element("title")?.Value ?? "";
                    string link = item.Element("link")?.Value ?? "";
                    string descriptionHtml = item.Element("description")?.Value ?? "";

                    if (_context.Posts.Any(p => p.OriginalUrl == link)) continue;

                    string imageUrl = ExtractImageUrl(descriptionHtml);
                    string summary = ExtractTextFromHtml(descriptionHtml);

                    // TRIỆU HỒI AI KIỂM DUYỆT
                    bool isRelevant = await _geminiService.IsRelevantToSecurityAsync(title, summary);
                    await Task.Delay(4000); // Nghỉ 4s chống spam API

                    if (!isRelevant)
                    {
                        Console.WriteLine($"[Gemini API - VnExpress] Vứt bỏ tin rác: {title}");
                        continue;
                    }

                    string contentHtml = await ExtractArticleContent(link);
                    if (string.IsNullOrEmpty(contentHtml)) continue;

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        var match = Regex.Match(contentHtml, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase);
                        if (match.Success) imageUrl = match.Groups[1].Value;
                    }

                    var newPost = new Post
                    {
                        Title = title,
                        Summary = summary,
                        Content = contentHtml,
                        ImageUrl = imageUrl,
                        OriginalUrl = link,
                        SourceType = "Image",
                        Status = "Pending",
                        CategoryId = null,
                        CreatedDate = DateTime.Now
                    };

                    _context.Posts.Add(newPost);
                    addedCount++;
                }

                await _context.SaveChangesAsync();
                return addedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi cào VnExpress: {ex.Message}");
                return addedCount;
            }
        }

        // ==========================================
        // 2. MODULE CÀO TIN BÁO C.A.N.D
        // ==========================================
        public async Task<int> ScrapeBaoCAND(string rssUrl)
        {
            int addedCount = 0;
            try
            {
                var rssContent = await _httpClient.GetStringAsync(rssUrl);
                var xmlDoc = XDocument.Parse(rssContent);
                var items = xmlDoc.Descendants("item").Take(15);

                foreach (var item in items)
                {
                    string title = item.Element("title")?.Value ?? "";
                    string link = item.Element("link")?.Value ?? "";
                    string descriptionHtml = item.Element("description")?.Value ?? "";

                    if (_context.Posts.Any(p => p.OriginalUrl == link)) continue;

                    string imageUrl = ExtractImageUrl(descriptionHtml);
                    string summary = ExtractTextFromHtml(descriptionHtml);

                    // TRIỆU HỒI AI KIỂM DUYỆT
                    bool isRelevant = await _geminiService.IsRelevantToSecurityAsync(title, summary);
                    await Task.Delay(4000);

                    if (!isRelevant)
                    {
                        Console.WriteLine($"[Gemini API - CAND] Vứt bỏ tin rác: {title}");
                        continue;
                    }

                    string contentHtml = await ExtractCANDContent(link);
                    if (string.IsNullOrEmpty(contentHtml)) continue;

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        var match = Regex.Match(contentHtml, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase);
                        if (match.Success) imageUrl = match.Groups[1].Value;
                    }

                    var newPost = new Post
                    {
                        Title = title,
                        Summary = summary,
                        Content = contentHtml,
                        ImageUrl = imageUrl,
                        OriginalUrl = link,
                        SourceType = "Image",
                        Status = "Pending",
                        CategoryId = null,
                        CreatedDate = DateTime.Now
                    };

                    _context.Posts.Add(newPost);
                    addedCount++;
                }

                await _context.SaveChangesAsync();
                return addedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi cào CAND: {ex.Message}");
                return addedCount;
            }
        }

        // ==========================================
        // 3. CỖ MÁY CÀO TIN VẠN NĂNG (TUỔI TRẺ / THANH NIÊN)
        // ==========================================
        public async Task<int> ScrapeGenericNews(string rssUrl, string contentXPath)
        {
            int addedCount = 0;
            try
            {
                var rssContent = await _httpClient.GetStringAsync(rssUrl);
                var xmlDoc = XDocument.Parse(rssContent);
                var items = xmlDoc.Descendants("item").Take(15);

                foreach (var item in items)
                {
                    string title = item.Element("title")?.Value ?? "";
                    string link = item.Element("link")?.Value ?? "";
                    string descriptionHtml = item.Element("description")?.Value ?? "";

                    if (_context.Posts.Any(p => p.OriginalUrl == link)) continue;

                    string imageUrl = ExtractImageUrl(descriptionHtml);
                    string summary = ExtractTextFromHtml(descriptionHtml);

                    // TRIỆU HỒI AI KIỂM DUYỆT
                    bool isRelevant = await _geminiService.IsRelevantToSecurityAsync(title, summary);
                    await Task.Delay(4000);

                    if (!isRelevant)
                    {
                        Console.WriteLine($"[Gemini API - Báo Đa Năng] Vứt bỏ tin rác: {title}");
                        continue;
                    }

                    string contentHtml = await ExtractDynamicContent(link, contentXPath);
                    if (string.IsNullOrEmpty(contentHtml)) continue;

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        var match = Regex.Match(contentHtml, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase);
                        if (match.Success) imageUrl = match.Groups[1].Value;
                    }

                    var newPost = new Post
                    {
                        Title = title,
                        Summary = summary,
                        Content = contentHtml,
                        ImageUrl = imageUrl,
                        OriginalUrl = link,
                        SourceType = "Image",
                        Status = "Pending",
                        CategoryId = null,
                        CreatedDate = DateTime.Now
                    };

                    _context.Posts.Add(newPost);
                    addedCount++;
                }

                await _context.SaveChangesAsync();
                return addedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi cào dữ liệu từ {rssUrl}: {ex.Message}");
                return addedCount;
            }
        }

        // ==========================================
        // 4. MODULE CÀO VIDEO YOUTUBE (XUYÊN GIÁP XML)
        // ==========================================
        public async Task<(int, string)> ScrapeTargetedYouTubeVideos()
        {
            int addedCount = 0;
            string debugLog = "";

            // DANH SÁCH NÀY CỤC CỨT NHỚ THAY BẰNG ID THẬT NHÉ (UC...)
            // DANH SÁCH ID CHUẨN ĐÉT KHÔNG BAO GIỜ LỖI 404
            var targetChannels = new List<string>
            {
                "UCIg56SgvoZF8Qg0Jx_gh6Pg", // ANTV
                "UCabsTV34JwALXKGMqHpvUiA", // VTV24
                "UCPJfjHrW3-zIeSaZTgmckmg", // Báo Nhân Dân
                "UCmBT5CqUxf3-K5_IU9tVtBg", // VNA MEDIA
                "UCYUxsH8xyKAQx1WpCcex5LA"  // Thông tin Chính phủ
            };

            foreach (var channelId in targetChannels)
            {
                try
                {
                    string rssUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    var rssContent = await _httpClient.GetStringAsync(rssUrl);
                    var xmlDoc = XDocument.Parse(rssContent);

                    var entries = xmlDoc.Descendants().Where(e => e.Name.LocalName == "entry").Take(15);

                    if (!entries.Any()) debugLog += $"[Kênh {channelId}: Không tìm thấy video!] ";

                    foreach (var entry in entries)
                    {
                        string title = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value ?? "";
                        string link = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "link")?.Attribute("href")?.Value ?? "";

                        if (_context.Posts.Any(p => p.OriginalUrl == link)) continue;

                        var mediaGroup = entry.Elements().FirstOrDefault(e => e.Name.LocalName == "group");
                        string description = mediaGroup?.Elements().FirstOrDefault(e => e.Name.LocalName == "description")?.Value ?? title;
                        string imageUrl = mediaGroup?.Elements().FirstOrDefault(e => e.Name.LocalName == "thumbnail")?.Attribute("url")?.Value ?? "";

                        // TRIỆU HỒI AI KIỂM DUYỆT
                        bool isRelevant = await _geminiService.IsRelevantToSecurityAsync(title, rssUrl);
                        await Task.Delay(4000); // Nghỉ 4s

                        if (!isRelevant)
                        {
                            debugLog += $"[Vứt rác: {title}] ";
                            Console.WriteLine($"[Gemini API - YouTube] Vứt bỏ Video rác: {title}");
                            continue;
                        }

                        var newPost = new Post
                        {
                            Title = title,
                            Summary = description.Length > 200 ? description.Substring(0, 200) + "..." : description,
                            Content = $"<p>{description}</p>",
                            ImageUrl = imageUrl,
                            OriginalUrl = link,
                            SourceType = "Video",
                            Status = "Pending",
                            CategoryId = null,
                            CreatedDate = DateTime.Now
                        };

                        _context.Posts.Add(newPost);
                        addedCount++;
                    }
                }
                catch (Exception ex)
                {
                    debugLog += $"[Lỗi kênh {channelId}: {ex.Message}] ";
                }
            }

            await _context.SaveChangesAsync();
            return (addedCount, debugLog);
        }

        // ==========================================
        // CÁC HÀM PHỤ TRỢ XỬ LÝ HTML (Giữ nguyên không đổi)
        // ==========================================
        private async Task<string> ExtractArticleContent(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var articleNode = htmlDoc.DocumentNode.SelectSingleNode("//article[contains(@class, 'fck_detail')]");

                if (articleNode != null)
                {
                    var adsNodes = articleNode.SelectNodes("//div[contains(@class, 'box_category')]");
                    if (adsNodes != null) foreach (var node in adsNodes) node.Remove();

                    var pictures = articleNode.SelectNodes("//picture");
                    if (pictures != null)
                    {
                        foreach (var pic in pictures)
                        {
                            var img = pic.SelectSingleNode(".//img");
                            if (img != null)
                            {
                                var realSrc = img.GetAttributeValue("data-src", img.GetAttributeValue("src", ""));
                                if (!string.IsNullOrEmpty(realSrc))
                                {
                                    var cleanImg = HtmlNode.CreateNode($"<img src='{realSrc}' style='max-width:100%; border-radius:8px; margin-bottom:8px;' />");
                                    pic.ParentNode.ReplaceChild(cleanImg, pic);
                                }
                            }
                        }
                    }

                    var standaloneImgs = articleNode.SelectNodes("//img");
                    if (standaloneImgs != null)
                    {
                        foreach (var img in standaloneImgs)
                        {
                            var dataSrc = img.GetAttributeValue("data-src", "");
                            if (!string.IsNullOrEmpty(dataSrc))
                            {
                                img.SetAttributeValue("src", dataSrc);
                                img.Attributes.Remove("data-src");
                                img.Attributes.Remove("loading");
                                img.SetAttributeValue("style", "max-width:100%; height:auto; border-radius:8px; margin-bottom:8px;");
                            }
                        }
                    }

                    return articleNode.InnerHtml;
                }
                return "";
            }
            catch { return ""; }
        }

        private async Task<string> ExtractCANDContent(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var articleNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'detail-content')]");

                if (articleNode != null)
                {
                    var imgs = articleNode.SelectNodes("//img");
                    if (imgs != null)
                    {
                        foreach (var img in imgs)
                        {
                            var realSrc = img.GetAttributeValue("data-src", img.GetAttributeValue("src", ""));
                            if (!string.IsNullOrEmpty(realSrc))
                            {
                                img.SetAttributeValue("src", realSrc);
                                img.SetAttributeValue("style", "max-width:100%; height:auto; border-radius:8px; margin-bottom:8px;");
                            }
                        }
                    }
                    return articleNode.InnerHtml;
                }
                return "";
            }
            catch { return ""; }
        }

        private async Task<string> ExtractDynamicContent(string url, string xpath)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var articleNode = htmlDoc.DocumentNode.SelectSingleNode(xpath);

                if (articleNode != null)
                {
                    var adsNodes = articleNode.SelectNodes("//div[contains(@class, 'box_category') or contains(@class, 'banner')]");
                    if (adsNodes != null) foreach (var node in adsNodes) node.Remove();

                    var pictures = articleNode.SelectNodes("//picture");
                    if (pictures != null)
                    {
                        foreach (var pic in pictures)
                        {
                            var img = pic.SelectSingleNode(".//img");
                            if (img != null)
                            {
                                var realSrc = img.GetAttributeValue("data-src", img.GetAttributeValue("src", ""));
                                if (!string.IsNullOrEmpty(realSrc))
                                {
                                    var cleanImg = HtmlNode.CreateNode($"<img src='{realSrc}' style='max-width:100%; border-radius:8px; margin-bottom:8px;' />");
                                    pic.ParentNode.ReplaceChild(cleanImg, pic);
                                }
                            }
                        }
                    }

                    var standaloneImgs = articleNode.SelectNodes("//img");
                    if (standaloneImgs != null)
                    {
                        foreach (var img in standaloneImgs)
                        {
                            var dataSrc = img.GetAttributeValue("data-src", "");
                            if (!string.IsNullOrEmpty(dataSrc))
                            {
                                img.SetAttributeValue("src", dataSrc);
                                img.Attributes.Remove("data-src");
                                img.Attributes.Remove("loading");
                            }
                            img.SetAttributeValue("style", "max-width:100%; height:auto; border-radius:8px; margin-bottom:8px;");
                        }
                    }

                    return articleNode.InnerHtml;
                }
                return "";
            }
            catch { return ""; }
        }

        private string ExtractImageUrl(string html)
        {
            var match = Regex.Match(html, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private string ExtractTextFromHtml(string html)
        {
            var text = Regex.Replace(html, "<.*?>", string.Empty);
            return text.Trim();
        }
    }
}