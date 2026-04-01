using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? PostId { get; set; }

    public int? AccountId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public bool? IsHidden { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Post? Post { get; set; }
}
