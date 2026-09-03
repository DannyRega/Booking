using System.Security.Claims;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DB Connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// Inyección de dependencias limpia cumpliendo DIP
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<BookingService>();

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Secret Key is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// POST /login
app.MapPost("/login", (LoginRequest req) =>
{
    if ((req.Email == "user1@test.com" || req.Email == "user2@test.com") && req.Password == "password123")
    {
        var userId = req.Email == "user1@test.com" ? 1 : 2;
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenDesc = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, req.Email)
            ]),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDesc);
        return Results.Ok(new { token = tokenHandler.WriteToken(token), user_id = userId });
    }
    return Results.Unauthorized();
});

// GET /sessions
app.MapGet("/sessions", async (
    DateTime? from,
    DateTime? to,
    string? instructor,
    bool? only_available,
    int? cursor,
    int? limit,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pageSize = Math.Clamp(limit ?? 20, 1, 100);
    var query = db.Sessions.AsNoTracking().AsQueryable();

    if (from.HasValue) query = query.Where(s => s.StartsAt >= from.Value);
    if (to.HasValue) query = query.Where(s => s.StartsAt <= to.Value);
    if (!string.IsNullOrWhiteSpace(instructor)) query = query.Where(s => s.Instructor == instructor);
    if (cursor.HasValue) query = query.Where(s => s.Id > cursor.Value);

    var projectedQuery = query.OrderBy(s => s.Id).Select(s => new SessionResponseDto(
        s.Id,
        s.Title,
        s.Instructor,
        s.StartsAt,
        s.DurationMinutes,
        s.Capacity,
        s.Capacity - db.Bookings.Count(b => b.SessionId == s.Id)
    ));

    if (only_available == true)
    {
        projectedQuery = projectedQuery.Where(s => s.AvailableSeats > 0);
    }

    var items = await projectedQuery.Take(pageSize + 1).ToListAsync(ct);
    var hasNext = items.Count > pageSize;
    var resultItems = items.Take(pageSize).ToList();
    var nextCursor = hasNext ? resultItems.Last().Id : (int?)null;

    return Results.Ok(new CursorPagedResult<SessionResponseDto>(resultItems, nextCursor, hasNext));
});

// POST /bookings
app.MapPost("/bookings", async (
    CreateBookingRequest req,
    HttpContext ctx,
    BookingService bookingService,
    CancellationToken ct) =>
{
    var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var userId)) return Results.Unauthorized();

    var idempotencyKey = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
    try
    {
        var (status, body) = await bookingService.CreateBookingAsync(userId, req.SessionId, idempotencyKey, ct);
        return Results.Json(body, statusCode: status);
    }
    catch (NotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
}).RequireAuthorization();

// DELETE /bookings/{id}
app.MapDelete("/bookings/{id:int}", async (
    int id,
    HttpContext ctx,
    BookingService bookingService,
    CancellationToken ct) =>
{
    var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var userId)) return Results.Unauthorized();

    try
    {
        await bookingService.CancelBookingAsync(id, userId, ct);
        return Results.NoContent();
    }
    catch (NotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (ForbiddenException ex) { return Results.Forbid(); }
    catch (ConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.Run();

public record LoginRequest(string Email, string Password);