using backend.Services;
using backend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Set the URL for the application to run on port 7092
builder.WebHost.UseUrls("https://localhost:7092"); // Use this to specify the port before building

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

// Configure JWT authentication
var key = Encoding.ASCII.GetBytes("yourSecretKey");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "yourIssuer",
        ValidAudience = "yourAudience",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
