using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DB Connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddScoped<ISessionRepository, SessionRepository>();
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Booking API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build(); 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
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
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] string? instructor,
    [FromQuery(Name = "only_available")] bool? onlyAvailable,
    [FromQuery] int? cursor,
    [FromQuery] int limit = 20,
    ISessionRepository sessionRepo = null!,
    CancellationToken ct = default) =>
{
    var result = await sessionRepo.GetSessionsAsync(from, to, instructor, onlyAvailable, cursor, limit, ct);
    return Results.Ok(result);
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
    catch (ForbiddenException) { return Results.Forbid(); }
    catch (ConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.Run();