using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Data;
using TiklabChallenge.Infrastructure.Repository;
using TiklabChallenge.Infrastructure.UnitOfWork;
using TiklabChallenge.UseCases.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tiklab API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "Nhập token theo dạng: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",                      
        BearerFormat = "Opaque"                 
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


bool useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (useInMemory)
{
    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseInMemoryDatabase("TiklabInMemoryDb"));
}
else
{
    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseNpgsql(connectionString)); 
}

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStudentRepository,StudentRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<CourseSchedulingService>();
builder.Services.AddScoped<SubjectManagementService>();
builder.Services.AddScoped<StudentEnrollmentService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationContext>();

builder.Services.AddAuthentication(IdentityConstants.BearerScheme);
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>();
builder.Services.AddScoped<AppSeeder>();
builder.Services.AddHostedService<SeedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationContext>();
    if(useInMemory)
    {
        dbContext.Database.EnsureCreated();
    }
    else
    {
       dbContext.Database.Migrate();
    }    
}
app.MapGet("/_debug/users", async (ApplicationContext db) => new {
    Users = await db.Users.Select(u => new { u.Id, u.UserName, u.Email }).ToListAsync(),
    Roles = await db.Roles.Select(r => r.Name).ToListAsync()
});
// New debug endpoints for academic data
var debugGroup = app.MapGroup("/_debug");

debugGroup.MapGet("/subjects", async (ApplicationContext db) =>
    await db.Subjects
        .Select(s => new {
            s.SubjectCode,
            s.SubjectName,
            s.Description,
            s.DefaultCredits,
            s.PrerequisiteSubjectCode
        })
        .ToListAsync());
debugGroup.MapGet("/courses", async (ApplicationContext db) =>
    await db.Courses
        .Select(c => new {
            c.CourseCode,
            c.SubjectCode,
            c.MaxEnrollment,
            c.CreatedAt,
            SubjectName = c.Subject.SubjectName
        })
        .ToListAsync());
debugGroup.MapGet("/schedules", async (ApplicationContext db) =>
    await db.Schedules
        .Select(s => new {
            s.Id,
            s.RoomId,
            s.CourseCode,
            Day = s.DayOfWeek.ToString(),
            s.StartTime,
            s.EndTime,
            CourseName = s.Course.Subject.SubjectName
        })
        .ToListAsync());
var identity = app.MapGroup("");                   
identity.MapCustomIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();