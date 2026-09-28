using JobApply.Data;
using JobApply.Models;
using JobApply.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;
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

// ============================================================
// ID DA EXECUÇÃO ATUAL
// ============================================================
//
// Cada vez que o "dotnet run" é executado,
// um novo ID é criado.
//
// Isso permite invalidar o cookie de uma execução anterior.
//
// Exemplo:
//
// Execução 1 → ID A
// Execução 2 → ID B
//
// O cookie da execução 1 terá ID A.
// Quando a execução 2 começar, ID A será rejeitado.
//

var appInstanceId = Guid.NewGuid().ToString();

builder.Services
    .AddAuthentication(options =>
    {
        // -----------------------------------------------------
        // AUTENTICAÇÃO PADRÃO DO SITE
        // -----------------------------------------------------

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
        // -----------------------------------------------------
        // QUANDO NÃO ESTIVER LOGADO
        // -----------------------------------------------------

        options.LoginPath = "/Login";

        options.AccessDeniedPath = "/Login";

        // -----------------------------------------------------
        // TEMPO DO LOGIN
        // -----------------------------------------------------

        options.ExpireTimeSpan =
            TimeSpan.FromHours(1);

        options.SlidingExpiration = true;

        // -----------------------------------------------------
        // EVENTOS DO COOKIE
        // -----------------------------------------------------

        options.Events =
            new CookieAuthenticationEvents
            {
                // =================================================
                // QUANDO O LOGIN É REALIZADO
                // =================================================

                OnSigningIn = context =>
                {
                    // Essa regra é somente para desenvolvimento.

                    if (builder.Environment.IsDevelopment())
                    {
                        if (
                            context.Principal?.Identity
                            is ClaimsIdentity identity
                        )
                        {
                            // Remove eventual ID antigo.

                            var claimExistente =
                                identity.FindFirst(
                                    "JobApplyAppInstance"
                                );

                            if (claimExistente != null)
                            {
                                identity.RemoveClaim(
                                    claimExistente
                                );
                            }

                            // Adiciona o ID da execução atual.

                            identity.AddClaim(
                                new Claim(
                                    "JobApplyAppInstance",
                                    appInstanceId
                                )
                            );
                        }
                    }

                    return Task.CompletedTask;
                },

                // =================================================
                // VALIDAÇÃO DO COOKIE
                // =================================================

                OnValidatePrincipal = async context =>
                {
                    // Em produção, não fazemos essa validação.
                    //
                    // Isso evita que uma reinicialização do servidor
                    // deslogue todos os usuários.

                    if (!builder.Environment.IsDevelopment())
                    {
                        return;
                    }

                    // Procura o ID da execução que criou o cookie.

                    var claim =
                        context.Principal?
                            .FindFirst(
                                "JobApplyAppInstance"
                            );

                    // =================================================
                    // COOKIE ANTIGO
                    // =================================================
                    //
                    // Se:
                    //
                    // 1. Não existe o claim
                    // OU
                    // 2. O ID é diferente do ID atual
                    //
                    // significa que o cookie pertence a uma
                    // execução anterior do JobApply.
                    //

                    if (
                        claim == null ||
                        claim.Value != appInstanceId
                    )
                    {
                        // Rejeita o usuário autenticado.

                        context.RejectPrincipal();

                        // Remove o cookie antigo do navegador.

                        await context.HttpContext
                            .SignOutAsync(
                                CookieAuthenticationDefaults
                                    .AuthenticationScheme
                            );
                    }
                }
            };
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
//
// Por padrão, TODAS as páginas exigem login.
//
// Para liberar uma página sem login,
// usamos:
//
// [AllowAnonymous]
//
// Exemplo:
//
// [AllowAnonymous]
// public IActionResult Login()
//
// Portanto:
// Login → público
// Home → protegido
// Automação → protegido
// Demais páginas → protegidas
//

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new Microsoft.AspNetCore.Authorization
            .AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

// ============================================================
// APLICAÇÃO
// ============================================================

var app = builder.Build();

// ============================================================
// AMBIENTE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();

// ============================================================
// ROTEAMENTO
// ============================================================

app.UseRouting();

// ============================================================
// AUTENTICAÇÃO
// ============================================================

app.UseAuthentication();

app.UseAuthorization();

// ============================================================
// ARQUIVOS ESTÁTICOS
// ============================================================

app.MapStaticAssets();

// ============================================================
// ROTAS MVC
// ============================================================
//
// Quando não houver uma rota específica,
// o sistema começa pela tela de Login.
//
// /
// ↓
// /Login
//
// Depois que o login for realizado,
// o ContaController manda o usuário para:
//
// /Home
//

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Conta}/{action=Login}/{id?}"
)
.WithStaticAssets();

// ============================================================
// ABRE A TELA DE LOGIN AUTOMATICAMENTE
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

// ============================================================
// EXECUÇÃO
// ============================================================

app.Run();