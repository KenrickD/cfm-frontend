using cfm_frontend.Filters;
using cfm_frontend.Handlers;
using cfm_frontend.Middleware;
using cfm_frontend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Register global exception filter for authentication errors
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuthenticationExceptionFilter>();
});

builder.Services.AddHttpClient();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// Configure antiforgery to use X-CSRF-TOKEN header for AJAX requests
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddScoped<IPrivilegeService, PrivilegeService>();
builder.Services.AddScoped<ISessionRestoreService, SessionRestoreService>();
builder.Services.AddSingleton<IFileLoggerService, FileLoggerService>();

// Persist Data Protection keys so auth cookies survive app restarts
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("CFM-Frontend");

builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendBaseUrl"]); 
})
.AddHttpMessageHandler<AuthTokenHandler>(); 

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Adjust as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseRouting();
app.UseSession();
app.UseAuthentication();

// Token expiration middleware - intercepts requests with expired tokens BEFORE controllers execute
// This is the PRIMARY FIX for the bug where pages load with no data when tokens expire
app.UseTokenExpiration();

app.UseAuthorization();

// Auto-restore session when expired but tokens still valid
app.UseSessionRestore();

// Work Request Detail route - maps /work-requests/{id} to HelpdeskController.WorkRequestDetail
app.MapControllerRoute(
    name: "work-request-detail",
    pattern: "work-requests/{id:int}",
    defaults: new { controller = "Helpdesk", action = "WorkRequestDetail" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=home}/{action=Index}/{id?}");

app.Run();
