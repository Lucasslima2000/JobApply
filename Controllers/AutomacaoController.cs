using JobApply.Data;
using JobApply.Models;
using JobApply.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobApply.Controllers
{
    [Authorize]
    public class AutomacaoController : Controller
    {
        private readonly LinkedInService _linkedInService;
        private readonly AppDbContext _context;

        public AutomacaoController(
            LinkedInService linkedInService,
            AppDbContext context)
        {
            _linkedInService = linkedInService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? busca,
            string? localizacao,
            string? periodo,
            bool aposCandidatura = false)
        {
            var vagas = new List<VagaLinkedIn>();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                Console.WriteLine("==========================================");
                Console.WriteLine("INICIANDO PESQUISA PELO CONTROLLER");
                Console.WriteLine($"BUSCA: {busca}");
                Console.WriteLine($"LOCALIZAÇÃO: {localizacao}");
                Console.WriteLine($"PERÍODO: {periodo}");
                Console.WriteLine("==========================================");

                var resultado =
                    await _linkedInService.BuscarVagasAsync(
                        busca,
                        localizacao,
                        periodo);

                if (resultado == null)
                {
                    TempData["MensagemCandidatura"] =
                        "Não foi possível concluir a pesquisa. Faça login no LinkedIn na janela do navegador e tente novamente.";

                    TempData["TipoMensagem"] = "warning";

                    ViewBag.Busca = busca;
                    ViewBag.Localizacao = localizacao;
                    ViewBag.Periodo = periodo;
                    ViewBag.AposCandidatura = aposCandidatura;

                    return View(vagas);
                }

                vagas = resultado;

                Console.WriteLine(
                    $"PESQUISA FINALIZADA. VAGAS ENCONTRADAS: {vagas.Count}");
            }

            ViewBag.Busca = busca;
            ViewBag.Localizacao = localizacao;
            ViewBag.Periodo = periodo;
            ViewBag.AposCandidatura = aposCandidatura;

            return View(vagas);
        }

        [HttpGet]
        public async Task<IActionResult> Candidatar(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Link da vaga não informado.",
                    tipo = "danger"
                });
            }

            try
            {
                // ==========================================
                // USUÁRIO LOGADO
                // ==========================================

                var usuarioIdClaim =
                                User.FindFirstValue(
                                    ClaimTypes.NameIdentifier
                                );

                if (!int.TryParse(usuarioIdClaim, out int usuarioId))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "Não foi possível identificar o usuário logado.",
                        tipo = "danger"
                    });
                }

                var usuario =
                    await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null || !usuario.Ativo)
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "Usuário não encontrado ou inativo.",
                        tipo = "danger"
                    });
                }

                // ==========================================
                // VALIDAR DADOS DA CANDIDATURA
                // ==========================================

                if (string.IsNullOrWhiteSpace(usuario.Email))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "O usuário não possui e-mail cadastrado.",
                        tipo = "warning"
                    });
                }

                if (string.IsNullOrWhiteSpace(usuario.Telefone))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "O usuário não possui telefone cadastrado.",
                        tipo = "warning"
                    });
                }

                if (string.IsNullOrWhiteSpace(usuario.Pais))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "O usuário não possui país cadastrado.",
                        tipo = "warning"
                    });
                }

                Console.WriteLine("==========================================");
                Console.WriteLine("INICIANDO CANDIDATURA");
                Console.WriteLine($"LINK: {link}");
                Console.WriteLine($"USUÁRIO: {usuario.Nome}");
                Console.WriteLine($"E-MAIL: {usuario.Email}");
                Console.WriteLine($"TELEFONE: {usuario.Telefone}");
                Console.WriteLine($"PAÍS: {usuario.Pais}");
                Console.WriteLine("==========================================");

                var resultado =
                    await _linkedInService.CandidatarAsync(
                        link,
                        usuario
                    );

                switch (resultado)
                {
                    case "LOGIN_LINKEDIN":
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "O LinkedIn está solicitando login. Faça o login na janela do navegador para continuar.",
                            tipo = "warning"
                        });

                    case "LOGIN_SITE_EXTERNO":
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "O site da empresa está solicitando login. Faça o login na janela do navegador para continuar.",
                            tipo = "warning"
                        });

                    case "CANDIDATURA_EXTERNA_ABERTA":
                        return Json(new
                        {
                            sucesso = true,
                            mensagem =
                                "Site da empresa aberto. A candidatura externa está pronta para continuar.",
                            tipo = "success"
                        });

                    case "CANDIDATURA_LINKEDIN_ABERTA":
                        return Json(new
                        {
                            sucesso = true,
                            mensagem =
                                "Formulário de candidatura do LinkedIn aberto.",
                            tipo = "success"
                        });

                    case "LINK_EXTERNO_NAO_ENCONTRADO":
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "A candidatura externa foi encontrada, mas o link não foi localizado.",
                            tipo = "danger"
                        });

                    case "CANDIDATURA_NAO_ENCONTRADA":
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "Não foi possível localizar o próximo passo da candidatura.",
                            tipo = "warning"
                        });

                    case "CANDIDATURA_GUPY_ABERTA":
                        return Json(new
                        {
                            sucesso = true,
                            mensagem =
                                "Candidatura da Gupy aberta.",
                            tipo = "success"
                        });

                    case "CANDIDATURA_GUPY_NAO_ENCONTRADA":
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "A página da Gupy foi aberta, mas o botão Candidatar-se não foi encontrado.",
                            tipo = "warning"
                        });

                    default:
                        return Json(new
                        {
                            sucesso = false,
                            mensagem =
                                "Não foi possível identificar o processo de candidatura.",
                            tipo = "warning"
                        });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "==========================================");

                Console.WriteLine(
                    $"ERRO AO PROCESSAR CANDIDATURA: {ex.Message}");

                Console.WriteLine(
                    "==========================================");

                return Json(new
                {
                    sucesso = false,
                    mensagem =
                        "Ocorreu um erro ao tentar iniciar a candidatura.",
                    tipo = "danger"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detalhes(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Link da vaga não informado."
                });
            }

            var detalhes =
                await _linkedInService.ObterDetalhesVagaAsync(link);

            if (detalhes == null)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível obter os detalhes da vaga."
                });
            }

            return Json(new
            {
                sucesso = true,
                dados = detalhes
            });
        }
    }
}