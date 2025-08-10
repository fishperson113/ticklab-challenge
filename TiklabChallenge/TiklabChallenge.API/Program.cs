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
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationContext>();

builder.Services.AddAuthentication(IdentityConstants.BearerScheme);
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!useInMemory)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationContext>();

        dbContext.Database.Migrate();
    }
}
app.MapIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();