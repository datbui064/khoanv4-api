using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class QuizSession
{
    public int SessionId { get; set; }

    public string SessionName { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int QuestionCount { get; set; }

    public int TimeLimitMinutes { get; set; }

    public string SessionCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }
}