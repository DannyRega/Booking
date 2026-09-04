using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a record of an idempotent operation, storing the key, status code, response body, and creation timestamp.
    /// </summary>
    public class IdempotencyRecord
    {
        public string Key { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
