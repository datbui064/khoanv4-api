using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhoaNVCB_API.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        public string OptionB { get; set; } = string.Empty;

        [Required]
        public string OptionC { get; set; } = string.Empty;

        [Required]
        public string OptionD { get; set; } = string.Empty;

        [Required]
        [MaxLength(1)] // Chỉ cho phép nhập 1 ký tự (A, B, C, D)
        public string CorrectAnswer { get; set; } = string.Empty;

         [ForeignKey("CategoryId")]
         public Category? Category { get; set; }
    }
}