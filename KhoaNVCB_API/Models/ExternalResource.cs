using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class ExternalResource
{
    public int ResourceId { get; set; }

    public int? PostId { get; set; }

    public string ResourceType { get; set; } = null!;

    public string SourceUrl { get; set; } = null!;

    public virtual Post? Post { get; set; }
}
