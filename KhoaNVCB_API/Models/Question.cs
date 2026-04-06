using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int CategoryId { get; set; }

    public string Content { get; set; } = null!;

    public string OptionA { get; set; } = null!;

    public string OptionB { get; set; } = null!;

    public string OptionC { get; set; } = null!;

    public string OptionD { get; set; } = null!;

    public string CorrectAnswer { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}