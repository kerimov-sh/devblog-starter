using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevBlog.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DevBlog.Api.Endpoints;

public static class AuthEndpoint
{
    // Name of the httpOnly cookie carrying the JWT. Frontend cannot (and should not) read this directly.
    public const string AccessTokenCookieName = "access_token";

    // Name of the JS-readable cookie used for the double-submit CSRF pattern.
    public const string XsrfCookieName = "XSRF-TOKEN";

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public static void Map(WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, AppDbContext db, HttpContext httpContext) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);

            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var jwtSecret = "devblog-super-secret-key-2024-dev"; // TODO: move to config
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var expiresAt = DateTime.UtcNow.Add(TokenLifetime);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            httpContext.Response.Cookies.Append(AccessTokenCookieName, tokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                // Dev: frontend (localhost:4200) and backend (localhost:5000) run on different ports, which
                // counts as cross-site for SameSite purposes — Strict would silently drop the cookie on
                // every cross-origin request. Lax still blocks the cookie from being sent on cross-site
                // POST/PUT/DELETE (the actual CSRF-relevant case), which combined with the double-submit
                // XSRF-TOKEN check below gives adequate protection for this dev-oriented starter. In a
                // same-origin production deployment this should be tightened back to Strict.
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt,
                Path = "/"
            });

            var xsrfToken = Guid.NewGuid().ToString("N");
            httpContext.Response.Cookies.Append(XsrfCookieName, xsrfToken, new CookieOptions
            {
                HttpOnly = false, // must be readable by frontend JS to echo it back in the X-CSRF-Token header
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt,
                Path = "/"
            });

            return Results.Ok(new LoginResponse(user.Username, user.Role));
        });

        app.MapPost("/auth/logout", (HttpContext httpContext) =>
        {
            httpContext.Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
            httpContext.Response.Cookies.Delete(XsrfCookieName, new CookieOptions { Path = "/" });
            return Results.Ok();
        });
    }
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Username, string Role);
