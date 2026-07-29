using System.Text;
using DevBlog.Api.Data;
using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;
using DevBlog.Api.Services;
using DevBlog.Api.Services.External;
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
builder.Services.AddHttpClient<IVoyageEmbeddingClient, VoyageEmbeddingClient>();
builder.Services.AddHttpClient<IClaudeChatClient, ClaudeChatClient>();

// 3. CORS — TODO: restrict in production
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()));

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
    });

// 5. Authorization
builder.Services.AddAuthorization();

// 6. OpenAPI
builder.Services.AddOpenApi();

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
app.UseAuthentication();
app.UseAuthorization();

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
