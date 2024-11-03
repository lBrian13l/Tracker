using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tracker.Middlewares;
using Tracker.Models;
using Tracker.Services;

var builder = WebApplication.CreateBuilder(args);

string? dbConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(dbConnection);
    options.EnableSensitiveDataLogging(true);
});

// Add services to the container.
builder.Services.AddRazorPages(options => options.Conventions.AuthorizeAreaFolder("Content", "/"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/accessdenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ILoginService, LoginService>();
builder.Services.AddScoped<LevelService, LevelService>();
builder.Services.AddScoped<DatabaseUpdateService, DatabaseUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.UseMiddleware<LevelMiddleware>();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Index");
});

app.MapGet("/page", () =>
{
    return Results.Redirect("/Content/Items");
});

app.MapPut("/increase", async (LevelService levelService, HttpContext context) =>
{
    await levelService.Increase();
    await context.Response.WriteAsJsonAsync(new { value = await levelService.GetLevelAsync() });
});

app.MapPut("/decrease", async (LevelService levelService, HttpContext context) =>
{
    await levelService.Decrease();
    await context.Response.WriteAsJsonAsync(new { value = await levelService.GetLevelAsync() });
});

app.Map("/update", [Authorize(Roles = "admin")] async (DatabaseUpdateService databaseUpdateService) =>
{
    await databaseUpdateService.UpdateDatabase();
    return Results.Redirect("/Index");
});

app.MapGet("/accessdenied", async (HttpContext context) =>
{
    context.Response.StatusCode = 403;
    await context.Response.WriteAsync("Access Denied");
});

app.Run();
