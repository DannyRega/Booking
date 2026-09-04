using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    /// <summary>
    /// Represents a request to create a new booking.
    /// </summary>
    /// <param name="SessionId">The ID of the session for which to create a booking.</param>
    public record CreateBookingRequest(int SessionId);
    /// <summary>
    /// Represents a response containing information about a booking.
    /// </summary>
    /// <param name="Id">The ID of the booking.</param>
    /// <param name="SessionId">The ID of the session for which the booking is created.</param>
    /// <param name="UserId">The ID of the user who created the booking.</param>
    /// <param name="CreatedAt">The date and time when the booking was created.</param>
    public record BookingResponseDto(int Id, int SessionId, int UserId, DateTime CreatedAt);
}
