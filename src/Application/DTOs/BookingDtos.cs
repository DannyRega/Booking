using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public record CreateBookingRequest(int SessionId);
    public record BookingResponseDto(int Id, int SessionId, int UserId, DateTime CreatedAt);
}
