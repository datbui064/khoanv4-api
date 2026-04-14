using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Post
{
    public int PostId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Summary { get; set; }

    public string Content { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? AuthorId { get; set; }

    public int? YearType { get; set; }

    public string? SourceType { get; set; }
    public string? Data { get; set; }

    public string? OriginalUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? PublishedDate { get; set; }

    public string? ImageUrl { get; set; }

    public virtual Account? Author { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<ExternalResource> ExternalResources { get; set; } = new List<ExternalResource>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}