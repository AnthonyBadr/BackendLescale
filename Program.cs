using backend;
using backend.Services;
using MongoDB.Driver;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.AspNetCore.Hosting;
using backend.Models;
using WorkerService1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Configure MongoDB settings
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection(nameof(DatabaseSettings)));
builder.Services.AddHostedService<Worker>();

// Register MongoClient with specific connection string
builder.Services.AddSingleton<IMongoClient>(s =>
    new MongoClient("mongodb+srv://joehadchity:J1j2j3j4@escale-royale-cluster.lann0.mongodb.net/?retryWrites=true&w=majority&authSource=admin&authMechanism=SCRAM-SHA-1&appName=escale-royale-cluster"));

builder.Services.AddSingleton<GlobalService>();

// Register IMongoDatabase
builder.Services.AddSingleton(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration.GetValue<string>("MongoDB:DatabaseName");
    return client.GetDatabase(databaseName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowSpecificOrigin");
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the app
app.Run();

// Create the host builder
IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    .UseWindowsService() // Add parentheses to invoke the method
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Worker>();
    });


// Build and run the host
CreateHostBuilder(args).Build().Run();
