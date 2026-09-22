using JobApply.Models;
using JobApply.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobApply.Controllers
{
    public class AutomacaoController : Controller
    {
        private readonly LinkedInService _linkedInService;

        public AutomacaoController(
            LinkedInService linkedInService)
        {
            _linkedInService = linkedInService;
        }

        public async Task<IActionResult> Index(
                                                string? busca,
                                                string? localizacao,
                                                string? periodo,
                                                bool aposCandidatura = false)
        {
            var vagas = new List<VagaLinkedIn>();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                vagas =
                    await _linkedInService.BuscarVagasAsync(
                        busca,
                        localizacao,
                        periodo);
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
                TempData["MensagemCandidatura"] =
                    "Link da vaga não informado.";

                TempData["TipoMensagem"] = "danger";

                return RedirectToAction(nameof(Index));
            }

            var resultado =
                await _linkedInService.CandidatarAsync(link);

            switch (resultado)
            {
                case "LOGIN_LINKEDIN":

                    TempData["MensagemCandidatura"] =
                        "O LinkedIn está solicitando login. Faça o login no navegador aberto para continuar.";

                    TempData["TipoMensagem"] = "warning";

                    break;

                case "LOGIN_SITE_EXTERNO":

                    TempData["MensagemCandidatura"] =
                        "O site da empresa está solicitando login. Faça o login no navegador aberto para continuar.";

                    TempData["TipoMensagem"] = "warning";

                    break;

                case "CANDIDATURA_EXTERNA_ABERTA":

                    TempData["MensagemCandidatura"] =
                        "Site da empresa aberto. A candidatura externa está pronta para continuar.";

                    TempData["TipoMensagem"] = "success";

                    break;

                case "CANDIDATURA_LINKEDIN_ABERTA":

                    TempData["MensagemCandidatura"] =
                        "Formulário de candidatura do LinkedIn aberto.";

                    TempData["TipoMensagem"] = "success";

                    break;

                case "LINK_EXTERNO_NAO_ENCONTRADO":

                    TempData["MensagemCandidatura"] =
                        "A candidatura externa foi encontrada, mas o link não foi localizado.";

                    TempData["TipoMensagem"] = "danger";

                    break;

                case "CANDIDATURA_NAO_ENCONTRADA":

                    TempData["MensagemCandidatura"] =
                        "Não foi possível localizar o próximo passo da candidatura.";

                    TempData["TipoMensagem"] = "warning";

                    break;

                case "CANDIDATURA_GUPY_ABERTA":

                    TempData["MensagemCandidatura"] =
                        "Candidatura da Gupy aberta.";

                    TempData["TipoMensagem"] = "success";

                    break;

                case "CANDIDATURA_GUPY_NAO_ENCONTRADA":

                    TempData["MensagemCandidatura"] =
                        "A página da Gupy foi aberta, mas o botão Candidatar-se não foi encontrado.";

                    TempData["TipoMensagem"] = "warning";

                    break;

                default:

                    TempData["MensagemCandidatura"] =
                        "Não foi possível identificar o processo de candidatura.";

                    TempData["TipoMensagem"] = "warning";

                    break;
            }

            // IMPORTANTE:
            // Não fazemos RedirectToAction(Index) aqui.
            // Isso evita executar novamente a busca no LinkedIn.

            return RedirectToAction(nameof(Index));
        }

    }

}