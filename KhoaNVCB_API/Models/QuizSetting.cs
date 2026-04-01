using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhoaNVCB_API.Models
{
    public class QuizSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int QuestionCount { get; set; } = 20;

        [Required]
        public int TimeLimitMinutes { get; set; } = 15;

        // Nếu có bảng Category, bỏ comment 2 dòng dưới để tạo liên kết (Foreign Key)
         [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}