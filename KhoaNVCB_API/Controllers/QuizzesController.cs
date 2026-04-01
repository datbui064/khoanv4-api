using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KhoaNVCB_API.Models;
using ClosedXML.Excel; // Thư viện ma thuật xử lý Excel

namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly KhoaNvcbBlogDbContext _context;

        public QuizzesController(KhoaNvcbBlogDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. API BỐC NGẪU NHIÊN CÂU HỎI (GIỮ NGUYÊN)
        // ==========================================
        // ==========================================
        // 1. API BỐC NGẪU NHIÊN CÂU HỎI (CHO SINH VIÊN)
        // ==========================================
        [HttpGet("random/{categoryId}")]
        public async Task<IActionResult> GetRandomQuestions(int categoryId)
        {
            int count = 20; // Mặc định là 20 câu

            IQueryable<Question> query = _context.Questions;

            // Nếu > 0 tức là thi theo chuyên đề. Nếu = 0 là thi Tổng hợp (trộn tất cả)
            if (categoryId > 0)
            {
                var setting = await _context.QuizSettings.FirstOrDefaultAsync(s => s.CategoryId == categoryId);
                if (setting != null) count = setting.QuestionCount;

                query = query.Where(q => q.CategoryId == categoryId); // Lọc theo chuyên đề
            }

            var questions = await query
                .OrderBy(q => Guid.NewGuid()) // Xáo trộn ngẫu nhiên
                .Take(count)
                // Tuyệt đối không Select cột CorrectAnswer để chống hack F12
                .Select(q => new {
                    q.QuestionId,
                    q.Content,
                    q.OptionA,
                    q.OptionB,
                    q.OptionC,
                    q.OptionD
                })
                .ToListAsync();

            if (!questions.Any()) return NotFound("Không có câu hỏi nào trong ngân hàng.");
            return Ok(questions);
        }

        // ==========================================
        // 2. API CHẤM ĐIỂM VÀ LƯU LỊCH SỬ THI
        // ==========================================
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
        {
            int score = 0;
            foreach (var item in request.Answers)
            {
                var question = await _context.Questions.FindAsync(item.Key);
                if (question != null && question.CorrectAnswer.ToUpper() == item.Value.ToUpper())
                    score++;
            }

            // Lưu lịch sử bài làm vào Database
            var attempt = new QuizAttempt
            {
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                TotalQuestions = request.Answers.Count,
                CorrectAnswers = score,
                AttemptDate = DateTime.Now
            };
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(new { TotalQuestions = request.Answers.Count, CorrectAnswers = score, Message = $"Bạn trả lời đúng {score}/{request.Answers.Count} câu!" });
        }

        // ==========================================
        // 8. API LẤY LỊCH SỬ THI (CHO ADMIN)
        // ==========================================
        [HttpGet("history")]
        public async Task<IActionResult> GetQuizHistory()
        {
            // Kết hợp (Join) 3 bảng để lấy Tên Sinh Viên và Tên Chuyên Đề
            var history = await (from a in _context.QuizAttempts
                                 join acc in _context.Accounts on a.AccountId equals acc.AccountId
                                 join cat in _context.Categories on a.CategoryId equals cat.CategoryId into catGroup
                                 from c in catGroup.DefaultIfEmpty()
                                 orderby a.AttemptDate descending
                                 select new
                                 {
                                     a.AttemptId,
                                     FullName = acc.FullName,
                                     CategoryName = c != null ? c.CategoryName : "Bài thi Tổng hợp",
                                     a.TotalQuestions,
                                     a.CorrectAnswers,
                                     a.AttemptDate
                                 }).ToListAsync();
            return Ok(history);
        }
        // ==========================================
        // 5. API LẤY DANH SÁCH CÂU HỎI THEO CHUYÊN MỤC (CHO ADMIN)
        // ==========================================
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetQuestionsByCategory(int categoryId)
        {
            var questions = await _context.Questions
                .Where(q => q.CategoryId == categoryId)
                .OrderByDescending(q => q.QuestionId) // Câu mới import sẽ hiện lên đầu
                .ToListAsync();

            return Ok(questions);
        }
        // ==========================================
        // 6. API SỬA CÂU HỎI
        // ==========================================
        [HttpPut("question/{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] Question updatedQuestion)
        {
            if (id != updatedQuestion.QuestionId) return BadRequest("ID không hợp lệ.");

            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound("Không tìm thấy câu hỏi.");

            // Cập nhật dữ liệu
            question.Content = updatedQuestion.Content;
            question.OptionA = updatedQuestion.OptionA;
            question.OptionB = updatedQuestion.OptionB;
            question.OptionC = updatedQuestion.OptionC;
            question.OptionD = updatedQuestion.OptionD;
            question.CorrectAnswer = updatedQuestion.CorrectAnswer.ToUpper();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==========================================
        // 7. API XÓA CÂU HỎI
        // ==========================================
        [HttpDelete("question/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // ==========================================
        // 3. API TẠO VÀ TẢI FILE EXCEL MẪU (TEMPLATE)
        // ==========================================
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("MauCauHoi");

                // Tạo Header
                worksheet.Cell(1, 1).Value = "Nội dung câu hỏi";
                worksheet.Cell(1, 2).Value = "Đáp án A";
                worksheet.Cell(1, 3).Value = "Đáp án B";
                worksheet.Cell(1, 4).Value = "Đáp án C";
                worksheet.Cell(1, 5).Value = "Đáp án D";
                worksheet.Cell(1, 6).Value = "Đáp án đúng (Chỉ nhập A, B, C hoặc D)";

                // Trang trí Header cho chuyên nghiệp (In đậm, nền xám)
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Columns().AdjustToContents(); // Tự động dãn cột cho vừa chữ

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    // Trả về file Excel tải thẳng xuống máy
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_NhapCauHoi.xlsx");
                }
            }
        }

        // ==========================================
        // 4. API ĐỌC FILE EXCEL ĐỂ LƯU VÀO DATABASE
        // ==========================================
        [HttpPost("import/{categoryId}")]
        public async Task<IActionResult> ImportQuestionsFromExcel(int categoryId, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng tải lên một file Excel.");
            if (!file.FileName.EndsWith(".xlsx")) return BadRequest("Chỉ hỗ trợ định dạng .xlsx");

            var questionsToAdd = new List<Question>();
            int rowIndex = 1; // Biến đếm để báo lỗi chính xác ở dòng nào

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua dòng Header đầu tiên

                        foreach (var row in rows)
                        {
                            rowIndex++;

                            // Lấy dữ liệu và làm sạch (Trim) khoảng trắng thừa
                            var content = row.Cell(1).GetString().Trim();
                            if (string.IsNullOrEmpty(content)) continue; // Bỏ qua nếu dòng này trống nội dung

                            var optA = row.Cell(2).GetString().Trim();
                            var optB = row.Cell(3).GetString().Trim();
                            var optC = row.Cell(4).GetString().Trim();
                            var optD = row.Cell(5).GetString().Trim();
                            var correct = row.Cell(6).GetString().Trim().ToUpper(); // Ép viết hoa

                            // Kiểm tra tính hợp lệ của Đáp án đúng
                            if (correct != "A" && correct != "B" && correct != "C" && correct != "D")
                            {
                                return BadRequest($"Lỗi ở dòng số {rowIndex}: 'Đáp án đúng' phải là A, B, C hoặc D. (Đang nhập: {correct})");
                            }

                            // Kiểm tra xem có thiếu đáp án nào không
                            if (string.IsNullOrEmpty(optA) || string.IsNullOrEmpty(optB) || string.IsNullOrEmpty(optC) || string.IsNullOrEmpty(optD))
                            {
                                return BadRequest($"Lỗi ở dòng số {rowIndex}: Không được để trống bất kỳ đáp án nào.");
                            }

                            questionsToAdd.Add(new Question
                            {
                                CategoryId = categoryId,
                                Content = content,
                                OptionA = optA,
                                OptionB = optB,
                                OptionC = optC,
                                OptionD = optD,
                                CorrectAnswer = correct
                            });
                        }
                    }
                }

                if (questionsToAdd.Any())
                {
                    await _context.Questions.AddRangeAsync(questionsToAdd);
                    await _context.SaveChangesAsync();
                    return Ok(new { Message = $"Tuyệt vời! Đã nhập thành công {questionsToAdd.Count} câu hỏi vào ngân hàng." });
                }

                return BadRequest("Không tìm thấy dữ liệu hợp lệ trong file Excel.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống khi đọc file Excel: {ex.Message}");
            }
        }
    }
    public class SubmitQuizRequest
    {
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public Dictionary<int, string> Answers { get; set; } = new();
    }
}