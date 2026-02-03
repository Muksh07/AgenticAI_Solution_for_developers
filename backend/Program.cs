using backend.Database;
using backend.Service;
using FluentValidation.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/log.txt", rollingInterval: RollingInterval.Day))
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();  // Use Serilog instead of default logging

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());;
// builder.Services.AddDbContext<DatabaseContext>();
// builder.Services.AddCors();
builder.Services.AddTransient<Functionality>();
// builder.Services.AddScoped<DabaseFunctionality>();
builder.Services.AddHttpClient(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();


// app.UseDefaultFiles();
// app.UseStaticFiles();
// app.MapFallbackToFile("index.html"); // For Angular routing

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // If your app is deployed at the root (e.g., http://server/):
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");


    options.RoutePrefix = "swagger"; // Default is "swagger". Use "" to serve at root.
});

app.UseDefaultFiles();
app.UseStaticFiles();

// app.UseHttpsRedirection();
app.UseRouting();  
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();


