using JobApply.Data;
using JobApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobApply.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(
            AppDbContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // =========================================================
        // LISTAR
        // GET /Usuarios
        // =========================================================

        [HttpGet]
        public IActionResult Index()
        {
            var usuarios =
                _context.Usuarios
                    .OrderBy(u => u.Nome)
                    .ToList();

            return View(usuarios);
        }

        // =========================================================
        // CRIAR
        // GET /Usuarios/Criar
        // =========================================================

        [HttpGet]
        public IActionResult Criar()
        {
            return View();
        }

        // =========================================================
        // CRIAR
        // POST /Usuarios/Criar
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            UsuarioFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string nome =
                model.Nome.Trim();

            bool nomeExiste =
                _context.Usuarios.Any(
                    u => u.Nome == nome
                );

            if (nomeExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Nome),
                    "Já existe um usuário com esse nome."
                );

                return View(model);
            }

            string? email = null;

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                email =
                    model.Email
                        .Trim()
                        .ToLowerInvariant();
            }

            var usuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Ativo = model.Ativo,
                DataCadastro = DateTime.UtcNow
            };

            usuario.SenhaHash =
                _passwordHasher.HashPassword(
                    usuario,
                    model.Senha
                );

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            TempData["Mensagem"] =
                "Usuário criado com sucesso.";

            TempData["TipoMensagem"] =
                "success";

            return RedirectToAction(
                nameof(Index)
            );
        }

        // =========================================================
        // EDITAR
        // GET /Usuarios/Editar/5
        // =========================================================

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var usuario =
                _context.Usuarios.Find(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var model =
                new UsuarioFormViewModel
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Ativo = usuario.Ativo
                };

            return View(model);
        }

        // =========================================================
        // EDITAR
        // POST /Usuarios/Editar
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            UsuarioFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario =
                await _context.Usuarios.FindAsync(
                    model.Id
                );

            if (usuario == null)
            {
                return NotFound();
            }

            string nome =
                model.Nome.Trim();

            bool nomeExiste =
                _context.Usuarios.Any(
                    u =>
                        u.Nome == nome &&
                        u.Id != model.Id
                );

            if (nomeExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Nome),
                    "Já existe outro usuário com esse nome."
                );

                return View(model);
            }

            string? email = null;

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                email =
                    model.Email
                        .Trim()
                        .ToLowerInvariant();
            }

            usuario.Nome = nome;
            usuario.Email = email;
            usuario.Ativo = model.Ativo;

            // Só altera a senha se uma nova senha foi informada.
            if (!string.IsNullOrWhiteSpace(model.Senha))
            {
                usuario.SenhaHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        model.Senha
                    );
            }

            await _context.SaveChangesAsync();

            TempData["Mensagem"] =
                "Usuário atualizado com sucesso.";

            TempData["TipoMensagem"] =
                "success";

            return RedirectToAction(
                nameof(Index)
            );
        }

        // =========================================================
        // EXCLUIR
        // GET /Usuarios/Excluir/5
        // =========================================================

        [HttpGet]
        public IActionResult Excluir(int id)
        {
            var usuario =
                _context.Usuarios.Find(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // =========================================================
        // EXCLUIR
        // POST /Usuarios/Excluir
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarExclusao(
            int id)
        {
            var usuario =
                await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Impede o usuário de excluir a própria conta
            // enquanto estiver usando ela.
            var usuarioLogadoId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                int.TryParse(
                    usuarioLogadoId,
                    out int idLogado
                )
                &&
                idLogado == usuario.Id
            )
            {
                TempData["Mensagem"] =
                    "Você não pode excluir o próprio usuário.";

                TempData["TipoMensagem"] =
                    "danger";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            _context.Usuarios.Remove(usuario);

            await _context.SaveChangesAsync();

            TempData["Mensagem"] =
                "Usuário excluído com sucesso.";

            TempData["TipoMensagem"] =
                "success";

            return RedirectToAction(
                nameof(Index)
            );
        }

        // =========================================================
        // VIEW MODEL
        // =========================================================

        public class UsuarioFormViewModel
        {
            public int Id { get; set; }

            public string Nome { get; set; } =
                string.Empty;

            public string? Email { get; set; }

            public string Senha { get; set; } =
                string.Empty;

            public bool Ativo { get; set; } = true;
        }
    }
}