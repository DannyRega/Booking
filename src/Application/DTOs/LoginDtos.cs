using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    /// <summary>
    /// Represents a request to log in a user with their email and password.
    /// </summary>
    /// <param name="Email">The email of the user.</param>
    /// <param name="Password">The password of the user.</param>
    public record LoginRequest(string Email, string Password);
}
