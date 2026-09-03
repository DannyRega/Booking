using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions
{
    public class ConflictException(string message) : Exception(message);
    public class ForbiddenException(string message) : Exception(message);
    public class NotFoundException(string message) : Exception(message);
}
