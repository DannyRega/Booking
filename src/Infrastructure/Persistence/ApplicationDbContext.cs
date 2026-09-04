using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;
/// <summary>
/// Represents the application's database context, providing access to the Users, Sessions, Bookings, and IdempotencyRecords tables in the database.
/// </summary>
/// <param name="options"></param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    /// <summary>
    /// Configures the model for the database context, specifying table names, primary keys, column names, and indexes for the User, Session, Booking, and IdempotencyRecord entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.Email).HasColumnName("email");
            b.Property(u => u.PasswordHash).HasColumnName("password_hash");
        });

        modelBuilder.Entity<Session>(b =>
        {
            b.ToTable("sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.Title).HasColumnName("title");
            b.Property(s => s.Instructor).HasColumnName("instructor");
            b.Property(s => s.StartsAt).HasColumnName("starts_at");
            b.Property(s => s.DurationMinutes).HasColumnName("duration_minutes");
            b.Property(s => s.Capacity).HasColumnName("capacity");

            b.HasIndex(s => s.StartsAt);
            b.HasIndex(s => s.Instructor);
        });

        modelBuilder.Entity<Booking>(b =>
        {
            b.ToTable("bookings");
            b.HasKey(bk => bk.Id);
            b.Property(bk => bk.Id).HasColumnName("id");
            b.Property(bk => bk.SessionId).HasColumnName("session_id");
            b.Property(bk => bk.UserId).HasColumnName("user_id");
            b.Property(bk => bk.CreatedAt).HasColumnName("created_at");

            b.HasIndex(bk => new { bk.SessionId, bk.UserId }).IsUnique();
            b.HasIndex(bk => bk.SessionId);
            b.HasIndex(bk => bk.UserId);
        });

        modelBuilder.Entity<IdempotencyRecord>(b =>
        {
            b.ToTable("idempotency_records");
            b.HasKey(i => i.Key);
            b.Property(i => i.Key).HasColumnName("key");
            b.Property(i => i.StatusCode).HasColumnName("status_code");
            b.Property(i => i.ResponseBody).HasColumnName("response_body");
            b.Property(i => i.CreatedAt).HasColumnName("created_at");
        });
    }
}