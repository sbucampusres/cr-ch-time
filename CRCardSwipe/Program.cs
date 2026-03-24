using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using CRCardSwipe.Data;
using CRCardSwipe.Middleware;
using CRCardSwipe.Models;
using CRCardSwipe.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Load template settings from configuration FIRST (needed by DbContext)
var templateSettings = builder.Configuration.GetSection("TemplateSettings").Get<TemplateSettings>() ?? new TemplateSettings();
builder.Services.AddSingleton(templateSettings);

// Load CardSwipe-specific settings
var cardSwipeSettings = builder.Configuration.GetSection("CardSwipe").Get<CardSwipeSettings>() ?? new CardSwipeSettings();
builder.Services.AddSingleton(cardSwipeSettings);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support for application context
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CRCardSwipe.Session";
});

// Add HttpContextAccessor for services that need it
builder.Services.AddHttpContextAccessor();

// Configure cookie-based authentication (no Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Unauthorized";
        options.AccessDeniedPath = "/Unauthorized";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Configure authorization policies based on AccessLevel
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole("Administrator"));
    options.AddPolicy("RequireOperator", policy =>
        policy.RequireRole("Administrator", "Operator"));
    options.AddPolicy("RequireViewer", policy =>
        policy.RequireRole("Administrator", "Operator", "Viewer"));
});

builder.Services.AddRazorPages();

// Register services
builder.Services.AddScoped<UserLookupService>();
builder.Services.AddScoped<IStoredProcService, StoredProcService>();
builder.Services.AddScoped<ICardSwipeService, CardSwipeService>();
builder.Services.AddScoped<IApplicationContextService, ApplicationContextService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Show detailed errors in development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware before authentication
app.UseSession();

// Add Shibboleth authorization middleware
app.UseMiddleware<ShibbolethAuthorizationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
