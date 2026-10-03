using Microsoft.Playwright;
using JobApply.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobApply.Services
{
    public class LinkedInService
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        private async Task<IPage> ObterPaginaAsync()
        {
            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
            }

            if (_context == null)
            {
                var pastaPerfil = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "linkedin-profile"
                );

                _context =
                    await _playwright.Chromium.LaunchPersistentContextAsync(
                        pastaPerfil,
                        new BrowserTypeLaunchPersistentContextOptions
                        {
                            Headless = false
                        }
                    );
            }

            if (_page == null || _page.IsClosed)
            {
                _page = await _context.NewPageAsync();
            }

            return _page;
        }

        // ============================================================
        // CANDIDATURA
        // ============================================================

        public async Task<string> CandidatarAsync(string linkVaga, Usuario usuario)
        {
            var page = await ObterPaginaAsync();

            Console.WriteLine("==========================================");
            Console.WriteLine("INICIANDO CANDIDATURA");
            Console.WriteLine($"LINK: {linkVaga}");
            Console.WriteLine("==========================================");

            Console.WriteLine($"Abrindo vaga: {linkVaga}");

            // ============================================================
            // ABRIR VAGA NO LINKEDIN
            // ============================================================

            try
            {
                await page.GotoAsync(
                    linkVaga,
                    new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });

                Console.WriteLine(
                    "========== 2 - TERMINOU GotoAsync =========="
                );
            }
            catch (TimeoutException)
            {
                Console.WriteLine(
                    "O LinkedIn demorou para carregar completamente."
                );

                Console.WriteLine(
                    "Continuando mesmo assim..."
                );
            }

            // Dar tempo para o conteúdo dinâmico do LinkedIn carregar
            await page.WaitForTimeoutAsync(3000);

            Console.WriteLine(
                $"Página atual do LinkedIn: {page.Url}"
            );

            // ============================================================
            // VERIFICAR LOGIN DO LINKEDIN
            // ============================================================

            Console.WriteLine(
                "========== VERIFICANDO LOGIN LINKEDIN =========="
            );

            bool precisaLogin =
                await PrecisaLoginLinkedInAsync(page);

            if (precisaLogin)
            {
                Console.WriteLine(
                    "================================================"
                );

                Console.WriteLine(
                    "LOGIN NECESSÁRIO NO LINKEDIN."
                );

                Console.WriteLine(
                    "O usuário precisa fazer login antes de continuar."
                );

                Console.WriteLine(
                    "================================================"
                );

                return "LOGIN_LINKEDIN";
            }

            // ============================================================
            // AGUARDAR CARREGAMENTO DO BOTÃO DE CANDIDATURA
            // ============================================================

            Console.WriteLine(
                "Aguardando carregamento do botão de candidatura..."
            );

            await page.WaitForTimeoutAsync(2000);

            // ============================================================
            // LOCALIZAR BOTÃO DE CANDIDATURA
            // ============================================================

            ILocator? botaoCandidatar = null;

            string seletorEncontrado = "";

            // ------------------------------------------------------------
            // TENTATIVA 1
            // Botões com texto Candidate-se / Candidatar-se
            // ------------------------------------------------------------

            try
            {
                var botoesTexto =
                    page.Locator(
                        "button:has-text('Candidate-se'), " +
                        "button:has-text('Candidatar-se'), " +
                        "a:has-text('Candidate-se'), " +
                        "a:has-text('Candidatar-se')"
                    );

                var quantidadeBotoesTexto =
                    await botoesTexto.CountAsync();

                Console.WriteLine(
                    $"Botões/links por texto encontrados: {quantidadeBotoesTexto}"
                );

                for (int i = 0; i < quantidadeBotoesTexto; i++)
                {
                    try
                    {
                        var candidato =
                            botoesTexto.Nth(i);

                        if (await candidato.IsVisibleAsync())
                        {
                            botaoCandidatar = candidato;
                            seletorEncontrado =
                                "button/a com texto Candidate-se ou Candidatar-se";

                            Console.WriteLine(
                                $"Botão de candidatura encontrado no índice {i}."
                            );

                            break;
                        }
                    }
                    catch
                    {
                        // Continua procurando.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro na busca por texto: {ex.Message}"
                );
            }

            // ------------------------------------------------------------
            // TENTATIVA 2
            // aria-label
            // ------------------------------------------------------------

            if (botaoCandidatar == null)
            {
                try
                {
                    var botoesAria =
                        page.Locator(
                            "[aria-label*='Candidate-se'], " +
                            "[aria-label*='Candidatar-se'], " +
                            "[aria-label*='candidatura'], " +
                            "[aria-label*='candidatar']"
                        );

                    var quantidadeAria =
                        await botoesAria.CountAsync();

                    Console.WriteLine(
                        $"Elementos por aria-label encontrados: {quantidadeAria}"
                    );

                    for (int i = 0; i < quantidadeAria; i++)
                    {
                        try
                        {
                            var candidato =
                                botoesAria.Nth(i);

                            if (await candidato.IsVisibleAsync())
                            {
                                botaoCandidatar = candidato;
                                seletorEncontrado =
                                    "aria-label de candidatura";

                                Console.WriteLine(
                                    $"Botão encontrado pelo aria-label no índice {i}."
                                );

                                break;
                            }
                        }
                        catch
                        {
                            // Continua procurando.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro na busca por aria-label: {ex.Message}"
                    );
                }
            }

            // ------------------------------------------------------------
            // TENTATIVA 3
            // data-test / classes conhecidas do LinkedIn
            // ------------------------------------------------------------

            if (botaoCandidatar == null)
            {
                try
                {
                    var botoesLinkedIn =
                        page.Locator(
                            "button[data-test-id*='apply'], " +
                            "button[data-control-name*='apply'], " +
                            "button[class*='apply'], " +
                            "a[data-test-id*='apply'], " +
                            "a[data-control-name*='apply']"
                        );

                    var quantidadeLinkedIn =
                        await botoesLinkedIn.CountAsync();

                    Console.WriteLine(
                        $"Elementos pelos seletores de candidatura encontrados: {quantidadeLinkedIn}"
                    );

                    for (int i = 0; i < quantidadeLinkedIn; i++)
                    {
                        try
                        {
                            var candidato =
                                botoesLinkedIn.Nth(i);

                            if (await candidato.IsVisibleAsync())
                            {
                                botaoCandidatar = candidato;
                                seletorEncontrado =
                                    "seletor de candidatura do LinkedIn";

                                Console.WriteLine(
                                    $"Botão encontrado pelo seletor LinkedIn no índice {i}."
                                );

                                break;
                            }
                        }
                        catch
                        {
                            // Continua procurando.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro nos seletores LinkedIn: {ex.Message}"
                    );
                }
            }

            // ------------------------------------------------------------
            // TENTATIVA 4
            // GetByText sem Exact
            // ------------------------------------------------------------

            if (botaoCandidatar == null)
            {
                try
                {
                    var textos =
                        new[]
                        {
                    "Candidatar-se",
                    "Candidate-se"
                        };

                    foreach (var texto in textos)
                    {
                        try
                        {
                            var elemento =
                                page.GetByText(
                                    texto,
                                    new PageGetByTextOptions
                                    {
                                        Exact = false
                                    });

                            var quantidade =
                                await elemento.CountAsync();

                            Console.WriteLine(
                                $"Texto '{texto}' encontrado: {quantidade}"
                            );

                            for (int i = 0; i < quantidade; i++)
                            {
                                try
                                {
                                    var candidato =
                                        elemento.Nth(i);

                                    if (!await candidato.IsVisibleAsync())
                                        continue;

                                    // Primeiro tenta descobrir se o texto
                                    // está dentro de um botão.
                                    var botaoPai =
                                        candidato.Locator(
                                            "xpath=ancestor::button[1]"
                                        );

                                    if (await botaoPai.CountAsync() > 0 &&
                                        await botaoPai.First.IsVisibleAsync())
                                    {
                                        botaoCandidatar =
                                            botaoPai.First;

                                        seletorEncontrado =
                                            $"texto '{texto}' dentro de botão";

                                        Console.WriteLine(
                                            $"Botão encontrado através do texto '{texto}' dentro de um botão."
                                        );

                                        break;
                                    }

                                    // Caso seja um link.
                                    var linkPai =
                                        candidato.Locator(
                                            "xpath=ancestor::a[1]"
                                        );

                                    if (await linkPai.CountAsync() > 0 &&
                                        await linkPai.First.IsVisibleAsync())
                                    {
                                        botaoCandidatar =
                                            linkPai.First;

                                        seletorEncontrado =
                                            $"texto '{texto}' dentro de link";

                                        Console.WriteLine(
                                            $"Link de candidatura encontrado através do texto '{texto}'."
                                        );

                                        break;
                                    }

                                    // Último recurso: usar o próprio elemento.
                                    botaoCandidatar =
                                        candidato;

                                    seletorEncontrado =
                                        $"texto '{texto}'";

                                    Console.WriteLine(
                                        $"Elemento de candidatura encontrado através do texto '{texto}'."
                                    );

                                    break;
                                }
                                catch
                                {
                                    // Continua procurando.
                                }
                            }

                            if (botaoCandidatar != null)
                                break;
                        }
                        catch
                        {
                            // Continua para o próximo texto.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro na busca textual de candidatura: {ex.Message}"
                    );
                }
            }

            // ============================================================
            // SE NÃO ENCONTROU, ESPERAR E TENTAR NOVAMENTE
            // ============================================================

            if (botaoCandidatar == null)
            {
                Console.WriteLine(
                    "Botão ainda não encontrado."
                );

                Console.WriteLine(
                    "Aguardando mais alguns segundos para o LinkedIn renderizar a página..."
                );

                await page.WaitForTimeoutAsync(5000);

                try
                {
                    var botoes =
                        page.Locator(
                            "button:has-text('Candidate-se'), " +
                            "button:has-text('Candidatar-se'), " +
                            "a:has-text('Candidate-se'), " +
                            "a:has-text('Candidatar-se')"
                        );

                    var quantidade =
                        await botoes.CountAsync();

                    Console.WriteLine(
                        $"Nova tentativa encontrou: {quantidade} elementos."
                    );

                    for (int i = 0; i < quantidade; i++)
                    {
                        try
                        {
                            var candidato =
                                botoes.Nth(i);

                            if (await candidato.IsVisibleAsync())
                            {
                                botaoCandidatar = candidato;
                                seletorEncontrado =
                                    "nova tentativa por texto";

                                Console.WriteLine(
                                    $"Botão encontrado na nova tentativa. Índice: {i}"
                                );

                                break;
                            }
                        }
                        catch
                        {
                            // Continua.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro na segunda tentativa: {ex.Message}"
                    );
                }
            }

            // ============================================================
            // NÃO ENCONTROU O BOTÃO
            // ============================================================

            if (botaoCandidatar == null)
            {
                Console.WriteLine(
                    "================================================"
                );

                Console.WriteLine(
                    "BOTÃO DE CANDIDATURA NÃO ENCONTRADO."
                );

                Console.WriteLine(
                    $"URL ATUAL: {page.Url}"
                );

                Console.WriteLine(
                    "================================================"
                );

                return "CANDIDATURA_NAO_ENCONTRADA";
            }

            Console.WriteLine(
                "================================================"
            );

            Console.WriteLine(
                "BOTÃO DE CANDIDATURA ENCONTRADO."
            );

            Console.WriteLine(
                $"Método: {seletorEncontrado}"
            );

            Console.WriteLine(
                "================================================"
            );

            // ============================================================
            // CLICAR NO BOTÃO
            // ============================================================

            IPage? paginaExterna = null;

            try
            {
                await botaoCandidatar
                    .ScrollIntoViewIfNeededAsync();

                await page.WaitForTimeoutAsync(500);

                Console.WriteLine(
                    "Clicando no botão de candidatura..."
                );

                // IMPORTANTE:
                // O clique acontece UMA ÚNICA VEZ.
                // Se abrir popup, capturamos a nova página.
                // Se não abrir popup, continuamos usando a página atual.

                try
                {
                    paginaExterna =
                        await page.RunAndWaitForPopupAsync(
                            async () =>
                            {
                                await botaoCandidatar
                                    .ClickAsync(
                                        new LocatorClickOptions
                                        {
                                            Timeout = 10000
                                        });
                            },
                            new PageRunAndWaitForPopupOptions
                            {
                                Timeout = 5000
                            });

                    Console.WriteLine(
                        "Nova página/janela detectada após o clique."
                    );
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(
                        "Nenhuma nova janela foi detectada."
                    );

                    Console.WriteLine(
                        "O clique pode ter aberto a candidatura na página atual."
                    );
                }

                Console.WriteLine(
                    "Botão de candidatura clicado."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao clicar no botão de candidatura: {ex.Message}"
                );

                return "ERRO_AO_ABRIR_CANDIDATURA";
            }

            // ============================================================
            // AGUARDAR A RESPOSTA DO LINKEDIN
            // ============================================================

            await page.WaitForTimeoutAsync(5000);

            // ============================================================
            // VERIFICAR NOVA PÁGINA / SITE EXTERNO
            // ============================================================

            if (paginaExterna != null)
            {
                try
                {
                    await paginaExterna.WaitForTimeoutAsync(5000);
                }
                catch
                {
                    // Continua mesmo se a página tiver algum atraso.
                }

                Console.WriteLine(
                    $"Nova página de candidatura: {paginaExterna.Url}"
                );

                if (!paginaExterna.IsClosed)
                {
                    // ========================================================
                    // GUPY
                    // ========================================================

                    if (paginaExterna.Url.Contains(
                            "gupy.io",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine(
                            "Site Gupy detectado."
                        );

                        if (await PrecisaLoginSiteExternoAsync(paginaExterna))
                        {
                            Console.WriteLine(
                                "LOGIN NECESSÁRIO NA GUPY."
                            );

                            Console.WriteLine(
                                "Faça o login manualmente no navegador."
                            );

                            var loginConcluido =
                                await AguardarLoginSiteExternoAsync(
                                    paginaExterna);

                            if (!loginConcluido)
                            {
                                Console.WriteLine(
                                    "Não foi possível concluir o login."
                                );

                                return "LOGIN_SITE_EXTERNO";
                            }

                            Console.WriteLine(
                                "Login da Gupy concluído."
                            );
                        }

                        var botaoGupy =
                            paginaExterna.Locator(
                                "a[data-testid='apply-link']"
                            );

                        var quantidadeGupy =
                            await botaoGupy.CountAsync();

                        Console.WriteLine(
                            $"Botões de candidatura da Gupy encontrados: {quantidadeGupy}"
                        );

                        if (quantidadeGupy > 0)
                        {
                            Console.WriteLine(
                                "Botão 'Candidatar-se' da Gupy encontrado."
                            );

                            await botaoGupy
                                .First
                                .ScrollIntoViewIfNeededAsync();

                            await botaoGupy
                                .First
                                .ClickAsync();

                            Console.WriteLine(
                                "Botão 'Candidatar-se' da Gupy clicado."
                            );

                            await paginaExterna.WaitForTimeoutAsync(5000);

                            Console.WriteLine(
                                $"Página após clicar na Gupy: {paginaExterna.Url}"
                            );

                            if (await PrecisaLoginSiteExternoAsync(
                                    paginaExterna))
                            {
                                Console.WriteLine(
                                    "LOGIN NECESSÁRIO APÓS CLICAR EM CANDIDATAR-SE NA GUPY."
                                );

                                return "LOGIN_SITE_EXTERNO";
                            }

                            return "CANDIDATURA_GUPY_ABERTA";
                        }

                        Console.WriteLine(
                            "Botão 'Candidatar-se' da Gupy não encontrado."
                        );

                        return "CANDIDATURA_GUPY_NAO_ENCONTRADA";
                    }

                    // ========================================================
                    // OUTROS SITES EXTERNOS
                    // ========================================================

                    if (!paginaExterna.Url.Contains(
                            "linkedin.com",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine(
                            "O LinkedIn abriu diretamente um site externo."
                        );

                        if (await PrecisaLoginSiteExternoAsync(
                                paginaExterna))
                        {
                            Console.WriteLine(
                                "LOGIN NECESSÁRIO NO SITE DA EMPRESA."
                            );

                            return "LOGIN_SITE_EXTERNO";
                        }

                        Console.WriteLine(
                            "Site externo pronto para iniciar candidatura."
                        );

                        return "CANDIDATURA_EXTERNA_ABERTA";
                    }
                }
            }

            // ============================================================
            // VERIFICAR URL DA PÁGINA ATUAL
            // ============================================================

            Console.WriteLine(
                $"Página após clicar no botão: {page.Url}"
            );

            // ============================================================
            // GUPY NA PRÓPRIA PÁGINA
            // ============================================================

            if (page.Url.Contains(
                    "gupy.io",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    "Site Gupy detectado na página atual."
                );

                if (await PrecisaLoginSiteExternoAsync(page))
                {
                    Console.WriteLine(
                        "LOGIN NECESSÁRIO NO SITE DA GUPY."
                    );

                    return "LOGIN_SITE_EXTERNO";
                }

                var botaoGupy =
                    page.Locator(
                        "a[data-testid='apply-link']"
                    );

                var quantidadeGupy =
                    await botaoGupy.CountAsync();

                Console.WriteLine(
                    $"Botões de candidatura da Gupy encontrados: {quantidadeGupy}"
                );

                if (quantidadeGupy > 0)
                {
                    Console.WriteLine(
                        "Botão 'Candidatar-se' da Gupy encontrado."
                    );

                    await botaoGupy
                        .First
                        .ScrollIntoViewIfNeededAsync();

                    await botaoGupy
                        .First
                        .ClickAsync();

                    Console.WriteLine(
                        "Botão 'Candidatar-se' da Gupy clicado."
                    );

                    await page.WaitForTimeoutAsync(5000);

                    Console.WriteLine(
                        $"Página após clicar na Gupy: {page.Url}"
                    );

                    if (await PrecisaLoginSiteExternoAsync(page))
                    {
                        Console.WriteLine(
                            "LOGIN NECESSÁRIO APÓS CLICAR EM CANDIDATAR-SE NA GUPY."
                        );

                        return "LOGIN_SITE_EXTERNO";
                    }

                    return "CANDIDATURA_GUPY_ABERTA";
                }

                Console.WriteLine(
                    "Botão 'Candidatar-se' da Gupy não encontrado."
                );

                return "CANDIDATURA_GUPY_NAO_ENCONTRADA";
            }

            // ============================================================
            // SE A PRÓPRIA PÁGINA SAIU DO LINKEDIN
            // ============================================================

            if (!page.Url.Contains(
                    "linkedin.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    "A candidatura foi aberta diretamente na página atual."
                );

                if (await PrecisaLoginSiteExternoAsync(page))
                {
                    Console.WriteLine(
                        "LOGIN NECESSÁRIO NO SITE DA EMPRESA."
                    );

                    return "LOGIN_SITE_EXTERNO";
                }

                Console.WriteLine(
                    "Site externo pronto para iniciar candidatura."
                );

                return "CANDIDATURA_EXTERNA_ABERTA";
            }

            // ============================================================
            // VERIFICAR CANDIDATURA EXTERNA DENTRO DO LINKEDIN
            // ============================================================

            var botaoExterno =
                page.Locator(
                    "a[aria-label='Candidatar-se no site da empresa']"
                );

            if (await botaoExterno.CountAsync() > 0)
            {
                Console.WriteLine(
                    "Candidatura externa encontrada dentro do LinkedIn."
                );

                var linkCandidatura =
                    await botaoExterno
                        .First
                        .GetAttributeAsync("href");

                if (string.IsNullOrWhiteSpace(linkCandidatura))
                {
                    Console.WriteLine(
                        "Link da candidatura externa não encontrado."
                    );

                    return "LINK_EXTERNO_NAO_ENCONTRADO";
                }

                Console.WriteLine(
                    $"Link recebido do LinkedIn: {linkCandidatura}"
                );

                // ========================================================
                // OBTER LINK REAL
                // ========================================================

                var linkReal =
                    ObterLinkRealCandidatura(linkCandidatura);

                Console.WriteLine(
                    $"Link real da candidatura: {linkReal}"
                );

                // ========================================================
                // ABRIR SITE EXTERNO
                // ========================================================

                var novaPagina =
                    await _context!.NewPageAsync();

                try
                {
                    await novaPagina.GotoAsync(
                        linkReal,
                        new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.Commit,
                            Timeout = 30000
                        });
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(
                        "O site externo demorou mais que o limite para carregar."
                    );

                    Console.WriteLine(
                        "A página pode ter sido aberta mesmo assim."
                    );
                }

                await novaPagina.WaitForTimeoutAsync(7000);

                Console.WriteLine(
                    $"Site externo aberto: {novaPagina.Url}"
                );

                if (novaPagina.IsClosed)
                {
                    Console.WriteLine(
                        "A página externa foi fechada."
                    );

                    return "SITE_EXTERNO_FECHADO";
                }

                // ========================================================
                // GUPY
                // ========================================================

                if (novaPagina.Url.Contains(
                        "gupy.io",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        "Site Gupy detectado."
                    );

                    if (await PrecisaLoginSiteExternoAsync(novaPagina))
                    {
                        Console.WriteLine(
                            "LOGIN NECESSÁRIO NO SITE DA GUPY."
                        );

                        return "LOGIN_SITE_EXTERNO";
                    }

                    var botaoGupy =
                        novaPagina.Locator(
                            "a[data-testid='apply-link']"
                        );

                    var quantidadeGupy =
                        await botaoGupy.CountAsync();

                    Console.WriteLine(
                        $"Botões de candidatura da Gupy encontrados: {quantidadeGupy}"
                    );

                    if (quantidadeGupy > 0)
                    {
                        Console.WriteLine(
                            "Botão 'Candidatar-se' da Gupy encontrado."
                        );

                        await botaoGupy
                            .First
                            .ScrollIntoViewIfNeededAsync();

                        await botaoGupy
                            .First
                            .ClickAsync();

                        Console.WriteLine(
                            "Botão 'Candidatar-se' da Gupy clicado."
                        );

                        await novaPagina.WaitForTimeoutAsync(5000);

                        Console.WriteLine(
                            $"Página após clicar na Gupy: {novaPagina.Url}"
                        );

                        if (await PrecisaLoginSiteExternoAsync(
                                novaPagina))
                        {
                            Console.WriteLine(
                                "LOGIN NECESSÁRIO APÓS CLICAR EM CANDIDATAR-SE NA GUPY."
                            );

                            return "LOGIN_SITE_EXTERNO";
                        }

                        return "CANDIDATURA_GUPY_ABERTA";
                    }

                    Console.WriteLine(
                        "Botão 'Candidatar-se' da Gupy não encontrado."
                    );

                    return "CANDIDATURA_GUPY_NAO_ENCONTRADA";
                }

                // ========================================================
                // VERIFICAR LOGIN EXTERNO
                // ========================================================

                if (await PrecisaLoginSiteExternoAsync(novaPagina))
                {
                    Console.WriteLine(
                        "LOGIN NECESSÁRIO NO SITE DA EMPRESA."
                    );

                    return "LOGIN_SITE_EXTERNO";
                }

                Console.WriteLine(
                    "Site externo pronto para iniciar candidatura."
                );

                return "CANDIDATURA_EXTERNA_ABERTA";
            }

            // ============================================================
            // CANDIDATURA DIRETA PELO LINKEDIN
            // ============================================================

            Console.WriteLine(
                "Nenhuma candidatura externa encontrada."
            );

            Console.WriteLine(
                "Verificando se a candidatura simplificada do LinkedIn foi aberta..."
            );

            // ============================================================
            // AGUARDAR ABERTURA DO EASY APPLY
            // ============================================================

            await page.WaitForTimeoutAsync(2000);

            bool candidaturaSimplificadaEncontrada = false;

            // ============================================================
            // VERIFICAR MODAIS
            // ============================================================

            var seletoresModal =
                new[]
                {
            ".jobs-easy-apply-modal",
            ".jobs-easy-apply-content",
            "[data-test-modal]",
            "[role='dialog']",
            ".artdeco-modal",
            ".artdeco-modal-overlay"
                };

            foreach (var seletor in seletoresModal)
            {
                try
                {
                    var elemento =
                        page.Locator(seletor);

                    var quantidade =
                        await elemento.CountAsync();

                    Console.WriteLine(
                        $"Seletor '{seletor}': {quantidade}"
                    );

                    if (quantidade > 0)
                    {
                        for (int i = 0; i < quantidade; i++)
                        {
                            try
                            {
                                if (await elemento.Nth(i).IsVisibleAsync())
                                {
                                    Console.WriteLine(
                                        $"Modal visível encontrado: {seletor}"
                                    );

                                    candidaturaSimplificadaEncontrada =
                                        true;

                                    break;
                                }
                            }
                            catch
                            {
                                // Continua.
                            }
                        }
                    }

                    if (candidaturaSimplificadaEncontrada)
                        break;
                }
                catch
                {
                    // Continua.
                }
            }

            // ============================================================
            // VERIFICAR TEXTOS DA CANDIDATURA SIMPLIFICADA
            // ============================================================

            if (!candidaturaSimplificadaEncontrada)
            {
                try
                {
                    var textosCandidatura =
                        new[]
                        {
                    "Candidatura simplificada",
                    "Candidatura Simplificada",
                    "Easy Apply",
                    "Enviar candidatura",
                    "Próximo",
                    "Revisar",
                    "Informações de contato",
                    "Experiência profissional",
                    "Currículo"
                        };

                    foreach (var texto in textosCandidatura)
                    {
                        try
                        {
                            var elementoTexto =
                                page.GetByText(
                                    texto,
                                    new PageGetByTextOptions
                                    {
                                        Exact = false
                                    });

                            var quantidade =
                                await elementoTexto.CountAsync();

                            Console.WriteLine(
                                $"Texto '{texto}' encontrado: {quantidade}"
                            );

                            for (int i = 0; i < quantidade; i++)
                            {
                                try
                                {
                                    if (await elementoTexto.Nth(i).IsVisibleAsync())
                                    {
                                        Console.WriteLine(
                                            $"Candidatura simplificada identificada pelo texto: {texto}"
                                        );

                                        candidaturaSimplificadaEncontrada =
                                            true;

                                        break;
                                    }
                                }
                                catch
                                {
                                    // Continua.
                                }
                            }

                            if (candidaturaSimplificadaEncontrada)
                                break;
                        }
                        catch
                        {
                            // Continua.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro ao verificar textos da candidatura: {ex.Message}"
                    );
                }
            }

            // ============================================================
            // VERIFICAR BOTÕES CARACTERÍSTICOS DO EASY APPLY
            // ============================================================

            if (!candidaturaSimplificadaEncontrada)
            {
                try
                {
                    var botoesEasyApply =
                        page.Locator(
                            "button:has-text('Próximo'), " +
                            "button:has-text('Revisar'), " +
                            "button:has-text('Enviar candidatura'), " +
                            "button:has-text('Avançar')"
                        );

                    var quantidade =
                        await botoesEasyApply.CountAsync();

                    Console.WriteLine(
                        $"Botões característicos do Easy Apply encontrados: {quantidade}"
                    );

                    for (int i = 0; i < quantidade; i++)
                    {
                        try
                        {
                            if (await botoesEasyApply.Nth(i).IsVisibleAsync())
                            {
                                candidaturaSimplificadaEncontrada =
                                    true;

                                Console.WriteLine(
                                    "Candidatura simplificada identificada através de botão do formulário."
                                );

                                break;
                            }
                        }
                        catch
                        {
                            // Continua.
                        }
                    }
                }
                catch
                {
                    // Continua.
                }
            }

            // ============================================================
            // VERIFICAR RESULTADO
            // ============================================================

            if (candidaturaSimplificadaEncontrada)
            {
                Console.WriteLine(
                    "=========================================="
                );

                Console.WriteLine(
                    "CANDIDATURA SIMPLIFICADA DO LINKEDIN ABERTA!"
                );

                Console.WriteLine(
                    "=========================================="
                );

                // ========================================================
                // ETAPA 1 - PREENCHER INFORMAÇÕES DE CONTATO
                // ========================================================

                try
                {
                    Console.WriteLine(
                        "Preparando primeira etapa da candidatura..."
                    );

                    await page.WaitForTimeoutAsync(1500);

                    // ----------------------------------------------------
                    // GARANTIR PAÍS
                    // ----------------------------------------------------
                    try
                    {
                        var selects = page.Locator("select");

                        var quantidadeSelects =
                            await selects.CountAsync();

                        Console.WriteLine(
                            $"Selects encontrados: {quantidadeSelects}"
                        );

                        var paisUsuario =
                            usuario.Pais?
                                .Trim()
                                .ToLowerInvariant();

                        if (string.IsNullOrWhiteSpace(paisUsuario))
                        {
                            Console.WriteLine(
                                "País do usuário não informado."
                            );
                        }
                        else
                        {
                            Console.WriteLine(
                                $"País do usuário: {paisUsuario}"
                            );

                            for (int i = 0; i < quantidadeSelects; i++)
                            {
                                try
                                {
                                    var select =
                                        selects.Nth(i);

                                    if (!await select.IsVisibleAsync())
                                        continue;

                                    var possuiPais =
                                        await select.Locator(
                                            $"option[value='{paisUsuario}']"
                                        ).CountAsync();

                                    if (possuiPais > 0)
                                    {
                                        await select.SelectOptionAsync(
                                            paisUsuario
                                        );

                                        Console.WriteLine(
                                            $"País definido como: {paisUsuario}"
                                        );

                                        break;
                                    }
                                }
                                catch
                                {
                                    // Continua procurando o select do país.
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Não foi possível definir o país: {ex.Message}"
                        );
                    }

                    // ----------------------------------------------------    
                    // PREENCHER TELEFONE
                    // ----------------------------------------------------
                    try
                    {
                        var telefone =
                            page.Locator(
                                "input[type='tel']"
                            );

                        var quantidadeTelefone =
                            await telefone.CountAsync();

                        Console.WriteLine(
                            $"Campos de telefone encontrados: {quantidadeTelefone}"
                        );

                        if (quantidadeTelefone > 0)
                        {
                            var campoTelefone =
                                telefone.First;

                            if (await campoTelefone.IsVisibleAsync())
                            {
                                await campoTelefone
                                    .FillAsync(usuario.Telefone!);

                                Console.WriteLine(
                                    $"Telefone preenchido com os dados do usuário: {usuario.Telefone}"
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Erro ao preencher telefone: {ex.Message}"
                        );
                    }

                    // ----------------------------------------------------
                    // E-MAIL
                    // ----------------------------------------------------

                    try
                    {
                        var camposEmail =
                            page.Locator("input[type='email']");

                        var quantidadeEmail =
                            await camposEmail.CountAsync();

                        Console.WriteLine(
                            $"Campos de e-mail encontrados: {quantidadeEmail}"
                        );

                        if (quantidadeEmail > 0)
                        {
                            var campoEmail =
                                camposEmail.First;

                            if (await campoEmail.IsVisibleAsync())
                            {
                                await campoEmail.FillAsync(
                                    usuario.Email!
                                );

                                Console.WriteLine(
                                    $"E-mail preenchido com os dados do usuário: {usuario.Email}"
                                );
                            }
                        }
                        else
                        {
                            Console.WriteLine(
                                "Campo de e-mail não encontrado. O LinkedIn pode ter deixado o e-mail selecionado automaticamente."
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Erro ao preencher e-mail: {ex.Message}"
                        );
                    }

                    // ====================================================
                    // INSPEÇÃO ANTES DE AVANÇAR
                    // ====================================================

                    Console.WriteLine(
                        "Dados da primeira etapa preenchidos."
                    );

                    // ====================================================
                    // LOCALIZAR BOTÃO AVANÇAR
                    // ====================================================

                    var botaoAvancar =
                        page.GetByRole(
                            AriaRole.Button,
                            new PageGetByRoleOptions
                            {
                                Name = "Avançar",
                                Exact = true
                            });

                    var quantidadeAvancar =
                        await botaoAvancar.CountAsync();

                    Console.WriteLine(
                        $"Botões 'Avançar' encontrados: {quantidadeAvancar}"
                    );

                    if (quantidadeAvancar == 0)
                    {
                        Console.WriteLine(
                            "Botão 'Avançar' não encontrado."
                        );

                        await InspecionarCandidaturaAsync(page);

                        return "CANDIDATURA_LINKEDIN_ETAPA_1_NAO_AVANCOU";
                    }

                    // ----------------------------------------------------
                    // CLICAR UMA ÚNICA VEZ
                    // ----------------------------------------------------

                    var avancar =
                        botaoAvancar.First;

                    if (!await avancar.IsVisibleAsync())
                    {
                        Console.WriteLine(
                            "Botão 'Avançar' não está visível."
                        );

                        await InspecionarCandidaturaAsync(page);

                        return "CANDIDATURA_LINKEDIN_ETAPA_1_NAO_AVANCOU";
                    }

                    await avancar.ScrollIntoViewIfNeededAsync();

                    await page.WaitForTimeoutAsync(500);

                    Console.WriteLine(
                        "Clicando em 'Avançar'..."
                    );

                    await avancar.ClickAsync();

                    Console.WriteLine(
                        "'Avançar' clicado com sucesso."
                    );

                    // ====================================================
                    // AGUARDAR PÁGINA 2
                    // ====================================================

                    await page.WaitForTimeoutAsync(2500);

                    Console.WriteLine(
                        "=========================================="
                    );

                    Console.WriteLine(
                        "AVANÇOU PARA A PRÓXIMA ETAPA."
                    );

                    Console.WriteLine(
                        $"URL ATUAL: {page.Url}"
                    );

                    Console.WriteLine(
                        "=========================================="
                    );

                    // ====================================================
                    // INSPECIONAR PÁGINA 2
                    // ====================================================

                    Console.WriteLine(
                        "Iniciando inspeção da próxima etapa..."
                    );

                    await InspecionarCandidaturaAsync(page);

                    // ========================================================
                    // ETAPA 2 - SELECIONAR CURRÍCULO
                    // ========================================================

                    Console.WriteLine(
                        "=========================================="
                    );

                    Console.WriteLine(
                        "INICIANDO ETAPA 2 - SELEÇÃO DO CURRÍCULO"
                    );

                    Console.WriteLine(
                        "=========================================="
                    );

                    await page.WaitForTimeoutAsync(1500);

                    // ========================================================
                    // VERIFICAR CURRÍCULO SELECIONADO
                    // ========================================================

                    bool curriculoSelecionado = false;

                    try
                    {
                        // ----------------------------------------------------
                        // LOCALIZAR OS RADIOS DOS CURRÍCULOS
                        // ----------------------------------------------------

                        var radiosCurriculo =
                            page.Locator(
                                "input[type='radio']"
                            );

                        var quantidadeRadios =
                            await radiosCurriculo.CountAsync();

                        Console.WriteLine(
                            $"Radios encontrados na etapa de currículo: {quantidadeRadios}"
                        );

                        // ----------------------------------------------------
                        // LOCALIZAR RADIOS QUE POSSUEM ARIA-LABEL
                        // ----------------------------------------------------

                        var radiosComCurriculo =
                            page.Locator(
                                "input[type='radio'][aria-label]"
                            );

                        var quantidadeCurriculos =
                            await radiosComCurriculo.CountAsync();

                        Console.WriteLine(
                            $"Currículos encontrados: {quantidadeCurriculos}"
                        );

                        if (quantidadeCurriculos == 0)
                        {
                            Console.WriteLine(
                                "Nenhum currículo foi encontrado."
                            );
                        }
                        else
                        {
                            // ------------------------------------------------
                            // LISTAR OS CURRÍCULOS
                            // ------------------------------------------------

                            for (int i = 0; i < quantidadeCurriculos; i++)
                            {
                                try
                                {
                                    var radio =
                                        radiosComCurriculo.Nth(i);

                                    var nome =
                                        await radio.GetAttributeAsync(
                                            "aria-label"
                                        );

                                    var marcado =
                                        await radio.IsCheckedAsync();

                                    Console.WriteLine(
                                        $"[{i}] {nome} | Selecionado: {marcado}"
                                    );

                                    if (marcado)
                                    {
                                        curriculoSelecionado = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(
                                        $"Erro ao verificar currículo [{i}]: {ex.Message}"
                                    );
                                }
                            }

                            // ------------------------------------------------
                            // CASO NENHUM RADIO INFORME CHECKED
                            //
                            // O LinkedIn pode estar usando um componente visual
                            // e o radio real pode permanecer oculto.
                            //
                            // Nesse caso, consideramos o currículo mais recente
                            // apresentado pelo LinkedIn como padrão.
                            // ------------------------------------------------

                            if (!curriculoSelecionado)
                            {
                                Console.WriteLine(
                                    "Nenhum radio informou IsChecked=true."
                                );

                                Console.WriteLine(
                                    "O LinkedIn pode estar controlando a seleção através do componente visual."
                                );

                                // ------------------------------------------------
                                // CONSIDERAR O PRIMEIRO CURRÍCULO COMO PADRÃO
                                // ------------------------------------------------

                                var primeiroCurriculo =
                                    radiosComCurriculo.First;

                                var nomePrimeiroCurriculo =
                                    await primeiroCurriculo.GetAttributeAsync(
                                        "aria-label"
                                    );

                                Console.WriteLine(
                                    $"Currículo padrão apresentado pelo LinkedIn: {nomePrimeiroCurriculo}"
                                );

                                // ------------------------------------------------
                                // NÃO CLICAR NO RADIO
                                // ------------------------------------------------

                                curriculoSelecionado = true;

                                Console.WriteLine(
                                    "Currículo considerado selecionado pelo estado padrão do LinkedIn."
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Erro ao verificar currículo: {ex.Message}"
                        );
                    }

                    // ========================================================
                    // VERIFICAR RESULTADO DA SELEÇÃO
                    // ========================================================

                    if (!curriculoSelecionado)
                    {
                        Console.WriteLine(
                            "=========================================="
                        );

                        Console.WriteLine(
                            "NÃO FOI POSSÍVEL IDENTIFICAR UM CURRÍCULO."
                        );

                        Console.WriteLine(
                            "A candidatura NÃO será avançada."
                        );

                        Console.WriteLine(
                            "=========================================="
                        );

                        await InspecionarCandidaturaAsync(page);

                        return "CANDIDATURA_LINKEDIN_CURRICULO_NAO_SELECIONADO";
                    }

                    Console.WriteLine(
                        "=========================================="
                    );

                    Console.WriteLine(
                        "CURRÍCULO PRONTO PARA A CANDIDATURA."
                    );

                    Console.WriteLine(
                        "O LinkedIn apresentou o currículo mais recente como padrão."
                    );

                    Console.WriteLine(
                        "=========================================="
                    );

                    // ========================================================
                    // LOCALIZAR BOTÃO DA ETAPA 2
                    // ========================================================

                    try
                    {
                        // ----------------------------------------------------
                        // PROCURAR "AVANÇAR"
                        // ----------------------------------------------------

                        var botaoAvancarEtapa2 =
                            page.GetByRole(
                                AriaRole.Button,
                                new PageGetByRoleOptions
                                {
                                    Name = "Avançar",
                                    Exact = true
                                });

                        var quantidadeAvancarEtapa2 =
                            await botaoAvancarEtapa2.CountAsync();

                        Console.WriteLine(
                            $"Botões 'Avançar' encontrados: {quantidadeAvancarEtapa2}"
                        );

                        // ====================================================
                        // SE ENCONTRAR "AVANÇAR"
                        //
                        // NÃO CLICAR.
                        //
                        // A próxima etapa pode conter perguntas específicas
                        // da vaga que ainda não conhecemos.
                        // ====================================================

                        if (quantidadeAvancarEtapa2 > 0)
                        {
                            var avancarEtapa2 =
                                botaoAvancarEtapa2.First;

                            if (await avancarEtapa2.IsVisibleAsync())
                            {
                                Console.WriteLine(
                                    "=========================================="
                                );

                                Console.WriteLine(
                                    "BOTÃO 'AVANÇAR' ENCONTRADO."
                                );

                                Console.WriteLine(
                                    "A próxima etapa pode conter perguntas específicas da vaga."
                                );

                                Console.WriteLine(
                                    "A AUTOMAÇÃO NÃO VAI CLICAR AUTOMATICAMENTE."
                                );

                                Console.WriteLine(
                                    "CONTROLE DEVOLVIDO AO USUÁRIO."
                                );

                                Console.WriteLine(
                                    "=========================================="
                                );

                                // ------------------------------------------------
                                // INSPECIONAR O ESTADO ATUAL
                                // ------------------------------------------------

                                await InspecionarCandidaturaAsync(page);

                                return "CANDIDATURA_LINKEDIN_CONTROLE_USUARIO";
                            }

                            Console.WriteLine(
                                "Botão 'Avançar' encontrado, mas não está visível."
                            );
                        }

                        // ====================================================
                        // PROCURAR "AVALIAR"
                        // ====================================================

                        var botaoAvaliarEtapa2 =
                            page.GetByRole(
                                AriaRole.Button,
                                new PageGetByRoleOptions
                                {
                                    Name = "Avaliar",
                                    Exact = true
                                });

                        var quantidadeAvaliarEtapa2 =
                            await botaoAvaliarEtapa2.CountAsync();

                        Console.WriteLine(
                            $"Botões 'Avaliar' encontrados: {quantidadeAvaliarEtapa2}"
                        );

                        // ====================================================
                        // SE ENCONTRAR "AVALIAR"
                        //
                        // CLICAR AUTOMATICAMENTE.
                        // ====================================================

                        if (quantidadeAvaliarEtapa2 > 0)
                        {
                            var avaliarEtapa2 =
                                botaoAvaliarEtapa2.First;

                            if (await avaliarEtapa2.IsVisibleAsync())
                            {
                                Console.WriteLine(
                                    "=========================================="
                                );

                                Console.WriteLine(
                                    "BOTÃO 'AVALIAR' ENCONTRADO."
                                );

                                Console.WriteLine(
                                    "O botão está disponível na etapa 2."
                                );

                                Console.WriteLine(
                                    "CLICANDO AUTOMATICAMENTE EM 'AVALIAR'..."
                                );

                                Console.WriteLine(
                                    "=========================================="
                                );

                                // ------------------------------------------------
                                // GARANTIR QUE O BOTÃO ESTÁ NA ÁREA VISÍVEL
                                // ------------------------------------------------

                                await avaliarEtapa2.ScrollIntoViewIfNeededAsync();

                                await page.WaitForTimeoutAsync(500);

                                // ------------------------------------------------
                                // CLICAR
                                // ------------------------------------------------

                                await avaliarEtapa2.ClickAsync();

                                Console.WriteLine(
                                    "'Avaliar' clicado com sucesso."
                                );

                                // ------------------------------------------------
                                // AGUARDAR O LINKEDIN PROCESSAR A PRÓXIMA ETAPA
                                // ------------------------------------------------

                                await page.WaitForTimeoutAsync(2500);

                                Console.WriteLine(
                                    "=========================================="
                                );

                                Console.WriteLine(
                                    "AVANÇOU PARA A PRÓXIMA ETAPA."
                                );

                                Console.WriteLine(
                                    $"URL ATUAL: {page.Url}"
                                );

                                Console.WriteLine(
                                    "=========================================="
                                );

                                // ------------------------------------------------
                                // INSPECIONAR A NOVA ETAPA
                                // ------------------------------------------------

                                await InspecionarCandidaturaAsync(page);

                                return "CANDIDATURA_LINKEDIN_ETAPA_SEGUINTE";
                            }

                            Console.WriteLine(
                                "Botão 'Avaliar' encontrado, mas não está visível."
                            );

                            await InspecionarCandidaturaAsync(page);

                            return "CANDIDATURA_LINKEDIN_AVALIAR_NAO_VISIVEL";
                        }

                        // ====================================================
                        // NENHUM BOTÃO CONHECIDO FOI ENCONTRADO
                        // ====================================================

                        Console.WriteLine(
                            "=========================================="
                        );

                        Console.WriteLine(
                            "NENHUM BOTÃO CONHECIDO FOI ENCONTRADO NA ETAPA 2."
                        );

                        Console.WriteLine(
                            "Botões esperados: 'Avançar' ou 'Avaliar'."
                        );

                        Console.WriteLine(
                            "=========================================="
                        );

                        await InspecionarCandidaturaAsync(page);

                        return "CANDIDATURA_LINKEDIN_ETAPA_2_BOTAO_NAO_ENCONTRADO";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "=========================================="
                        );

                        Console.WriteLine(
                            "ERRO AO PROCESSAR A ETAPA 2 DA CANDIDATURA"
                        );

                        Console.WriteLine(
                            ex.Message
                        );

                        Console.WriteLine(
                            "=========================================="
                        );

                        return "ERRO_CANDIDATURA_LINKEDIN_ETAPA_2";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "=========================================="
                    );

                    Console.WriteLine(
                        "ERRO AO PREENCHER A PRIMEIRA ETAPA"
                    );

                    Console.WriteLine(
                        ex.Message
                    );

                    Console.WriteLine(
                        "=========================================="
                    );

                    return "ERRO_CANDIDATURA_LINKEDIN";
                }
            }

            // ============================================================
            // ÚLTIMA TENTATIVA:
            // VERIFICAR SE A PÁGINA MUDOU PARA UMA TELA DE CANDIDATURA
            // ============================================================

            try
            {
                var dialogs =
                    page.Locator("[role='dialog']");

                var quantidadeDialogs =
                    await dialogs.CountAsync();

                Console.WriteLine(
                    $"Dialogs encontrados na página: {quantidadeDialogs}"
                );

                if (quantidadeDialogs > 0)
                {
                    for (int i = 0; i < quantidadeDialogs; i++)
                    {
                        try
                        {
                            if (!await dialogs.Nth(i).IsVisibleAsync())
                                continue;

                            var textoDialog =
                                await dialogs.Nth(i).InnerTextAsync();

                            Console.WriteLine(
                                "=========================================="
                            );

                            Console.WriteLine(
                                "TEXTO DO DIALOG ENCONTRADO:"
                            );

                            Console.WriteLine(
                                textoDialog
                            );

                            Console.WriteLine(
                                "=========================================="
                            );

                            if (
                                textoDialog.Contains(
                                    "candidatura",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                textoDialog.Contains(
                                    "candidatar",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                textoDialog.Contains(
                                    "próximo",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                textoDialog.Contains(
                                    "enviar",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                textoDialog.Contains(
                                    "currículo",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                textoDialog.Contains(
                                    "curriculo",
                                    StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                Console.WriteLine(
                                    "Dialog identificado como formulário de candidatura."
                                );

                                return "CANDIDATURA_LINKEDIN_ABERTA";
                            }
                        }
                        catch
                        {
                            // Continua.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao analisar dialogs: {ex.Message}"
                );
            }

            // ============================================================
            // NÃO FOI POSSÍVEL IDENTIFICAR
            // ============================================================

            Console.WriteLine(
                "================================================"
            );

            Console.WriteLine(
                "O botão de candidatura foi clicado, porém o próximo passo não foi identificado."
            );

            Console.WriteLine(
                $"URL ATUAL: {page.Url}"
            );

            Console.WriteLine(
                "================================================"
            );

            return "CANDIDATURA_NAO_ENCONTRADA";
        }

        // ================================================================
        // INSPEÇÃO DA CANDIDATURA
        // ================================================================
        private async Task InspecionarCandidaturaAsync(IPage page)
        {
            string pastaLogs = Path.Combine(AppContext.BaseDirectory, "logs");

            Directory.CreateDirectory(pastaLogs);

            string nomeArquivo =
                $"candidatura-linkedin-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";

            string caminhoArquivo = Path.Combine(pastaLogs, nomeArquivo);

            using StreamWriter log = new StreamWriter(
                caminhoArquivo,
                append: false,
                encoding: System.Text.Encoding.UTF8
            );

            // =========================================================
            // ESCREVE NO ARQUIVO
            // =========================================================

            async Task LogArquivoAsync(string texto = "")
            {
                await log.WriteLineAsync(texto);
            }

            // =========================================================
            // ESCREVE NO TERMINAL E NO ARQUIVO
            // =========================================================

            async Task LogTerminalEArquivoAsync(string texto = "")
            {
                Console.WriteLine(texto);
                await log.WriteLineAsync(texto);
            }

            // =========================================================
            // ARQUIVO SOMENTE
            // =========================================================

            async Task LogSomenteArquivoAsync(string texto = "")
            {
                await log.WriteLineAsync(texto);
            }

            try
            {
                // =====================================================
                // CABEÇALHO
                // =====================================================

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "============================================================"
                );
                await LogTerminalEArquivoAsync(
                    "        INSPEÇÃO DA CANDIDATURA LINKEDIN"
                );
                await LogTerminalEArquivoAsync(
                    "============================================================"
                );

                await LogTerminalEArquivoAsync(
                    $"DATA/HORA: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
                );

                await LogTerminalEArquivoAsync(
                    $"URL: {page.Url}"
                );

                string titulo = await page.TitleAsync();

                await LogTerminalEArquivoAsync(
                    $"TÍTULO: {titulo}"
                );

                // =====================================================
                // TEXTO COMPLETO DA PÁGINA
                // ARQUIVO
                // =====================================================

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ TEXTO COMPLETO DA PÁGINA ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                string textoPagina = await page
                    .Locator("body")
                    .InnerTextAsync();

                await LogSomenteArquivoAsync(textoPagina);

                // =====================================================
                // DIALOG
                // =====================================================

                var dialogs = page.GetByRole(AriaRole.Dialog);

                int quantidadeDialogs = await dialogs.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ DIALOGS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE DIALOGS: {quantidadeDialogs}"
                );

                string textoDialogPrincipal = "";

                if (quantidadeDialogs > 0)
                {
                    var dialog = dialogs.Nth(0);

                    textoDialogPrincipal = await dialog.InnerTextAsync();

                    await LogSomenteArquivoAsync();
                    await LogSomenteArquivoAsync(
                        "----- DIALOG [0] -----"
                    );

                    await LogSomenteArquivoAsync(
                        textoDialogPrincipal
                    );
                }

                // =====================================================
                // TERMINAL - RESUMO DO DIALOG
                // =====================================================

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "---------------- DIALOG ----------------"
                );

                if (!string.IsNullOrWhiteSpace(textoDialogPrincipal))
                {
                    string[] linhasDialog =
                        textoDialogPrincipal
                            .Split(
                                new[] { '\r', '\n' },
                                StringSplitOptions.RemoveEmptyEntries
                            )
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Take(20)
                            .ToArray();

                    foreach (string linha in linhasDialog)
                    {
                        await LogTerminalEArquivoAsync(linha);
                    }
                }
                else
                {
                    await LogTerminalEArquivoAsync(
                        "Nenhum dialog encontrado."
                    );
                }

                // =====================================================
                // INPUTS
                // =====================================================

                var inputs = page.Locator("input");

                int quantidadeInputs = await inputs.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ INPUTS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE INPUTS: {quantidadeInputs}"
                );

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "---------------- INPUTS ----------------"
                );

                if (quantidadeInputs == 0)
                {
                    await LogTerminalEArquivoAsync(
                        "Nenhum input encontrado."
                    );
                }

                for (int i = 0; i < quantidadeInputs; i++)
                {
                    var input = inputs.Nth(i);

                    try
                    {
                        string type =
                            await input.GetAttributeAsync("type") ?? "";

                        string name =
                            await input.GetAttributeAsync("name") ?? "";

                        string id =
                            await input.GetAttributeAsync("id") ?? "";

                        string value =
                            await input.InputValueAsync();

                        string placeholder =
                            await input.GetAttributeAsync("placeholder") ?? "";

                        string ariaLabel =
                            await input.GetAttributeAsync("aria-label") ?? "";

                        string autocomplete =
                            await input.GetAttributeAsync("autocomplete") ?? "";

                        string required =
                            await input.GetAttributeAsync("required") ?? "";

                        bool visible =
                            await input.IsVisibleAsync();

                        // ---------------------------------------------
                        // ARQUIVO COMPLETO
                        // ---------------------------------------------

                        await LogSomenteArquivoAsync();
                        await LogSomenteArquivoAsync(
                            $"INPUT [{i}]"
                        );
                        await LogSomenteArquivoAsync(
                            $"  type: {type}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  name: {name}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  id: {id}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  value: {value}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  placeholder: {placeholder}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  aria-label: {ariaLabel}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  autocomplete: {autocomplete}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  required: {required}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  visible: {visible}"
                        );

                        // ---------------------------------------------
                        // TERMINAL RESUMIDO
                        // ---------------------------------------------

                        string valorTerminal =
                            string.IsNullOrWhiteSpace(value)
                                ? "(vazio)"
                                : value;

                        string identificacao =
                            !string.IsNullOrWhiteSpace(name)
                                ? $"name={name}"
                                : !string.IsNullOrWhiteSpace(id)
                                    ? $"id={id}"
                                    : !string.IsNullOrWhiteSpace(ariaLabel)
                                        ? $"aria-label={ariaLabel}"
                                        : "";

                        await LogTerminalEArquivoAsync(
                            $"[{i}] type={type} | {identificacao} | value={valorTerminal} | visible={visible}"
                        );
                    }
                    catch (Exception ex)
                    {
                        await LogSomenteArquivoAsync(
                            $"INPUT [{i}] ERRO: {ex}"
                        );

                        await LogTerminalEArquivoAsync(
                            $"[{i}] ERRO ao inspecionar input."
                        );
                    }
                }

                // =====================================================
                // TEXTAREAS
                // =====================================================

                var textareas = page.Locator("textarea");

                int quantidadeTextareas =
                    await textareas.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ TEXTAREAS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE TEXTAREAS: {quantidadeTextareas}"
                );

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "---------------- TEXTAREAS ----------------"
                );

                if (quantidadeTextareas == 0)
                {
                    await LogTerminalEArquivoAsync(
                        "Nenhuma textarea encontrada."
                    );
                }

                for (int i = 0; i < quantidadeTextareas; i++)
                {
                    var textarea = textareas.Nth(i);

                    try
                    {
                        string name =
                            await textarea.GetAttributeAsync("name") ?? "";

                        string id =
                            await textarea.GetAttributeAsync("id") ?? "";

                        string placeholder =
                            await textarea.GetAttributeAsync("placeholder") ?? "";

                        string ariaLabel =
                            await textarea.GetAttributeAsync("aria-label") ?? "";

                        string value =
                            await textarea.InputValueAsync();

                        bool visible =
                            await textarea.IsVisibleAsync();

                        // ARQUIVO
                        await LogSomenteArquivoAsync();
                        await LogSomenteArquivoAsync(
                            $"TEXTAREA [{i}]"
                        );
                        await LogSomenteArquivoAsync(
                            $"  name: {name}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  id: {id}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  value: {value}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  placeholder: {placeholder}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  aria-label: {ariaLabel}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  visible: {visible}"
                        );

                        // TERMINAL
                        string identificacao =
                            !string.IsNullOrWhiteSpace(name)
                                ? $"name={name}"
                                : !string.IsNullOrWhiteSpace(id)
                                    ? $"id={id}"
                                    : !string.IsNullOrWhiteSpace(ariaLabel)
                                        ? $"aria-label={ariaLabel}"
                                        : "";

                        string valorTerminal =
                            string.IsNullOrWhiteSpace(value)
                                ? "(vazio)"
                                : value;

                        await LogTerminalEArquivoAsync(
                            $"[{i}] {identificacao} | value={valorTerminal} | visible={visible}"
                        );
                    }
                    catch (Exception ex)
                    {
                        await LogSomenteArquivoAsync(
                            $"TEXTAREA [{i}] ERRO: {ex}"
                        );

                        await LogTerminalEArquivoAsync(
                            $"[{i}] ERRO ao inspecionar textarea."
                        );
                    }
                }

                // =====================================================
                // SELECTS
                // =====================================================

                var selects = page.Locator("select");

                int quantidadeSelects =
                    await selects.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ SELECTS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE SELECTS: {quantidadeSelects}"
                );

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "---------------- SELECTS ----------------"
                );

                if (quantidadeSelects == 0)
                {
                    await LogTerminalEArquivoAsync(
                        "Nenhum select encontrado."
                    );
                }

                for (int i = 0; i < quantidadeSelects; i++)
                {
                    var select = selects.Nth(i);

                    try
                    {
                        string name =
                            await select.GetAttributeAsync("name") ?? "";

                        string id =
                            await select.GetAttributeAsync("id") ?? "";

                        string ariaLabel =
                            await select.GetAttributeAsync("aria-label") ?? "";

                        string valorAtual =
                            await select.InputValueAsync();

                        bool visible =
                            await select.IsVisibleAsync();

                        // ARQUIVO
                        await LogSomenteArquivoAsync();
                        await LogSomenteArquivoAsync(
                            $"SELECT [{i}]"
                        );
                        await LogSomenteArquivoAsync(
                            $"  name: {name}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  id: {id}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  aria-label: {ariaLabel}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  valor atual: {valorAtual}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  visible: {visible}"
                        );

                        var options =
                            select.Locator("option");

                        int quantidadeOptions =
                            await options.CountAsync();

                        await LogSomenteArquivoAsync(
                            $"  OPTIONS: {quantidadeOptions}"
                        );

                        for (int j = 0; j < quantidadeOptions; j++)
                        {
                            var option = options.Nth(j);

                            string texto =
                                await option.InnerTextAsync();

                            string valor =
                                await option.GetAttributeAsync("value") ?? "";

                            await LogSomenteArquivoAsync(
                                $"    [{j}] texto=\"{texto}\" value=\"{valor}\""
                            );
                        }

                        // TERMINAL
                        string identificacao =
                            !string.IsNullOrWhiteSpace(name)
                                ? $"name={name}"
                                : !string.IsNullOrWhiteSpace(id)
                                    ? $"id={id}"
                                    : !string.IsNullOrWhiteSpace(ariaLabel)
                                        ? $"aria-label={ariaLabel}"
                                        : $"select[{i}]";

                        await LogTerminalEArquivoAsync(
                            $"[{i}] {identificacao} | valor atual={valorAtual} | options={quantidadeOptions} | visible={visible}"
                        );
                    }
                    catch (Exception ex)
                    {
                        await LogSomenteArquivoAsync(
                            $"SELECT [{i}] ERRO: {ex}"
                        );

                        await LogTerminalEArquivoAsync(
                            $"[{i}] ERRO ao inspecionar select."
                        );
                    }
                }

                // =====================================================
                // BOTÕES
                // =====================================================

                var buttons = page.Locator("button");

                int quantidadeButtons =
                    await buttons.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ BUTTONS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE BUTTONS: {quantidadeButtons}"
                );

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "---------------- BUTTONS ----------------"
                );

                int botoesVisiveis = 0;

                for (int i = 0; i < quantidadeButtons; i++)
                {
                    var button = buttons.Nth(i);

                    try
                    {
                        string texto =
                            (await button.InnerTextAsync()).Trim();

                        string type =
                            await button.GetAttributeAsync("type") ?? "";

                        string ariaLabel =
                            await button.GetAttributeAsync("aria-label") ?? "";

                        string name =
                            await button.GetAttributeAsync("name") ?? "";

                        string id =
                            await button.GetAttributeAsync("id") ?? "";

                        bool visible =
                            await button.IsVisibleAsync();

                        // ARQUIVO COMPLETO
                        await LogSomenteArquivoAsync();
                        await LogSomenteArquivoAsync(
                            $"BUTTON [{i}]"
                        );
                        await LogSomenteArquivoAsync(
                            $"  texto: {texto}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  type: {type}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  aria-label: {ariaLabel}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  name: {name}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  id: {id}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  visible: {visible}"
                        );

                        // TERMINAL
                        if (visible && !string.IsNullOrWhiteSpace(texto))
                        {
                            await LogTerminalEArquivoAsync(
                                $"[{i}] {texto} | visible={visible}"
                            );

                            botoesVisiveis++;
                        }
                        else if (
                            visible &&
                            !string.IsNullOrWhiteSpace(ariaLabel)
                        )
                        {
                            await LogTerminalEArquivoAsync(
                                $"[{i}] aria-label={ariaLabel} | visible={visible}"
                            );

                            botoesVisiveis++;
                        }
                    }
                    catch (Exception ex)
                    {
                        await LogSomenteArquivoAsync(
                            $"BUTTON [{i}] ERRO: {ex}"
                        );
                    }
                }

                if (botoesVisiveis == 0)
                {
                    await LogTerminalEArquivoAsync(
                        "Nenhum botão relevante encontrado."
                    );
                }

                // =====================================================
                // LINKS
                // =====================================================

                var links = page.Locator("a");

                int quantidadeLinks =
                    await links.CountAsync();

                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "############################################################"
                );
                await LogSomenteArquivoAsync(
                    "################ LINKS ################"
                );
                await LogSomenteArquivoAsync(
                    "############################################################"
                );

                await LogSomenteArquivoAsync(
                    $"QUANTIDADE DE LINKS: {quantidadeLinks}"
                );

                // Não poluir o terminal.
                // Links completos ficam SOMENTE no arquivo.

                for (int i = 0; i < quantidadeLinks; i++)
                {
                    var link = links.Nth(i);

                    try
                    {
                        string texto =
                            (await link.InnerTextAsync()).Trim();

                        string href =
                            await link.GetAttributeAsync("href") ?? "";

                        string ariaLabel =
                            await link.GetAttributeAsync("aria-label") ?? "";

                        bool visible =
                            await link.IsVisibleAsync();

                        await LogSomenteArquivoAsync();
                        await LogSomenteArquivoAsync(
                            $"LINK [{i}]"
                        );
                        await LogSomenteArquivoAsync(
                            $"  texto: {texto}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  href: {href}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  aria-label: {ariaLabel}"
                        );
                        await LogSomenteArquivoAsync(
                            $"  visible: {visible}"
                        );
                    }
                    catch (Exception ex)
                    {
                        await LogSomenteArquivoAsync(
                            $"LINK [{i}] ERRO: {ex}"
                        );
                    }
                }

                // =====================================================
                // RESUMO FINAL
                // =====================================================

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    "============================================================"
                );
                await LogTerminalEArquivoAsync(
                    "INSPEÇÃO FINALIZADA"
                );
                await LogTerminalEArquivoAsync(
                    "============================================================"
                );

                await LogTerminalEArquivoAsync(
                    $"Inputs encontrados: {quantidadeInputs}"
                );

                await LogTerminalEArquivoAsync(
                    $"Textareas encontradas: {quantidadeTextareas}"
                );

                await LogTerminalEArquivoAsync(
                    $"Selects encontrados: {quantidadeSelects}"
                );

                await LogTerminalEArquivoAsync(
                    $"Botões encontrados: {quantidadeButtons}"
                );

                await LogTerminalEArquivoAsync(
                    $"Links encontrados: {quantidadeLinks}"
                );

                await LogTerminalEArquivoAsync();
                await LogTerminalEArquivoAsync(
                    $"LOG COMPLETO: {caminhoArquivo}"
                );

                await LogTerminalEArquivoAsync(
                    "============================================================"
                );
            }
            catch (Exception ex)
            {
                await LogSomenteArquivoAsync();
                await LogSomenteArquivoAsync(
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
                );
                await LogSomenteArquivoAsync(
                    "ERRO DURANTE A INSPEÇÃO"
                );
                await LogSomenteArquivoAsync(
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
                );
                await LogSomenteArquivoAsync(
                    ex.ToString()
                );

                Console.WriteLine();
                Console.WriteLine("ERRO DURANTE A INSPEÇÃO.");
                Console.WriteLine($"Veja o log: {caminhoArquivo}");
            }
            finally
            {
                await log.FlushAsync();
            }
        }

        // ================================================================
        // CONVERTER LINK DO LINKEDIN PARA LINK REAL
        // ================================================================
        private string ObterLinkRealCandidatura(string link)
        {
            try
            {
                var uri = new Uri(link);

                // Se não for um link do safety/go do LinkedIn,
                // não precisamos fazer nenhuma conversão.
                if (!uri.Host.Contains("linkedin.com"))
                {
                    return link;
                }

                var query =
                    Microsoft.AspNetCore.WebUtilities
                        .QueryHelpers
                        .ParseQuery(uri.Query);

                if (query.TryGetValue("url", out var url))
                {
                    var valor = url.ToString();

                    if (!string.IsNullOrWhiteSpace(valor))
                    {
                        var linkDecodificado =
                            Uri.UnescapeDataString(valor);

                        Console.WriteLine(
                            $"URL externa encontrada: {linkDecodificado}"
                        );

                        return linkDecodificado;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao extrair link real da candidatura: {ex.Message}"
                );
            }

            // Se não conseguir extrair o endereço,
            // mantém o link original.
            return link;
        }

        // ============================================================
        // DETECTAR LOGIN LINKEDIN
        // ============================================================  
        private async Task<bool> PrecisaLoginLinkedInAsync(IPage page)
        {
            try
            {
                Console.WriteLine("========== VERIFICANDO LOGIN LINKEDIN ==========");

                string urlAtual = page.Url;

                Console.WriteLine($"URL para verificar login: {urlAtual}");

                // ============================================================
                // 1. VERIFICAR A URL
                // ============================================================

                if (urlAtual.Contains(
                        "/login",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    urlAtual.Contains(
                        "/signup",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    urlAtual.Contains(
                        "/checkpoint",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    urlAtual.Contains(
                        "authwall",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        "LinkedIn redirecionou para uma página de login/autenticação."
                    );

                    return true;
                }

                // ============================================================
                // 2. VERIFICAR ELEMENTOS DA TELA DE LOGIN
                // ============================================================

                var campoEmail =
                    page.Locator(
                        "input[name='session_key'], " +
                        "input[name='email-or-phone'], " +
                        "input[id='username']"
                    );

                var campoSenha =
                    page.Locator(
                        "input[name='session_password'], " +
                        "input[name='password'], " +
                        "input[id='password']"
                    );

                int quantidadeEmail =
                    await campoEmail.CountAsync();

                int quantidadeSenha =
                    await campoSenha.CountAsync();

                Console.WriteLine(
                    $"Campos de e-mail encontrados: {quantidadeEmail}"
                );

                Console.WriteLine(
                    $"Campos de senha encontrados: {quantidadeSenha}"
                );

                if (quantidadeEmail > 0 && quantidadeSenha > 0)
                {
                    Console.WriteLine(
                        "Tela de login do LinkedIn detectada."
                    );

                    return true;
                }

                // ============================================================
                // 3. VERIFICAR TEXTOS DA TELA DE LOGIN
                // ============================================================

                var textoEntrar =
                    page.GetByText(
                        "Entrar",
                        new PageGetByTextOptions
                        {
                            Exact = true
                        });

                var textoEmailOuTelefone =
                    page.GetByText(
                        "E-mail ou telefone",
                        new PageGetByTextOptions
                        {
                            Exact = true
                        });

                var quantidadeEntrar =
                    await textoEntrar.CountAsync();

                var quantidadeEmailTexto =
                    await textoEmailOuTelefone.CountAsync();

                Console.WriteLine(
                    $"Texto 'Entrar' encontrado: {quantidadeEntrar}"
                );

                Console.WriteLine(
                    $"Texto 'E-mail ou telefone' encontrado: {quantidadeEmailTexto}"
                );

                if (quantidadeEmailTexto > 0)
                {
                    Console.WriteLine(
                        "Página de autenticação do LinkedIn detectada."
                    );

                    return true;
                }

                // ============================================================
                // 4. VERIFICAR AUTHWALL
                // ============================================================

                var authWall =
                    page.Locator(
                        ".authwall-join-form, " +
                        ".join-form, " +
                        "#session_key, " +
                        "#session_password"
                    );

                int quantidadeAuthWall =
                    await authWall.CountAsync();

                Console.WriteLine(
                    $"Elementos de AuthWall encontrados: {quantidadeAuthWall}"
                );

                if (quantidadeAuthWall > 0)
                {
                    Console.WriteLine(
                        "AuthWall do LinkedIn detectado."
                    );

                    return true;
                }

                // ============================================================
                // 5. NÃO FOI IDENTIFICADO LOGIN
                // ============================================================

                Console.WriteLine(
                    "Não foi identificada uma tela de login do LinkedIn."
                );

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao verificar login do LinkedIn: {ex.Message}"
                );

                // Em caso de erro na verificação,
                // não vamos assumir que o usuário está logado.
                return true;
            }
        }


        // ============================================================
        // DETECTAR LOGIN SITE EXTERNO
        // ============================================================
        private async Task<bool> PrecisaLoginSiteExternoAsync(IPage page)
        {
            var url = page.Url.ToLowerInvariant();

            if (
                url.Contains("/login") ||
                url.Contains("/signin") ||
                url.Contains("/sign-in") ||
                url.Contains("/auth") ||
                url.Contains("/entrar"))
            {
                return true;
            }

            var camposSenha =
                page.Locator(
                    "input[type='password']"
                );

            if (await camposSenha.CountAsync() > 0)
            {
                return true;
            }

            var textoPagina =
                (await page.Locator("body").InnerTextAsync())
                    .ToLowerInvariant();

            var indicadores = new[]
            {
                "fazer login",
                "faça login",
                "entrar na conta",
                "login",
                "sign in",
                "signin"
            };

            return indicadores.Any(
                indicador => textoPagina.Contains(indicador)
            );
        }

        // ============================================================
        // FECHAR
        // ============================================================
        public async Task FecharAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _context = null;
            _page = null;

            _playwright?.Dispose();
            _playwright = null;
        }


        // ============================================================
        // AGUARDAR LOGIN SITE EXTERNO
        // ============================================================
        private async Task<bool> AguardarLoginSiteExternoAsync(IPage page, int tempoMaximoSegundos = 120)
        {
            Console.WriteLine(
                "Aguardando você fazer login no site externo..."
            );

            var tempoInicial = DateTime.Now;

            while (
                (DateTime.Now - tempoInicial).TotalSeconds
                < tempoMaximoSegundos)
            {
                await page.WaitForTimeoutAsync(2000);

                if (page.IsClosed)
                {
                    Console.WriteLine(
                        "A página externa foi fechada."
                    );

                    return false;
                }

                var urlAtual =
                    page.Url.ToLowerInvariant();

                // Ainda está claramente na página de login
                var aindaLogin =
                    urlAtual.Contains("/login") ||
                    urlAtual.Contains("/signin") ||
                    urlAtual.Contains("/sign-in") ||
                    urlAtual.Contains("/auth") ||
                    urlAtual.Contains("/entrar");

                if (aindaLogin)
                {
                    continue;
                }

                // Verificar se ainda existe campo de senha
                var camposSenha =
                    page.Locator(
                        "input[type='password']"
                    );

                if (await camposSenha.CountAsync() > 0)
                {
                    continue;
                }

                Console.WriteLine(
                    "Login aparentemente concluído."
                );

                Console.WriteLine(
                    $"Página após login: {page.Url}"
                );

                return true;
            }

            Console.WriteLine(
                "Tempo máximo aguardando login atingido."
            );

            return false;
        }

        // ============================================================
        // BUSCAR VAGAS
        // ============================================================
        public async Task<List<VagaLinkedIn>?> BuscarVagasAsync(string termo, string? localizacao, string? periodo)
        {
            Console.WriteLine("========== ENTROU NO BuscarVagasAsync ==========");

            var pagina = await ObterPaginaAsync();

            var vagasPorId = new Dictionary<string, VagaLinkedIn>();

            // ============================================================
            // PERÍODO
            // ============================================================

            string periodoFiltro =
                periodo?.Trim().ToLowerInvariant() switch
                {
                    "24h" => "r86400",
                    "24 horas" => "r86400",

                    "semana" => "r604800",
                    "ultima semana" => "r604800",
                    "última semana" => "r604800",
                    "7 dias" => "r604800",

                    "mes" => "r2592000",
                    "mês" => "r2592000",
                    "ultimo mes" => "r2592000",
                    "último mês" => "r2592000",
                    "30 dias" => "r2592000",

                    _ => "r86400"
                };

            const string geoIdSaoPaulo = "105871508";

            // ============================================================
            // NORMALIZAÇÃO
            // ============================================================

            string Normalizar(string texto)
            {
                return texto
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("á", "a")
                    .Replace("à", "a")
                    .Replace("ã", "a")
                    .Replace("â", "a")
                    .Replace("ä", "a")
                    .Replace("é", "e")
                    .Replace("è", "e")
                    .Replace("ê", "e")
                    .Replace("ë", "e")
                    .Replace("í", "i")
                    .Replace("ì", "i")
                    .Replace("î", "i")
                    .Replace("ï", "i")
                    .Replace("ó", "o")
                    .Replace("ò", "o")
                    .Replace("õ", "o")
                    .Replace("ô", "o")
                    .Replace("ö", "o")
                    .Replace("ú", "u")
                    .Replace("ù", "u")
                    .Replace("û", "u")
                    .Replace("ü", "u")
                    .Replace("ç", "c");
            }

            bool ContemExpressao(
                string texto,
                string expressao)
            {
                string textoNormalizado = Normalizar(texto);
                string expressaoNormalizada = Normalizar(expressao);

                if (string.IsNullOrWhiteSpace(expressaoNormalizada))
                {
                    return false;
                }

                if (expressaoNormalizada.Contains(' '))
                {
                    return textoNormalizado.Contains(
                        expressaoNormalizada
                    );
                }

                return System.Text.RegularExpressions.Regex.IsMatch(
                    textoNormalizado,
                    $@"(?<![\p{{L}}\p{{N}}])" +
                    System.Text.RegularExpressions.Regex.Escape(
                        expressaoNormalizada
                    ) +
                    $@"(?![\p{{L}}\p{{N}}])"
                );
            }

            // ============================================================
            // TERMOS DE TI
            // ============================================================

            var termosTI = new[]
            {
                    "ti",
                    "tecnologia",
                    "tecnologia da informação",
                    "technology",
                    "tech",

                    "desenvolvedor",
                    "desenvolvedora",
                    "developer",
                    "programador",
                    "programadora",
                    "software",
                    "programming",

                    "full stack",
                    "fullstack",
                    "frontend",
                    "front end",
                    "front-end",
                    "backend",
                    "back end",
                    "back-end",

                    "java",
                    "python",
                    "javascript",
                    "typescript",
                    "c#",
                    ".net",
                    "php",
                    "react",
                    "angular",
                    "node",
                    "node.js",

                    "dados",
                    "data",
                    "analista de dados",
                    "data analyst",
                    "data science",
                    "cientista de dados",
                    "business intelligence",
                    "power bi",

                    "sql",
                    "banco de dados",
                    "database",

                    "suporte",
                    "suporte técnico",
                    "suporte ti",
                    "help desk",
                    "service desk",
                    "analista de suporte",
                    "technical support",

                    "infraestrutura",
                    "infra",
                    "redes",
                    "network",
                    "networking",
                    "servidor",

                    "sistemas",
                    "sistemas de informação",
                    "analista de sistemas",

                    "cibersegurança",
                    "cybersecurity",
                    "segurança da informação",
                    "information security",

                    "cloud",
                    "aws",
                    "azure",
                    "devops",

                    "qa",
                    "quality assurance",
                    "testes de software",

                    "automação",
                    "automation",
                    "robótica",
                    "robotica",

                    "inteligência artificial",
                    "artificial intelligence",
                    "machine learning",

                    "lgpd",
                    "privacidade",
                    "governança de ti",
                    "governança de dados"
                };

            bool buscaTI =
                Normalizar(termo).Contains("estagio")
                ||
                termosTI.Any(
                    x => ContemExpressao(termo, x)
                );

            // ============================================================
            // URL DA PESQUISA
            // ============================================================

            string buscaUrl =
                "https://www.linkedin.com/jobs/search-results/?" +
                $"keywords={Uri.EscapeDataString(termo).Replace("%20", "+")}" +
                $"&origin=JOB_SEARCH_PAGE_JOB_FILTER" +
                $"&geoId={geoIdSaoPaulo}" +
                $"&distance=0.0" +
                $"&f_TPR={periodoFiltro}";

            Console.WriteLine(
                "========== URL DA BUSCA =========="
            );

            Console.WriteLine(buscaUrl);

            // ============================================================
            // ABRE O LINKEDIN
            // ============================================================

            try
            {
                await pagina.GotoAsync(
                    buscaUrl,
                    new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 60000
                    }
                );

                await pagina.WaitForTimeoutAsync(4000);

                // VERIFICAÇÃO DE LOGIN DO LINKEDIN
                Console.WriteLine("========== VERIFICANDO LOGIN DO LINKEDIN ==========");

                if (await PrecisaLoginLinkedInAsync(pagina))
                {
                    Console.WriteLine("LOGIN NECESSÁRIO NO LINKEDIN.");
                    Console.WriteLine($"URL ATUAL: {pagina.Url}");
                    Console.WriteLine("AGUARDANDO O USUÁRIO REALIZAR O LOGIN...");

                    var inicioEsperaLogin = DateTime.UtcNow;
                    var tempoMaximoEspera = TimeSpan.FromMinutes(2);

                    bool loginRealizado = false;

                    while (DateTime.UtcNow - inicioEsperaLogin < tempoMaximoEspera)
                    {
                        await pagina.WaitForTimeoutAsync(2000);

                        if (!await PrecisaLoginLinkedInAsync(pagina))
                        {
                            loginRealizado = true;

                            Console.WriteLine("==========================================");
                            Console.WriteLine("LOGIN DO LINKEDIN DETECTADO!");
                            Console.WriteLine("CONTINUANDO A PESQUISA AUTOMATICAMENTE...");
                            Console.WriteLine("==========================================");

                            break;
                        }

                        Console.WriteLine("Aguardando login...");
                    }

                    if (!loginRealizado)
                    {
                        Console.WriteLine("==========================================");
                        Console.WriteLine("TEMPO DE ESPERA DO LOGIN ESGOTADO.");
                        Console.WriteLine("==========================================");

                        return null;
                    }

                    // Depois do login, o LinkedIn pode redirecionar
                    // para outra página. Então voltamos para a mesma busca.
                    Console.WriteLine("VOLTANDO PARA A PÁGINA DA PESQUISA...");

                    await pagina.GotoAsync(
                        buscaUrl,
                        new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded,
                            Timeout = 60000
                        });

                    await pagina.WaitForTimeoutAsync(4000);

                    Console.WriteLine(
                        "========== PESQUISA APÓS LOGIN =========="
                    );

                    Console.WriteLine(
                        $"URL ATUAL: {pagina.Url}"
                    );

                    // Confirma novamente se o login foi realmente concluído.
                    if (await PrecisaLoginLinkedInAsync(pagina))
                    {
                        Console.WriteLine("O LINKEDIN AINDA ESTÁ SOLICITANDO LOGIN.");

                        return null;
                    }

                    Console.WriteLine("LOGIN CONFIRMADO.");
                    Console.WriteLine("CONTINUANDO A EXTRAÇÃO DAS VAGAS.");
                }
                else
                {
                    Console.WriteLine("LOGIN DO LINKEDIN OK. CONTINUANDO A BUSCA.");
                }

                Console.WriteLine(
                    "========== GOTO FINALIZADO =========="
                );

                Console.WriteLine(
                    $"URL ATUAL: {pagina.Url}"
                );

                // ============================================================
                // DIAGNÓSTICO DA PÁGINA ABERTA PELO PLAYWRIGHT
                // ============================================================

                Console.WriteLine("========== DIAGNÓSTICO PLAYWRIGHT ==========");

                try
                {
                    string tituloPagina =
                        await pagina.TitleAsync();

                    Console.WriteLine(
                        $"TÍTULO DA PÁGINA: {tituloPagina}"
                    );

                    string textoPagina =
                        await pagina.Locator("body").InnerTextAsync();

                    Console.WriteLine(
                        $"TAMANHO DO TEXTO DA PÁGINA: {textoPagina.Length} caracteres"
                    );

                    string htmlPagina =
                        await pagina.ContentAsync();

                    string caminhoHtml =
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "linkedin-debug.html"
                        );

                    await File.WriteAllTextAsync(
                        caminhoHtml,
                        htmlPagina
                    );

                    string caminhoImagem =
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "linkedin-debug.png"
                        );

                    await pagina.ScreenshotAsync(
                        new PageScreenshotOptions
                        {
                            Path = caminhoImagem,
                            FullPage = true
                        }
                    );

                    Console.WriteLine(
                        $"HTML salvo em: {caminhoHtml}"
                    );

                    Console.WriteLine(
                        $"IMAGEM salva em: {caminhoImagem}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"ERRO NO DIAGNÓSTICO: {ex.Message}"
                    );
                }

                Console.WriteLine(
                    "========== FIM DO DIAGNÓSTICO =========="
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"AVISO NO GOTO: {ex.Message}"
                );
            }

            // ============================================================
            // NÃO USAR NETWORK IDLE NO LINKEDIN
            // ============================================================

            await pagina.WaitForTimeoutAsync(4000);

            // ============================================================
            // LOCALIZA OS CARDS - LINKEDIN
            // ============================================================

            var cards = pagina.Locator(
                "div[role='button'][componentkey^='job-card-component-ref-']"
            );

            int quantidadeCards = await cards.CountAsync();

            // Console.WriteLine(
            //     $"CARDS [componentkey]: {quantidadeCards}"
            // );

            // ============================================================
            // FALLBACK 1
            // ============================================================

            if (quantidadeCards == 0)
            {
                cards = pagina.Locator(
                    "li[data-occludable-job-id]"
                );

                quantidadeCards = await cards.CountAsync();

                Console.WriteLine(
                    $"CARDS [data-occludable-job-id]: {quantidadeCards}"
                );
            }

            // ============================================================
            // FALLBACK 2
            // ============================================================

            if (quantidadeCards == 0)
            {
                cards = pagina.Locator(
                    "li[data-job-id]"
                );

                quantidadeCards = await cards.CountAsync();

                Console.WriteLine(
                    $"CARDS [data-job-id]: {quantidadeCards}"
                );
            }

            // ============================================================
            // FALLBACK 3
            // ============================================================

            if (quantidadeCards == 0)
            {
                cards = pagina.Locator(
                    "li.jobs-search-results__list-item"
                );

                quantidadeCards = await cards.CountAsync();

                Console.WriteLine(
                    $"CARDS [jobs-search-results__list-item]: {quantidadeCards}"
                );
            }

            // ============================================================
            // VALIDAÇÃO
            // ============================================================

            if (quantidadeCards == 0)
            {
                Console.WriteLine(
                    "NENHUM CARD ENCONTRADO NO DOM."
                );

                return new List<VagaLinkedIn>();
            }

            // ============================================================
            // LÊ AS PÁGINAS DO LINKEDIN
            // ============================================================           

            const int maxPaginas = 20;

            for (int paginaAtual = 1; paginaAtual <= maxPaginas; paginaAtual++)
            {


                for (int i = 0; i < quantidadeCards; i++)
                {
                    var card = cards.Nth(i);

                    string htmlCard = await card.InnerHTMLAsync();

                    // Console.WriteLine(
                    //     $"================ HTML CARD {i} ================"
                    // );

                    // Console.WriteLine(htmlCard);

                    // Console.WriteLine(
                    //     $"================================================"
                    // );

                    string id = "";

                    try
                    {
                        string componentKey =
                            await card.GetAttributeAsync("componentkey")
                            ?? "";

                        if (!string.IsNullOrWhiteSpace(componentKey))
                        {
                            var match =
                                Regex.Match(
                                    componentKey,
                                    @"job-card-component-ref-(\d+)"
                                );

                            if (match.Success)
                            {
                                id = match.Groups[1].Value;
                            }
                        }
                    }
                    catch
                    {
                    }

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        try
                        {
                            id =
                                await card.GetAttributeAsync(
                                    "data-occludable-job-id"
                                ) ?? "";
                        }
                        catch
                        {
                        }
                    }

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        try
                        {
                            id =
                                await card.GetAttributeAsync(
                                    "data-job-id"
                                ) ?? "";
                        }
                        catch
                        {
                        }
                    }

                    string link = "";

                    try
                    {
                        var linkElement =
                            card.Locator(
                                "a[href*='/jobs/view/']"
                            ).First;

                        if (await linkElement.CountAsync() > 0)
                        {
                            link =
                                await linkElement.GetAttributeAsync(
                                    "href"
                                ) ?? "";
                        }
                    }
                    catch
                    {
                    }

                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        var match =
                            Regex.Match(
                                link,
                                @"/jobs/view/(\d+)"
                            );

                        if (match.Success &&
                            string.IsNullOrWhiteSpace(id))
                        {
                            id = match.Groups[1].Value;
                        }

                        if (link.StartsWith("/"))
                        {
                            link =
                                "https://www.linkedin.com" +
                                link;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(link))
                    {
                        link =
                            $"https://www.linkedin.com/jobs/view/{id}/";
                    }

                    string titulo = "";

                    try
                    {
                        var tituloElement =
                            card.Locator(
                                ".job-card-list__title, " +
                                ".artdeco-entity-lockup__title, " +
                                "a[aria-label]"
                            ).First;

                        if (await tituloElement.CountAsync() > 0)
                        {
                            titulo =
                                (
                                    await tituloElement.InnerTextAsync()
                                ).Trim();
                        }

                        if (string.IsNullOrWhiteSpace(titulo))
                        {
                            string textoCard =
                                await card.InnerTextAsync();

                            titulo =
                                textoCard
                                    .Split(
                                        '\n',
                                        StringSplitOptions.RemoveEmptyEntries
                                    )
                                    .Select(
                                        linha => linha.Trim()
                                    )
                                    .FirstOrDefault(
                                        linha =>
                                            !string.IsNullOrWhiteSpace(
                                                linha
                                            )
                                    )
                                    ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"CARD {i}: ERRO AO EXTRAIR TÍTULO: {ex.Message}"
                        );
                    }

                    if (string.IsNullOrWhiteSpace(titulo))
                    {
                        Console.WriteLine(
                            $"CARD {i}: TÍTULO NÃO ENCONTRADO"
                        );

                        continue;
                    }

                    string empresa = "";

                    try
                    {
                        var empresaElement =
                            card.Locator("p").Nth(1);

                        if (await empresaElement.CountAsync() > 0)
                        {
                            empresa =
                                (
                                    await empresaElement.InnerTextAsync()
                                ).Trim();
                        }
                    }
                    catch
                    {
                    }

                    string localizacaoVaga = "";

                    try
                    {
                        var localizacaoElement =
                            card.Locator("p").Nth(2);

                        if (await localizacaoElement.CountAsync() > 0)
                        {
                            localizacaoVaga =
                                (
                                    await localizacaoElement.InnerTextAsync()
                                ).Trim();
                        }
                    }
                    catch
                    {
                    }

                    string publicacao = "";

                    try
                    {
                        var timeElement =
                            card.Locator("time").First;

                        if (await timeElement.CountAsync() > 0)
                        {
                            publicacao =
                                (
                                    await timeElement.InnerTextAsync()
                                ).Trim();
                        }
                    }
                    catch
                    {
                    }

                    if (string.IsNullOrWhiteSpace(publicacao))
                    {
                        try
                        {
                            string textoCard =
                                await card.InnerTextAsync();

                            publicacao =
                                textoCard
                                    .Split(
                                        '\n',
                                        StringSplitOptions.RemoveEmptyEntries
                                    )
                                    .Select(
                                        linha => linha.Trim()
                                    )
                                    .FirstOrDefault(
                                        linha =>
                                            linha.Contains(
                                                "há ",
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                            ||
                                            linha.Contains(
                                                "hora",
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                            ||
                                            linha.Contains(
                                                "dia",
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                            ||
                                            linha.Contains(
                                                "semana",
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                    )
                                    ?? "";
                        }
                        catch
                        {
                        }
                    }

                    // ============================================================
                    // INDICADORES DO LINKEDIN
                    // ============================================================

                    string status = "";

                    try
                    {
                        var indicadoresPermitidos = new[]
                        {
                            "Visto",
                            "Salva",
                            "Candidatura simplificada"
                        };

                        var paragrafos = card.Locator("p");

                        int quantidadeParagrafos =
                            await paragrafos.CountAsync();

                        var indicadoresEncontrados = new List<string>();

                        for (int p = 0; p < quantidadeParagrafos; p++)
                        {
                            string texto = (
                                await paragrafos.Nth(p).InnerTextAsync()
                            ).Trim();

                            foreach (var indicador in indicadoresPermitidos)
                            {
                                if (texto.Equals(
                                    indicador,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!indicadoresEncontrados.Contains(indicador))
                                    {
                                        indicadoresEncontrados.Add(indicador);
                                    }

                                    break;
                                }
                            }
                        }

                        status = string.Join(" • ", indicadoresEncontrados);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"ERRO AO IDENTIFICAR INDICADORES LINKEDIN: {ex.Message}"
                        );
                    }

                    if (buscaTI)
                    {
                        string textoAnalise =
                            titulo + " " +
                            empresa;

                        bool possuiTI =
                            termosTI.Any(
                                termoTI =>
                                    ContemExpressao(
                                        textoAnalise,
                                        termoTI
                                    )
                            );

                        bool tituloGenerico =
                            ContemExpressao(
                                titulo,
                                "estagiário"
                            )
                            ||
                            ContemExpressao(
                                titulo,
                                "estagiaria"
                            )
                            ||
                            ContemExpressao(
                                titulo,
                                "estágio"
                            )
                            ||
                            ContemExpressao(
                                titulo,
                                "programa de estágio"
                            );

                        if (!possuiTI && !tituloGenerico)
                        {
                            Console.WriteLine(
                                $"IGNORADA PELO FILTRO TI: {titulo}"
                            );

                            continue;
                        }
                    }

                    if (vagasPorId.ContainsKey(id))
                    {
                        continue;
                    }

                    vagasPorId.Add(
                        id,
                        new VagaLinkedIn
                        {
                            Titulo = titulo,
                            Empresa = empresa,
                            Localizacao = localizacaoVaga,
                            Link = link,
                            Publicacao = publicacao,
                            StatusLinkedIn = status,
                            // A descrição será carregada somente
                            // quando o usuário clicar em "Ver detalhes".
                            Descricao = ""
                        }
                    );

                    Console.WriteLine(
                        $"VAGA: {titulo} | " +
                        $"EMPRESA: {empresa} | " +
                        $"LOCAL: {localizacaoVaga} | " +
                        $"PUBLICAÇÃO: {publicacao} | " +
                        $"STATUS: {status}"
                    );
                }

                if (paginaAtual >= maxPaginas)
                {
                    Console.WriteLine(
                        $"LIMITE MÁXIMO DE {maxPaginas} PÁGINAS ATINGIDO."
                    );

                    break;
                }

                int proximaPaginaNumero =
                    paginaAtual + 1;

                var paginaProxima =
                    pagina.Locator(
                        $"[aria-label='Página {proximaPaginaNumero}']"
                    );

                if (await paginaProxima.CountAsync() == 0)
                {
                    Console.WriteLine(
                        $"PÁGINA {proximaPaginaNumero} NÃO ENCONTRADA. FIM DA PAGINAÇÃO."
                    );

                    break;
                }

                Console.WriteLine(
                    $"NAVEGANDO PARA PÁGINA {proximaPaginaNumero}..."
                );

                await paginaProxima.ClickAsync();

                await pagina.WaitForTimeoutAsync(2000);

                cards =
                    pagina.Locator(
                        "div[role='button'][componentkey^='job-card-component-ref-']"
                    );

                quantidadeCards =
                    await cards.CountAsync();

                if (quantidadeCards == 0)
                {
                    cards =
                        pagina.Locator(
                            "li[data-occludable-job-id]"
                        );

                    quantidadeCards =
                        await cards.CountAsync();
                }

                if (quantidadeCards == 0)
                {
                    cards =
                        pagina.Locator(
                            "li[data-job-id]"
                        );

                    quantidadeCards =
                        await cards.CountAsync();
                }

                if (quantidadeCards == 0)
                {
                    cards =
                        pagina.Locator(
                            "li.jobs-search-results__list-item"
                        );

                    quantidadeCards =
                        await cards.CountAsync();
                }

                if (quantidadeCards == 0)
                {
                    Console.WriteLine(
                        $"NENHUM CARD ENCONTRADO NA PÁGINA {proximaPaginaNumero}. FIM DA PAGINAÇÃO."
                    );

                    break;
                }
            }

            // ============================================================
            // RESULTADO
            // ============================================================

            Console.WriteLine(
                $"VAGAS EXTRAÍDAS DO HTML: {vagasPorId.Count}"
            );

            var resultadoFinal =
                vagasPorId.Values.ToList();

            Console.WriteLine(
                $"RESULTADO FINAL BuscarVagasAsync: {resultadoFinal.Count} vagas"
            );

            return resultadoFinal;

        }

        // ----------------------------------------------------
        // OBTER DETALHES DA VAGA
        // ----------------------------------------------------
        public async Task<object?> ObterDetalhesVagaAsync(string link)
        {
            try
            {
                var page = await ObterPaginaAsync();

                Console.WriteLine(
                    "=========================================="
                );
                Console.WriteLine(
                    "BUSCANDO DETALHES DA VAGA"
                );
                Console.WriteLine(
                    $"LINK: {link}"
                );
                Console.WriteLine(
                    "=========================================="
                );

                await page.GotoAsync(
                    link,
                    new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    }
                );

                await page.WaitForTimeoutAsync(2000);

                // ----------------------------------------------------
                // TÍTULO
                // ----------------------------------------------------

                string titulo = "";

                try
                {
                    var elementoTitulo =
                        page.Locator(
                            "h1"
                        ).First;

                    if (await elementoTitulo.CountAsync() > 0)
                    {
                        titulo =
                            (
                                await elementoTitulo.InnerTextAsync()
                            ).Trim();
                    }
                }
                catch
                {
                }

                // ----------------------------------------------------
                // EMPRESA
                // ----------------------------------------------------

                string empresa = "";

                try
                {
                    var elementoEmpresa =
                        page.Locator(
                            "a[href*='/company/']"
                        ).First;

                    if (await elementoEmpresa.CountAsync() > 0)
                    {
                        empresa =
                            (
                                await elementoEmpresa.InnerTextAsync()
                            ).Trim();
                    }
                }
                catch
                {
                }

                // ----------------------------------------------------
                // LOCALIZAÇÃO
                // ----------------------------------------------------

                string localizacao = "";

                try
                {
                    var elementosTexto =
                        page.Locator("span");

                    int quantidade =
                        await elementosTexto.CountAsync();

                    for (int i = 0; i < quantidade; i++)
                    {
                        string texto =
                            (
                                await elementosTexto.Nth(i).InnerTextAsync()
                            ).Trim();

                        if (
                            texto.Contains("São Paulo") ||
                            texto.Contains("Brasil") ||
                            texto.Contains("SP")
                        )
                        {
                            localizacao = texto;
                            break;
                        }
                    }
                }
                catch
                {
                }

                // ----------------------------------------------------
                // DESCRIÇÃO DA VAGA
                // ----------------------------------------------------

                string descricao = "";

                try
                {
                    // Primeiro tenta os seletores conhecidos do LinkedIn
                    var seletoresDescricao = new[]
                    {
                        ".jobs-description-content__text",
                        ".jobs-description-content__text--stretch",
                        ".jobs-box__html-content",
                        ".jobs-description__content"
                    };

                    foreach (var seletor in seletoresDescricao)
                    {
                        try
                        {
                            var elemento =
                                page.Locator(seletor).First;

                            if (await elemento.CountAsync() > 0)
                            {
                                string texto =
                                    (
                                        await elemento.InnerTextAsync()
                                    ).Trim();

                                if (!string.IsNullOrWhiteSpace(texto))
                                {
                                    descricao = texto;

                                    Console.WriteLine(
                                        $"DESCRIÇÃO ENCONTRADA PELO SELETOR: {seletor}"
                                    );

                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    // ------------------------------------------------
                    // FALLBACK
                    // Procura o bloco que contém "Sobre a vaga"
                    // ------------------------------------------------

                    if (string.IsNullOrWhiteSpace(descricao))
                    {
                        var elementos =
                            page.Locator("div, section, article");

                        int quantidade =
                            await elementos.CountAsync();

                        for (int i = 0; i < quantidade; i++)
                        {
                            try
                            {
                                var elemento =
                                    elementos.Nth(i);

                                string texto =
                                    (
                                        await elemento.InnerTextAsync()
                                    ).Trim();

                                if (string.IsNullOrWhiteSpace(texto))
                                {
                                    continue;
                                }

                                if (
                                    texto.StartsWith(
                                        "Sobre a vaga",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    &&
                                    texto.Length > 500
                                )
                                {
                                    descricao = texto;

                                    // Console.WriteLine(
                                    //     $"DESCRIÇÃO ENCONTRADA NO ELEMENTO: {i}"
                                    // );

                                    break;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"ERRO AO EXTRAIR DESCRIÇÃO: {ex.Message}"
                    );
                }

                Console.WriteLine(
                    $"DESCRIÇÃO: {descricao.Length} caracteres"
                );


                Console.WriteLine(
                    $"TÍTULO: {titulo}"
                );

                Console.WriteLine(
                    $"EMPRESA: {empresa}"
                );

                Console.WriteLine(
                    $"LOCALIZAÇÃO: {localizacao}"
                );

                Console.WriteLine(
                    $"DESCRIÇÃO: {descricao.Length} caracteres"
                );

                return new
                {
                    titulo,
                    empresa,
                    localizacao,
                    descricao,
                    link
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERRO AO OBTER DETALHES DA VAGA: {ex.Message}"
                );

                return null;
            }
        }


    }
}