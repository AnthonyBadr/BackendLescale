using backend;
using backend.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers() // Changed to AddControllers
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // This will keep property names as they are defined
    });
builder.Services.AddControllersWithViews(); // Keep this if you still need MVC views

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Specify your frontend origin
               .AllowAnyMethod()                     // Allow all HTTP methods (GET, POST, etc.)
               .AllowAnyHeader()                     // Allow all headers
               .AllowCredentials();                  // Allow credentials (e.g., cookies)
    });
});

// Configure MongoDB settings
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection(nameof(DatabaseSettings)));

// Register MongoClient with specific connection string
builder.Services.AddSingleton<IMongoClient>(s =>
    new MongoClient("mongodb+srv://joehadchity:J1j2j3j4@escale-royale-cluster.lann0.mongodb.net/?retryWrites=true&w=majority&authSource=admin&authMechanism=SCRAM-SHA-1&appName=escale-royale-cluster"));

builder.Services.AddSingleton<GlobalService>(); // Register GlobalService

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Apply CORS before routing and authorization
app.UseCors("AllowSpecificOrigin");

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
