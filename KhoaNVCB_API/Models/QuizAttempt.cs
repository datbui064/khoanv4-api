using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class QuizAttempt
{
    public int AttemptId { get; set; }

    public int CategoryId { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public DateTime? AttemptDate { get; set; }

    public int? SessionId { get; set; }

    public string FullName { get; set; } = null!;

    public string StudentIdOrEmail { get; set; } = null!;

    public string ClassName { get; set; } = null!;
}