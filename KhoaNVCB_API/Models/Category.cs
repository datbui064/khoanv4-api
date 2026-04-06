using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Slug { get; set; }

    public int? ParentId { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual Category? Parent { get; set; }

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<QuizSetting> QuizSettings { get; set; } = new List<QuizSetting>();
}