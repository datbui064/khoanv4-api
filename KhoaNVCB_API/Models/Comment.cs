using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? PostId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public bool? IsHidden { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Website { get; set; }

    public virtual Post? Post { get; set; }
    
   
}