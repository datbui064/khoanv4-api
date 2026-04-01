using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Models
{
    public class QuizAttempt
    {
        [Key]
        public int AttemptId { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        
        public int TotalQuestions { get; set; }
        // Navigation properties
        public int CorrectAnswers { get; set; }
        public DateTime AttemptDate { get; set; }

    }
}
