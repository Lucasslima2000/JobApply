using JobApply.Data;
using JobApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobApply.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            _passwordHasher =
                new PasswordHasher<Usuario>();
        }

               
        // =========================================================
        // LOGIN
        // =========================================================

        [HttpPost("login")]
        public IActionResult Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return BadRequest(new
                {
                    mensagem = "O nome é obrigatório."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Senha))
            {
                return BadRequest(new
                {
                    mensagem = "A senha é obrigatória."
                });
            }

            string nome =
                request.Nome.Trim();

            var usuario =
                _context.Usuarios
                    .FirstOrDefault(
                        u =>
                            u.Nome == nome &&
                            u.Ativo
                    );

            if (usuario == null)
            {
                return Unauthorized(new
                {
                    mensagem =
                        "Nome ou senha inválidos."
                });
            }

            var resultado =
                _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.SenhaHash,
                    request.Senha
                );

            if (
                resultado ==
                PasswordVerificationResult.Failed
            )
            {
                return Unauthorized(new
                {
                    mensagem =
                        "Nome ou senha inválidos."
                });
            }

            string token =
                GerarToken(usuario);

            return Ok(new
            {
                token,

                usuario = new
                {
                    usuario.Id,
                    usuario.Nome,
                    usuario.Email
                }
            });
        }

        // =========================================================
        // GERAR TOKEN JWT
        // =========================================================

        private string GerarToken(
            Usuario usuario)
        {
            string? jwtKey =
                _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "Jwt:Key não configurada."
                );
            }

            var claims = new[]
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

            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                claims =
                    claims
                        .Append(
                            new Claim(
                                ClaimTypes.Email,
                                usuario.Email
                            )
                        )
                        .ToArray();
            }

            var chave =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                );

            var credenciais =
                new SigningCredentials(
                    chave,
                    SecurityAlgorithms.HmacSha256
                );

            var token =
                new JwtSecurityToken(
                    claims: claims,

                    expires:
                        DateTime.UtcNow.AddHours(8),

                    signingCredentials:
                        credenciais
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // =========================================================
        // REQUESTS
        // =========================================================       

        public class LoginRequest
        {
            public string Nome { get; set; } = string.Empty;

            public string Senha { get; set; } = string.Empty;
        }
    }
}