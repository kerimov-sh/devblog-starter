using System.Text;
using System.Threading.RateLimiting;
using DevBlog.Api.Data;
using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;
using DevBlog.Api.Services;
using DevBlog.Api.Services.External;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Repositories & Services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IRagChunkRepository, RagChunkRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHttpClient<IVoyageEmbeddingClient, VoyageEmbeddingClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient<IClaudeChatClient, ClaudeChatClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

// 3. CORS — auth now relies on httpOnly cookies, so AllowAnyOrigin + AllowCredentials is not viable
// (browsers reject that combination). Origins are read from config so dev/prod can differ without a
// code change; falls back to the Angular dev server origin if not configured.
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedCorsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

// 4. JWT Authentication
var jwtSecret = "devblog-super-secret-key-2024-dev"; // TODO: move to config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // The JWT now travels as an httpOnly cookie instead of an Authorization header. If a caller still
        // sends a Bearer header (e.g. a non-browser client), that takes precedence; otherwise fall back to
        // reading the cookie so [Authorize]/RequireAuthorization keeps working transparently.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthEndpoint.AccessTokenCookieName, out var cookieToken) &&
                    !string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// 5. Authorization
builder.Services.AddAuthorization();

// 6. OpenAPI
builder.Services.AddOpenApi();

// 6.1 Rate limiting — paid dış API'lere gidiyor, /chat için basit bir koruma
// Not: ASP.NET Core'un varsayılan RejectionStatusCode'u 503'tür (Program.cs'teki
// ChatErrorCode.ServiceUnavailable durumuyla karışmaması için burada standart
// 429 Too Many Requests'e çeviriyoruz).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("chat", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

var app = builder.Build();

// 7. Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DataSeeder.Seed(db);

    var ragDbPath = Path.GetFullPath(Path.Combine(
        app.Environment.ContentRootPath,
        builder.Configuration["Rag:DbPath"] ?? "../../rag/rag.db"));
    RagChunkSeeder.Seed(db, ragDbPath);
}

app.UseCors();

// Double-submit-cookie CSRF protection: state-changing requests must echo the XSRF-TOKEN cookie value
// (set at login, JS-readable) back in the X-CSRF-Token header. GET/HEAD/OPTIONS are read-only and exempt;
// /auth/login is exempt because no XSRF cookie exists yet before a successful login.
app.Use(async (context, next) =>
{
    var request = context.Request;
    var isStateChangingMethod = HttpMethods.IsPost(request.Method)
        || HttpMethods.IsPut(request.Method)
        || HttpMethods.IsDelete(request.Method)
        || HttpMethods.IsPatch(request.Method);

    var isExempt = request.Path.StartsWithSegments("/auth/login")
        || request.Path.StartsWithSegments("/chat");

    if (isStateChangingMethod && !isExempt)
    {
        var cookieToken = request.Cookies[AuthEndpoint.XsrfCookieName];
        var headerToken = request.Headers["X-CSRF-Token"].ToString();

        if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken) || cookieToken != headerToken)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "CSRF token missing or invalid." });
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

PostsEndpoint.Map(app);
CommentsEndpoint.Map(app);
AuthEndpoint.Map(app);
LikesEndpoint.Map(app);
SearchEndpoint.Map(app);
ChatEndpoint.Map(app);

app.Run();
