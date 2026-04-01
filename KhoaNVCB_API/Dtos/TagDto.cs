using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Dtos
{
    public class TagDto
    {
        public int TagId { get; set; }
        [Required]
        public string TagName { get; set; } = null!;
    }
}
