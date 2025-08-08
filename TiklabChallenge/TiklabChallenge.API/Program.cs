using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Data;
using TiklabChallenge.Infrastructure.Repository;
using TiklabChallenge.Infrastructure.UnitOfWork;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();