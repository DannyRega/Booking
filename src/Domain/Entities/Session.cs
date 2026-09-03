using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Session
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Capacity { get; set; }
        public ICollection<Booking> Bookings { get; set; } = [];

        public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);
    }
}
