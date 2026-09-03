using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions
{
    public class IdempotencyRecord
    {
        public string Key { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
