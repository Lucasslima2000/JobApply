using JobApply.Data;
using JobApply.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobApply.Controllers
{
    public class ContaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario>
            _passwordHasher;

        public ContaController(
            AppDbContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // =========================================================
        // LOGIN - TELA
        // =========================================================

        [AllowAnonymous]
        [HttpGet("/Login")]
        public IActionResult Login(
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // =========================================================
        // LOGIN - PROCESSAMENTO
        // =========================================================

        [AllowAnonymous]
        [HttpPost("/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;

                return View(model);
            }

            var usuario =
                _context.Usuarios
                    .FirstOrDefault(
                        u =>
                            u.Nome == model.Nome &&
                            u.Ativo
                    );

            if (usuario == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Nome ou senha inválidos."
                );

                return View(model);
            }

            var resultado =
                _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.SenhaHash,
                    model.Senha
                );

            if (
                resultado ==
                PasswordVerificationResult.Failed
            )
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Nome ou senha inválidos."
                );

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    usuario.Nome
                )
            };

            if (!string.IsNullOrWhiteSpace(
                usuario.Email))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Email,
                        usuario.Email
                    )
                );
            }

            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme
                );

            var principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal
            );

            if (
                !string.IsNullOrWhiteSpace(returnUrl)
                &&
                Url.IsLocalUrl(returnUrl)
            )
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home"
            );
        }
        

        // =========================================================
        // LOGOUT
        // =========================================================

        [Authorize]
        [HttpPost("/Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );

            return RedirectToAction(
                nameof(Login)
            );
        }
    }

    // =============================================================
    // VIEW MODELS
    // =============================================================

    public class LoginViewModel
    {
        public string Nome { get; set; } =
            string.Empty;

        public string Senha { get; set; } =
            string.Empty;
    }

    
}