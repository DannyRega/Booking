using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a session that users can book, including details such as title, instructor, start time, duration, capacity, and associated bookings.
    /// </summary>
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
