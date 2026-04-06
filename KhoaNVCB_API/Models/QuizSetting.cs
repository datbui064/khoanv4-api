using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class QuizSetting
{
    public int SettingId { get; set; }

    public int CategoryId { get; set; }

    public int QuestionCount { get; set; }

    public int TimeLimitMinutes { get; set; }

    public virtual Category Category { get; set; } = null!;
}