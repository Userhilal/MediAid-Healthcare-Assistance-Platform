using System.Threading.RateLimiting;
using MediAid.Data;
using MediAid.Models;
using MediAid.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<MediAid.Filters.LayoutDataFilter>();
});

// MongoDB Configuration
var connectionString = builder.Configuration.GetConnectionString("MongoDB");
var databaseName = builder.Configuration.GetConnectionString("DatabaseName");
builder.Services.AddSingleton<MongoDbContext>(sp => 
    new MongoDbContext(connectionString!, databaseName!));

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAidantService, AidantService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<IProposalRecommendationService, ProposalRecommendationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IIntelligentAlertService, IntelligentAlertService>();
builder.Services.AddScoped<IAidantCommentService, AidantCommentService>();
builder.Services.AddScoped<IPlanningService, PlanningService>();
builder.Services.AddScoped<IPlatformStatsService, PlatformStatsService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
        
        // Configure SignalR to use cookies for authentication (for JWT Bearer)
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // Try query string first
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                // Try Authorization header
                else if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer "))
                    {
                        context.Token = authHeader.Substring(7);
                    }
                }
                
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"));
    options.AddPolicy("AidantOnly", policy => policy.RequireRole("Aidant"));
    options.AddPolicy("ExpertOnly", policy => policy.RequireRole("Expert"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100KB
});

// Configure SignalR to use cookies for authentication
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.EnableDetailedErrors = true;
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// MediAid cookie security configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "MediAid.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "MediAid.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
var app = builder.Build();



// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// MediAid security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(self)");

    await next();
});
app.UseRateLimiter();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<MediAid.Hubs.ChatHub>("/chathub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

app.Run();









