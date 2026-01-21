using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using UserManagementApi.Data;
using UserManagementApi.Dtos;
using UserManagementApi.Middleware;
using UserManagementApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure API Key (for middleware)
builder.Configuration["ApiKey"] ??= "dev-key-change-me";

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core InMemory DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("UserDb"));

var app = builder.Build();

// Middleware: logging + simple auth
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed sample data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { Name = "Ada Lovelace", Email = "ada@example.com", Age = 28 },
            new User { Name = "Alan Turing", Email = "alan@example.com", Age = 41 }
        );
        db.SaveChanges();
    }
}

// Helper: validate DTOs using DataAnnotations (so Minimal API behaves like Controllers)
static Dictionary<string, string[]>? ValidateDto(object dto)
{
    var ctx = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    var ok = Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);
    if (ok) return null;

    return results
        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "")
        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid").ToArray());
}

// ---- CRUD Endpoints ----

// GET all users
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users.AsNoTracking().ToListAsync();
    return Results.Ok(users);
});

// GET user by id
app.MapGet("/api/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

// POST create user (validation + unique email check)
app.MapPost("/api/users", async (CreateUserDto dto, AppDbContext db) =>
{
    var errors = ValidateDto(dto);
    if (errors is not null) return Results.ValidationProblem(errors);

    var email = dto.Email.Trim().ToLowerInvariant();
    var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email);
    if (exists)
        return Results.Conflict(new { message = "Email already exists." });

    var user = new User
    {
        Name = dto.Name.Trim(),
        Email = email,
        Age = dto.Age,
        CreatedAtUtc = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
});

// PUT update user (validation + unique email check)
app.MapPut("/api/users/{id:guid}", async (Guid id, UpdateUserDto dto, AppDbContext db) =>
{
    var errors = ValidateDto(dto);
    if (errors is not null) return Results.ValidationProblem(errors);

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    if (user is null) return Results.NotFound();

    var email = dto.Email.Trim().ToLowerInvariant();
    var emailTaken = await db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email);
    if (emailTaken)
        return Results.Conflict(new { message = "Email already exists." });

    user.Name = dto.Name.Trim();
    user.Email = email;
    user.Age = dto.Age;
    user.UpdatedAtUtc = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(user);
});

// DELETE user
app.MapDelete("/api/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
