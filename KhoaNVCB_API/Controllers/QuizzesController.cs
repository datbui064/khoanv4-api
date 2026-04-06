using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaNVCB_API.Models;
using ClosedXML.Excel;

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
        // KHU VỰC 1: QUẢN LÝ PHIÊN THI (CHO ADMIN)
        // ==========================================

        [HttpPost("session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req)
        {
            try
            {
                string newCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                var session = new QuizSession
                {
                    SessionName = req.SessionName ?? "Phiên thi mới",
                    CategoryId = req.CategoryId > 0 ? req.CategoryId : null,
                    QuestionCount = req.QuestionCount,
                    TimeLimitMinutes = req.TimeLimitMinutes,
                    SessionCode = newCode,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.QuizSessions.Add(session);
                await _context.SaveChangesAsync();

                // Chỉ trả về dữ liệu phẳng để tránh lỗi JSON
                return Ok(new
                {
                    session.SessionId,
                    session.SessionCode,
                    session.SessionName
                });
            }
            catch (Exception ex)
            {
                // Trả về nội dung lỗi thực sự để bạn dễ debug
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================================
        // TÍNH NĂNG MỚI: ÔN TẬP (PRACTICE MODE)
        // ==========================================

        [HttpGet("practice/{categoryId}/{count}")]
        public async Task<IActionResult> GetPracticeQuestions(int categoryId, int count)
        {
            if (count <= 0) count = 10;

            IQueryable<Question> query = _context.Questions;

            // Nếu categoryId > 0 thì lọc theo chủ đề, nếu = 0 thì lấy ngẫu nhiên toàn bộ ngân hàng
            if (categoryId > 0)
            {
                query = query.Where(q => q.CategoryId == categoryId);
            }

            var questions = await query
                .OrderBy(q => Guid.NewGuid())
                .Take(count)
                .Select(q => new {
                    q.QuestionId,
                    q.Content,
                    q.OptionA,
                    q.OptionB,
                    q.OptionC,
                    q.OptionD,
                    q.CorrectAnswer
                })
                .ToListAsync();

            if (!questions.Any())
                return NotFound("Không tìm thấy câu hỏi nào trong chủ đề này.");

            return Ok(new
            {
                CategoryName = _context.Categories.FirstOrDefault(c => c.CategoryId == categoryId)?.CategoryName,
                TotalRequested = count,
                Questions = questions
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            var sessions = await _context.QuizSessions
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new {
                    s.SessionId,
                    s.SessionName,
                    s.SessionCode,
                    s.QuestionCount,
                    s.TimeLimitMinutes,
                    s.IsActive,
                    s.CreatedDate,
                    s.CategoryId
                })
                .ToListAsync();
            return Ok(sessions);
        }
        [HttpDelete("session/{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.QuizSessions.FindAsync(id);
            if (session == null) return NotFound("Không tìm thấy phiên thi.");

            // XÓA CỨNG: Xóa sạch bách toàn bộ lịch sử nộp bài của phiên này trước
            var relatedAttempts = _context.QuizAttempts.Where(a => a.SessionId == id);
            _context.QuizAttempts.RemoveRange(relatedAttempts);

            // Sau đó mới trảm cái Phiên thi
            _context.QuizSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPut("session/{id}/toggle")]
        public async Task<IActionResult> ToggleSession(int id)
        {
            var session = await _context.QuizSessions.FindAsync(id);
            if (session == null) return NotFound();

            session.IsActive = !session.IsActive; // Đảo trạng thái (Đóng/Mở)
            await _context.SaveChangesAsync();
            return Ok(session);
        }

        [HttpGet("session/{sessionId}/history")]
        public async Task<IActionResult> GetSessionHistory(int sessionId)
        {
            var history = await _context.QuizAttempts
                .Where(a => a.SessionId == sessionId)
                .OrderByDescending(a => a.CorrectAnswers) // Xếp hạng từ cao xuống thấp
                .Select(a => new
                {
                    a.AttemptId,
                    a.FullName,
                    a.StudentIdOrEmail,
                    a.ClassName,
                    a.TotalQuestions,
                    a.CorrectAnswers,
                    a.AttemptDate
                })
                .ToListAsync();
            return Ok(history);
        }

        // ==========================================
        // KHU VỰC 2: TƯƠNG TÁC THI CỬ (CHO SINH VIÊN)
        // ==========================================

        [HttpGet("join/{sessionCode}")]
        public async Task<IActionResult> GetQuizBySessionCode(string sessionCode)
        {
            var session = await _context.QuizSessions.FirstOrDefaultAsync(s => s.SessionCode == sessionCode);
            if (session == null) return NotFound("Mã phiên thi không tồn tại.");

            // Nếu Admin đã đóng thì sinh viên không lấy được đề
            if (session.IsActive == false) return BadRequest("Phiên thi này đã kết thúc.");

            IQueryable<Question> query = _context.Questions;
            if (session.CategoryId.HasValue && session.CategoryId > 0)
            {
                query = query.Where(q => q.CategoryId == session.CategoryId.Value);
            }

            var questions = await query
                .OrderBy(q => Guid.NewGuid()) // Bốc ngẫu nhiên (mỗi người 1 đề khác nhau)
                .Take(session.QuestionCount)
                .Select(q => new {
                    q.QuestionId,
                    q.Content,
                    q.OptionA,
                    q.OptionB,
                    q.OptionC,
                    q.OptionD
                })
                .ToListAsync();

            if (!questions.Any()) return BadRequest("Ngân hàng không đủ câu hỏi.");

            return Ok(new { Session = session, Questions = questions });
        }

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

            // Lưu lịch sử bài làm vào Database (Theo form mới)
            var attempt = new QuizAttempt
            {
                SessionId = request.SessionId,
                CategoryId = request.CategoryId,
                FullName = request.FullName,
                StudentIdOrEmail = request.StudentIdOrEmail,
                ClassName = request.ClassName,
                TotalQuestions = request.Answers.Count,
                CorrectAnswers = score,
                AttemptDate = DateTime.Now
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                TotalQuestions = request.Answers.Count,
                CorrectAnswers = score,
                Message = $"Tuyệt vời! Bạn trả lời đúng {score}/{request.Answers.Count} câu!"
            });
        }

        // ==========================================
        // KHU VỰC 3: QUẢN LÝ NGÂN HÀNG CÂU HỎI (CRUD & EXCEL)
        // ==========================================

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetQuestionsByCategory(int categoryId)
        {
            var questions = await _context.Questions
                .Where(q => q.CategoryId == categoryId)
                .OrderByDescending(q => q.QuestionId)
                .ToListAsync();
            return Ok(questions);
        }

        [HttpPut("question/{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] Question updatedQuestion)
        {
            if (id != updatedQuestion.QuestionId) return BadRequest("ID không hợp lệ.");

            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound("Không tìm thấy câu hỏi.");

            question.Content = updatedQuestion.Content;
            question.OptionA = updatedQuestion.OptionA;
            question.OptionB = updatedQuestion.OptionB;
            question.OptionC = updatedQuestion.OptionC;
            question.OptionD = updatedQuestion.OptionD;
            question.CorrectAnswer = updatedQuestion.CorrectAnswer.ToUpper();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("question/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("MauCauHoi");
                worksheet.Cell(1, 1).Value = "Nội dung câu hỏi";
                worksheet.Cell(1, 2).Value = "Đáp án A";
                worksheet.Cell(1, 3).Value = "Đáp án B";
                worksheet.Cell(1, 4).Value = "Đáp án C";
                worksheet.Cell(1, 5).Value = "Đáp án D";
                worksheet.Cell(1, 6).Value = "Đáp án đúng (Chỉ nhập A, B, C hoặc D)";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_NhapCauHoi.xlsx");
                }
            }
        }

        //xóa nhiều câu hỏi
        [HttpPost("/api/Quizzes/questions/bulk-delete")] // Thêm dấu gạch chéo ở đầu để ép route tuyệt đối
        public async Task<IActionResult> BulkDeleteQuestions([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID trống.");

            var questionsToDelete = await _context.Questions.Where(q => ids.Contains(q.QuestionId)).ToListAsync();

            if (!questionsToDelete.Any()) return NotFound("Không tìm thấy câu hỏi nào để xóa.");

            _context.Questions.RemoveRange(questionsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa thành công {questionsToDelete.Count} câu hỏi." });
        }

        [HttpPost("import/{categoryId}")]
        public async Task<IActionResult> ImportQuestionsFromExcel(int categoryId, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng tải lên một file Excel.");
            if (!file.FileName.EndsWith(".xlsx")) return BadRequest("Chỉ hỗ trợ định dạng .xlsx");

            var questionsToAdd = new List<Question>();
            int rowIndex = 1;

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            rowIndex++;
                            var content = row.Cell(1).GetString().Trim();
                            if (string.IsNullOrEmpty(content)) continue;

                            var optA = row.Cell(2).GetString().Trim();
                            var optB = row.Cell(3).GetString().Trim();
                            var optC = row.Cell(4).GetString().Trim();
                            var optD = row.Cell(5).GetString().Trim();
                            var correct = row.Cell(6).GetString().Trim().ToUpper();

                            if (correct != "A" && correct != "B" && correct != "C" && correct != "D")
                                return BadRequest($"Lỗi ở dòng số {rowIndex}: 'Đáp án đúng' phải là A, B, C hoặc D. (Đang nhập: {correct})");

                            if (string.IsNullOrEmpty(optA) || string.IsNullOrEmpty(optB) || string.IsNullOrEmpty(optC) || string.IsNullOrEmpty(optD))
                                return BadRequest($"Lỗi ở dòng số {rowIndex}: Không được để trống bất kỳ đáp án nào.");

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

    // ==========================================
    // KHU VỰC 4: DTOs TRUYỀN TẢI DỮ LIỆU
    // ==========================================
    public class CreateSessionRequest
    {
        public string SessionName { get; set; } = null!;
        public int CategoryId { get; set; }
        public int QuestionCount { get; set; }
        public int TimeLimitMinutes { get; set; }
    }

    public class SubmitQuizRequest
    {
        public int SessionId { get; set; }
        public int CategoryId { get; set; }
        public string FullName { get; set; } = null!;
        public string StudentIdOrEmail { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public Dictionary<int, string> Answers { get; set; } = new();
    }
}