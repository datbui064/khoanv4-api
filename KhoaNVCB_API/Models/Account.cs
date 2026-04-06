using System;
using System.Collections.Generic;

namespace KhoaNVCB_API.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string Role { get; set; } = null!;

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}