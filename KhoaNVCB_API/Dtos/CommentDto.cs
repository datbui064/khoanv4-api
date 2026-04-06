using System;

namespace KhoaNVCB_API.Dtos
{
    public class CommentDto
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        // BỎ DÒNG NÀY: public int AccountId { get; set; }
        public string FullName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime? CreatedDate { get; set; }
    }
}