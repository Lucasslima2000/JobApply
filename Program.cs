using JobApply.Data;
using JobApply.Models;
using JobApply.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// BANCO
// ============================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

// ============================================================
// SERVIÇOS
// ============================================================

builder.Services.AddSingleton<LinkedInService>();

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>
>();

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();

// ============================================================
// AUTENTICAÇÃO
// ============================================================

var jwtKey =
    builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada. " +
        "Configure usando User Secrets."
    );
}

builder.Services
    .AddAuthentication(options =>
    {
        // Navegador usa Cookie.
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })

    // =========================================================
    // COOKIE - LOGIN DO SITE
    // =========================================================

    .AddCookie(options =>
    {
        options.LoginPath = "/Login";

        options.AccessDeniedPath = "/Login";

        options.ExpireTimeSpan =
            TimeSpan.FromHours(1);

        options.SlidingExpiration = true;
    })

    // =========================================================
    // JWT - API
    // =========================================================

    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtKey
                            )
                        ),

                    ValidateIssuer = false,
                    ValidateAudience = false,

                    ValidateLifetime = true,

                    ClockSkew =
                        TimeSpan.Zero
                };
        }
    );

// ============================================================
// AUTORIZAÇÃO
// ============================================================

builder.Services.AddAuthorization(options =>
{
    // Por padrão, tudo exige autenticação.
    //
    // Login/Cadastro/API Auth usam [AllowAnonymous].
    options.FallbackPolicy =
        new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

// ============================================================
// PIPELINE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// ============================================================
// ROTAS MVC
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Conta}/{action=Login}/{id?}"
)
.WithStaticAssets();

// ============================================================
// ABRE A TELA DE LOGIN
// ============================================================

if (app.Environment.IsDevelopment())
{
    var url =
        "http://localhost:5192/Login";

    Process.Start(
        new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        }
    );
}

app.Run();