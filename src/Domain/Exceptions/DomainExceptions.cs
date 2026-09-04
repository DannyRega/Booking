using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when a conflict occurs, such as when trying to create a resource that already exists.
    /// </summary>
    /// <param name="message">The message that describes the exception.</param>
    public class ConflictException(string message) : Exception(message);
    /// <summary>
    /// Represents an exception that is thrown when a request is forbidden, such as when a user does not have permission to access a resource.
    /// </summary>
    /// <param name="message">The message that describes the exception.</param>
    public class ForbiddenException(string message) : Exception(message);
    /// <summary>
    /// Represents an exception that is thrown when a resource is not found.
    /// </summary>
    /// <param name="message">The message that describes the exception.</param>
    public class NotFoundException(string message) : Exception(message);
}
