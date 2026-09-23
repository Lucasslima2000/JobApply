using Microsoft.Playwright;
using JobApply.Models;
using System.Text.Json;

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

        public async Task<string> CandidatarAsync(string linkVaga)
        {
            var page = await ObterPaginaAsync();

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
            }
            catch (TimeoutException)
            {
                Console.WriteLine(
                    "O LinkedIn demorou para carregar completamente."
                );
            }

            await page.WaitForTimeoutAsync(3000);

            Console.WriteLine(
                $"Página atual do LinkedIn: {page.Url}"
            );

            // ============================================================
            // VERIFICAR LOGIN DO LINKEDIN
            // ============================================================

            if (await PrecisaLoginLinkedInAsync(page))
            {
                Console.WriteLine(
                    "LOGIN NECESSÁRIO NO LINKEDIN."
                );

                return "LOGIN_LINKEDIN";
            }

            // ============================================================
            // LOCALIZAR "CANDIDATE-SE"
            // ============================================================

            var botaoCandidatar =
                page.GetByText(
                    "Candidate-se",
                    new PageGetByTextOptions
                    {
                        Exact = true
                    });

            var quantidade =
                await botaoCandidatar.CountAsync();

            Console.WriteLine(
                $"Elementos 'Candidate-se' encontrados: {quantidade}"
            );

            // Tentar também "Candidatar-se"
            if (quantidade == 0)
            {
                botaoCandidatar =
                    page.GetByText(
                        "Candidatar-se",
                        new PageGetByTextOptions
                        {
                            Exact = true
                        });

                quantidade =
                    await botaoCandidatar.CountAsync();

                Console.WriteLine(
                    $"Elementos 'Candidatar-se' encontrados: {quantidade}"
                );
            }

            if (quantidade == 0)
            {
                Console.WriteLine(
                    "Botão 'Candidate-se' não encontrado."
                );

                return "CANDIDATURA_NAO_ENCONTRADA";
            }

            Console.WriteLine(
                "Botão 'Candidate-se' encontrado."
            );

            // ============================================================
            // CLICAR NO "CANDIDATE-SE"
            // ============================================================

            IPage? paginaExterna = null;

            try
            {
                await botaoCandidatar
                    .First
                    .ScrollIntoViewIfNeededAsync();

                Console.WriteLine(
                    "Clicando no botão 'Candidate-se'..."
                );

                try
                {
                    paginaExterna =
                        await page.RunAndWaitForPopupAsync(
                            async () =>
                            {
                                await botaoCandidatar
                                    .First
                                    .ClickAsync();
                            },
                            new PageRunAndWaitForPopupOptions
                            {
                                Timeout = 5000
                            });

                    Console.WriteLine(
                        "Uma nova página/janela foi aberta pelo LinkedIn."
                    );
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(
                        "Nenhuma nova janela detectada. Verificando a página atual."
                    );

                    // O clique pode ter acontecido mesmo sem popup.
                }

                Console.WriteLine(
                    "Botão 'Candidate-se' clicado automaticamente."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao clicar no botão 'Candidate-se': {ex.Message}"
                );

                return "ERRO_AO_ABRIR_CANDIDATURA";
            }

            await page.WaitForTimeoutAsync(5000);

            // ============================================================
            // VERIFICAR NOVA PÁGINA / SITE EXTERNO
            // ============================================================

            if (paginaExterna != null)
            {
                await paginaExterna.WaitForTimeoutAsync(5000);

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

                        // Verificar se precisa de login antes de continuar
                        if (await PrecisaLoginSiteExternoAsync(paginaExterna))
                        {
                            Console.WriteLine(
                                "LOGIN NECESSÁRIO NA GUPY."
                            );

                            Console.WriteLine(
                                "Faça o login manualmente no navegador."
                            );

                            var loginConcluido =
                                await AguardarLoginSiteExternoAsync(paginaExterna);

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

                            Console.WriteLine(
                                $"Continuando na página: {paginaExterna.Url}"
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

                            // Verificar se o clique levou para login
                            if (await PrecisaLoginSiteExternoAsync(paginaExterna))
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

                        if (await PrecisaLoginSiteExternoAsync(paginaExterna))
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
                $"Página após clicar no Candidate-se: {page.Url}"
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
                // GUPY ABERTA ATRAVÉS DO LINK DO LINKEDIN
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

                        if (await PrecisaLoginSiteExternoAsync(novaPagina))
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

            var modalCandidatura =
                page.Locator(
                    "[data-test-modal], " +
                    ".jobs-easy-apply-modal, " +
                    ".jobs-easy-apply-content"
                );

            if (await modalCandidatura.CountAsync() > 0)
            {
                Console.WriteLine(
                    "Formulário de candidatura do LinkedIn aberto."
                );

                return "CANDIDATURA_LINKEDIN_ABERTA";
            }

            Console.WriteLine(
                "O botão 'Candidate-se' foi clicado, mas não foi possível identificar o próximo passo."
            );

            return "CANDIDATURA_NAO_ENCONTRADA";
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
            var url = page.Url.ToLowerInvariant();

            if (
                url.Contains("/login") ||
                url.Contains("/checkpoint") ||
                url.Contains("/authwall"))
            {
                return true;
            }

            var indicadores =
                page.Locator(
                    "input[name='session_key'], " +
                    "input[name='session_password'], " +
                    "form[action*='login']"
                );

            return await indicadores.CountAsync() > 0;
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

        private async Task<bool> AguardarLoginSiteExternoAsync(
    IPage page,
    int tempoMaximoSegundos = 120)
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







        public async Task<List<VagaLinkedIn>> BuscarVagasAsync(
            string termo,
            string? localizacao,
            string? periodo)
        {
            var pagina = await ObterPaginaAsync();

            var vagasPorId =
                new Dictionary<string, VagaLinkedIn>();

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

            const string geoIdSaoPaulo =
                "105871508";

            const int quantidadeSolicitada =
                7;

            const int maxRequisicoes =
                50;

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("       INICIANDO BUSCA NO LINKEDIN");
            Console.WriteLine("==========================================");
            Console.WriteLine($"TERMO: [{termo}]");
            Console.WriteLine($"LOCALIZAÇÃO: [{localizacao}]");
            Console.WriteLine($"PERÍODO: [{periodo}]");
            Console.WriteLine($"FILTRO LINKEDIN: [{periodoFiltro}]");

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
                string textoNormalizado =
                    Normalizar(texto);

                string expressaoNormalizada =
                    Normalizar(expressao);

                if (string.IsNullOrWhiteSpace(
                    expressaoNormalizada))
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

            // ============================================================
            // TERMOS CLARAMENTE FORA DE TI
            // ============================================================

            var termosForaTI = new[]
            {
        "logística",
        "logistica",
        "delivery",

        "recepcionista",
        "recepção",
        "recepcao",

        "atendente",
        "atendimento",

        "administrativo",
        "administrativa",
        "administração",
        "administracao",

        "estoque",
        "almoxarifado",

        "financeiro",
        "finanças",
        "financas",
        "contabilidade",
        "contábil",
        "contabil",

        "marketing",

        "vendas",
        "vendedor",
        "vendedora",
        "comercial",
        "business development",

        "recursos humanos",
        "rh",
        "recrutamento",

        "jurídico",
        "juridico",
        "advocacia",

        "compras",
        "procurement",

        "motorista",

        "enfermagem",
        "farmácia",
        "farmacia",

        "meio ambiente",

        "engenharia civil",
        "engenharia mecânica",
        "engenharia mecanica",
        "engenharia elétrica",
        "engenharia eletrica",

        "facilities",

        "legal operations",

        "accounting",
        "accountant",

        "merchandising",

        "jovem aprendiz",
        "aprendiz administrativo"
    };

            bool buscaTI =
                Normalizar(termo).Contains("estagio")
                ||
                termosTI.Any(
                    x => ContemExpressao(termo, x)
                );

            // ============================================================
            // 1. ABRE A PÁGINA NORMAL DO LINKEDIN
            // ============================================================

            string buscaUrl =
                "https://www.linkedin.com/jobs/search/?" +
                $"keywords={Uri.EscapeDataString(termo)}" +
                $"&location={Uri.EscapeDataString(localizacao ?? "São Paulo")}" +
                $"&geoId={geoIdSaoPaulo}" +
                $"&distance=0.0" +
                $"&f_TPR={periodoFiltro}";

            Console.WriteLine();
            Console.WriteLine("ABRINDO BUSCA:");
            Console.WriteLine(buscaUrl);

            pagina.Request += (_, request) =>
{
    if (
        request.Url.Contains(
            "voyagerJobsDashJobCards",
            StringComparison.OrdinalIgnoreCase
        )
    )
    {
        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine("JOB SEARCH REQUEST REAL DO LINKEDIN");
        Console.WriteLine("================================================");
        Console.WriteLine(request.Url);
        Console.WriteLine("================================================");
    }
};

            pagina.Response += (_, response) =>
            {
                if (
                    response.Url.Contains(
                        "voyagerJobsDashJobCards",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    Console.WriteLine();
                    Console.WriteLine("================================================");
                    Console.WriteLine("JOB SEARCH RESPONSE REAL DO LINKEDIN");
                    Console.WriteLine("STATUS: " + response.Status);
                    Console.WriteLine(response.Url);
                    Console.WriteLine("================================================");
                }
            };

            string? endpointBuscaReal = null;

            pagina.Request += (_, request) =>
            {
                if (
                    request.Url.Contains(
                        "/voyager/api/voyagerJobsDashJobCards",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    request.Url.Contains(
                        "JobSearchCardsCollection-",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    !request.Url.Contains(
                        "JobSearchCardsCollectionLite-",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    endpointBuscaReal = request.Url;

                    Console.WriteLine();
                    Console.WriteLine("================================================");
                    Console.WriteLine("ENDPOINT REAL DA PESQUISA DO LINKEDIN");
                    Console.WriteLine("================================================");
                    Console.WriteLine(endpointBuscaReal);
                    Console.WriteLine("================================================");
                }
            };

            try
            {
                await pagina.GotoAsync(
                    buscaUrl,
                    new PageGotoOptions
                    {
                        WaitUntil =
                            WaitUntilState.DOMContentLoaded,

                        Timeout = 30000
                    }
                );

                Console.WriteLine();
                Console.WriteLine("URL FINAL DO LINKEDIN:");
                Console.WriteLine(pagina.Url);

            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Aviso na navegação: {ex.Message}"
                );
            }

            await pagina.WaitForTimeoutAsync(1200);

            // ============================================================
            // 2. DESCOBRE O DATALET DO LINKEDIN
            // ============================================================



            string? endpoint =
                null;

            string? bodyId =
                null;

            var datalets =
                pagina.Locator(
                    "code[id^='datalet-bpr-guid-']"
                );

            int quantidadeDatalets =
                await datalets.CountAsync();

            for (
                int i = 0;
                i < quantidadeDatalets;
                i++)
            {
                try
                {
                    string texto =
                        await datalets
                            .Nth(i)
                            .InnerTextAsync();

                    if (
                        !texto.Contains(
                            "voyagerJobsDashJobCards",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    using var metaJson =
                        JsonDocument.Parse(texto);

                    var root =
                        metaJson.RootElement;

                    if (
                        root.TryGetProperty(
                            "request",
                            out var requestElement
                        )
                    )
                    {
                        endpoint =
                            requestElement.GetString();
                    }

                    if (
                        root.TryGetProperty(
                            "body",
                            out var bodyElement
                        )
                    )
                    {
                        bodyId =
                            bodyElement.GetString();
                    }

                    break;
                }
                catch
                {
                }
            }

            if (
                string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(bodyId)
            )
            {
                Console.WriteLine();
                Console.WriteLine(
                    "ERRO: não foi possível localizar " +
                    "o endpoint voyagerJobsDashJobCards."
                );

                return new List<VagaLinkedIn>();
            }

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("ENDPOINT ORIGINAL DO DATALET:");
            Console.WriteLine(endpoint);
            Console.WriteLine("==========================================");

            Console.WriteLine();
            Console.WriteLine("BODY ID:");
            Console.WriteLine(bodyId);

            string endpointBase =
                new Uri(
                    new Uri("https://www.linkedin.com"),
                    endpoint
                ).ToString();

            Console.WriteLine();
            Console.WriteLine("ENDPOINT BASE ORIGINAL:");
            Console.WriteLine(endpointBase);


            // ============================================================
            // ALINHA A ORIGEM
            // ============================================================

            // endpointBase =
            //     endpointBase.Replace(
            //         "origin:JOB_SEARCH_PAGE_OTHER_ENTRY",
            //         "origin:JOB_SEARCH_PAGE_JOB_FILTER",
            //         StringComparison.Ordinal
            //     );

            // endpointBase =
            //     endpointBase.Replace(
            //         "origin%3AJOB_SEARCH_PAGE_OTHER_ENTRY",
            //         "origin%3AJOB_SEARCH_PAGE_JOB_FILTER",
            //         StringComparison.OrdinalIgnoreCase
            //     );

            Console.WriteLine();
            Console.WriteLine(
                "ENDPOINT INTERNO DO LINKEDIN:"
            );
            //Console.WriteLine(endpointBase);

            // ============================================================
            // 3. FUNÇÃO PARA PEGAR ID DO URN
            // ============================================================

            string ExtrairId(string? urn)
            {
                if (string.IsNullOrWhiteSpace(urn))
                    return "";

                var match =
                    System.Text.RegularExpressions.Regex.Match(
                        urn,
                        @"\((\d+),"
                    );

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                match =
                    System.Text.RegularExpressions.Regex.Match(
                        urn,
                        @":(\d+)$"
                    );

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                return "";
            }

            // ============================================================
            // 4. PROCESSA UMA RESPOSTA
            // ============================================================

            int totalResultados =
                0;

            int ProcessarResposta(
                string json,
                bool exibirLog)
            {
                int novos =
                    0;

                using var documento =
                    JsonDocument.Parse(json);

                var raiz =
                    documento.RootElement;

                JsonElement dados;

                if (
                    raiz.TryGetProperty(
                        "data",
                        out var dataElement
                    )
                )
                {
                    dados = dataElement;
                }
                else
                {
                    dados = raiz;
                }

                // --------------------------------------------------------
                // PAGINAÇÃO
                // --------------------------------------------------------

                int paginaTotal =
                    0;

                if (
                    dados.TryGetProperty(
                        "paging",
                        out var paging
                    )
                )
                {
                    if (
                        paging.TryGetProperty(
                            "total",
                            out var totalElement
                        )
                    )
                    {
                        paginaTotal =
                            totalElement.GetInt32();
                    }
                }

                Console.WriteLine(
    $"PAGING COMPLETO: {paging.GetRawText()}"
);

                if (paginaTotal > 0)
                {
                    totalResultados =
                        paginaTotal;
                }

                // --------------------------------------------------------
                // ELEMENTS
                // --------------------------------------------------------

                if (
                    !dados.TryGetProperty(
                        "elements",
                        out var elements
                    )
                )
                {
                    return 0;
                }

                // --------------------------------------------------------
                // INDEXA JOB POSTING CARDS
                // --------------------------------------------------------

                var cards =
                    new Dictionary<string, JsonElement>();

                if (
                    raiz.TryGetProperty(
                        "included",
                        out var included
                    )
                    &&
                    included.ValueKind ==
                        JsonValueKind.Array
                )
                {
                    foreach (
                        var item
                        in included.EnumerateArray()
                    )
                    {
                        if (
                            !item.TryGetProperty(
                                "$type",
                                out var typeElement
                            )
                        )
                        {
                            continue;
                        }

                        string tipo =
                            typeElement.GetString() ?? "";

                        if (
                            !tipo.EndsWith(
                                "JobPostingCard",
                                StringComparison.Ordinal
                            )
                        )
                        {
                            continue;
                        }

                        string id =
                            "";

                        if (
                            item.TryGetProperty(
                                "jobPostingUrn",
                                out var jobUrnElement
                            )
                        )
                        {
                            id =
                                ExtrairId(
                                    jobUrnElement.GetString()
                                );
                        }

                        if (
                            string.IsNullOrWhiteSpace(id)
                            &&
                            item.TryGetProperty(
                                "entityUrn",
                                out var entityElement
                            )
                        )
                        {
                            id =
                                ExtrairId(
                                    entityElement.GetString()
                                );
                        }

                        if (
                            !string.IsNullOrWhiteSpace(id)
                        )
                        {
                            cards[id] =
                                item;
                        }
                    }
                }

                // --------------------------------------------------------
                // PROCESSA ELEMENTS
                // --------------------------------------------------------

                foreach (
                    var element
                    in elements.EnumerateArray()
                )
                {
                    try
                    {
                        if (
                            !element.TryGetProperty(
                                "jobCardUnion",
                                out var unionElement
                            )
                        )
                        {
                            continue;
                        }

                        if (
                            !unionElement.TryGetProperty(
                                "*jobPostingCard",
                                out var cardUrnElement
                            )
                        )
                        {
                            continue;
                        }

                        string cardUrn =
                            cardUrnElement.GetString() ?? "";

                        string id =
                            ExtrairId(cardUrn);

                        if (
                            string.IsNullOrWhiteSpace(id)
                            ||
                            vagasPorId.ContainsKey(id)
                        )
                        {
                            continue;
                        }

                        if (
                            !cards.TryGetValue(
                                id,
                                out var card
                            )
                        )
                        {
                            continue;
                        }

                        // ------------------------------------------------
                        // TÍTULO
                        // ------------------------------------------------

                        string titulo =
                            "";

                        if (
                            card.TryGetProperty(
                                "jobPostingTitle",
                                out var jobTitleElement
                            )
                        )
                        {
                            titulo =
                                jobTitleElement.GetString() ?? "";
                        }

                        if (
                            string.IsNullOrWhiteSpace(titulo)
                            &&
                            card.TryGetProperty(
                                "title",
                                out var titleElement
                            )
                        )
                        {
                            if (
                                titleElement.TryGetProperty(
                                    "text",
                                    out var textElement
                                )
                            )
                            {
                                titulo =
                                    textElement.GetString() ?? "";
                            }

                            if (
                                string.IsNullOrWhiteSpace(titulo)
                                &&
                                titleElement.TryGetProperty(
                                    "accessibilityText",
                                    out var accessibilityElement
                                )
                            )
                            {
                                titulo =
                                    accessibilityElement.GetString()
                                    ?? "";
                            }
                        }

                        titulo =
                            titulo
                                .Replace(
                                    "with verification",
                                    "",
                                    StringComparison.OrdinalIgnoreCase
                                )
                                .Trim();

                        if (
                            string.IsNullOrWhiteSpace(titulo)
                        )
                        {
                            continue;
                        }

                        // ------------------------------------------------
                        // EMPRESA
                        // ------------------------------------------------

                        string empresa =
                            "";

                        if (
                            card.TryGetProperty(
                                "primaryDescription",
                                out var primaryElement
                            )
                            &&
                            primaryElement.TryGetProperty(
                                "text",
                                out var primaryText
                            )
                        )
                        {
                            empresa =
                                primaryText.GetString() ?? "";
                        }

                        // ------------------------------------------------
                        // LOCALIZAÇÃO
                        // ------------------------------------------------

                        string local =
                            "";

                        if (
                            card.TryGetProperty(
                                "secondaryDescription",
                                out var secondaryElement
                            )
                            &&
                            secondaryElement.TryGetProperty(
                                "text",
                                out var secondaryText
                            )
                        )
                        {
                            local =
                                secondaryText.GetString() ?? "";
                        }

                        // ------------------------------------------------
                        // LINK
                        // ------------------------------------------------

                        string link =
                            $"https://www.linkedin.com/jobs/view/{id}/";

                        // ------------------------------------------------
                        // RELEVÂNCIA
                        // ------------------------------------------------

                        if (buscaTI)
                        {
                            string textoAnalise =
                                            titulo;

                            bool possuiTI =
                                termosTI.Any(
                                    termoTI =>
                                        ContemExpressao(
                                            textoAnalise,
                                            termoTI
                                        )
                                );

                            bool possuiForaTI =
                                termosForaTI.Any(
                                    termoForaTI =>
                                        ContemExpressao(
                                            textoAnalise,
                                            termoForaTI
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

                            if (
                                !possuiTI
                                &&
                                !tituloGenerico
                            )
                            {
                                if (exibirLog)
                                {
                                    Console.WriteLine(
                                        $"DESCARTADA: {titulo}"
                                    );
                                }

                                continue;
                            }
                        }
                        var vaga =
                            new VagaLinkedIn
                            {
                                Titulo = titulo,
                                Empresa = empresa,
                                Localizacao = local,
                                Link = link,
                                Publicacao = "",
                                StatusLinkedIn = ""
                            };

                        vagasPorId.Add(
                            id,
                            vaga
                        );

                        novos++;

                        if (exibirLog)
                        {
                            Console.WriteLine(
                                $"VAGA: {titulo} | " +
                                $"EMPRESA: {empresa} | " +
                                $"LOCAL: {local}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        if (exibirLog)
                        {
                            Console.WriteLine(
                                $"Erro ao processar vaga: " +
                                $"{ex.Message}"
                            );
                        }
                    }
                }

                return novos;
            }

            // ============================================================
            // 5. PROCESSA O PRIMEIRO BODY SSR
            // ============================================================

            try
            {
                var bodyLocator =
                    pagina.Locator(
                        $"code[id='{bodyId}']"
                    );

                if (
                    await bodyLocator.CountAsync() > 0
                )
                {
                    string bodyJson =
                        await bodyLocator
                            .InnerTextAsync();

                    int novos =
                        ProcessarResposta(
                            bodyJson,
                            true
                        );

                    Console.WriteLine();
                    Console.WriteLine(
                        $"PRIMEIRA RESPOSTA: " +
                        $"{novos} novas vagas"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao processar resposta inicial: " +
                    $"{ex.Message}"
                );
            }

            // ============================================================
            // 6. PAGINAÇÃO
            //
            // IMPORTANTE:
            // A consulta agora é executada dentro da própria página
            // do LinkedIn através de fetch().
            //
            // Isso evita o problema que estávamos tendo com:
            //
            // HTTP 403
            // CSRF check failed
            // ============================================================

            int start =
                0;

            int requisicao =
                0;

            int semNovidade =
                0;

            while (
                requisicao < maxRequisicoes
                &&
                (
                    totalResultados == 0
                    ||
                    start < totalResultados
                )
            )
            {
                requisicao++;

                string apiUrl =
                    new System.Text.RegularExpressions.Regex(
                        @"([?&])count=\d+"
                    ).Replace(
                        endpointBase,
                        $"$1count={quantidadeSolicitada}",
                        1
                    );

                apiUrl =
                    new System.Text.RegularExpressions.Regex(
                        @"([?&])start=\d+"
                    ).Replace(
                        apiUrl,
                        $"$1start={start}",
                        1
                    );

                Console.WriteLine();
                Console.WriteLine("API URL COMPLETA:");
                Console.WriteLine(apiUrl);
                Console.WriteLine();

                Console.WriteLine();
                Console.WriteLine(
                    $"CONSULTA API {requisicao}: " +
                    $"start={start}"
                );

                try
                {
                    // --------------------------------------------------------
                    // FAZ A REQUISIÇÃO DENTRO DA PRÓPRIA PÁGINA
                    // DO LINKEDIN.
                    // --------------------------------------------------------

                    string resultadoFetch =
    await pagina.EvaluateAsync<string>(
        @"async (url) => {

            try
            {
                // ====================================================
                // O LinkedIn utiliza o JSESSIONID como base
                // para validação do CSRF nas chamadas Voyager.
                // ====================================================

                const match =
                    document.cookie.match(
                        /JSESSIONID=(?:""([^""]+)""|([^;]+))/
                    );

                const csrfToken =
                    match
                        ? (match[1] || match[2])
                        : '';

                console.log(
                    'CSRF encontrado:',
                    csrfToken ? 'SIM' : 'NAO'
                );

                const resposta =
                    await fetch(
                        url,
                        {
                            method: 'GET',

                            credentials: 'include',

                            headers: {
                                'Accept':
                                    'application/vnd.linkedin.normalized+json+2.1',

                                'X-Restli-Protocol-Version':
                                    '2.0.0',

                                'csrf-token':
                                    csrfToken
                            }
                        }
                    );

                const texto =
                    await resposta.text();

                return JSON.stringify({
                    status:
                        resposta.status,

                    ok:
                        resposta.ok,

                    body:
                        texto
                });
            }
            catch (erro)
            {
                return JSON.stringify({
                    status: 0,

                    ok: false,

                    body:
                        String(erro)
                });
            }

        }",
        apiUrl
    );
                    using var wrapper =
                        JsonDocument.Parse(
                            resultadoFetch
                        );

                    var wrapperRoot =
                        wrapper.RootElement;

                    int statusHttp =
                        wrapperRoot
                            .GetProperty("status")
                            .GetInt32();

                    bool respostaOk =
                        wrapperRoot
                            .GetProperty("ok")
                            .GetBoolean();

                    string json =
                        wrapperRoot
                            .GetProperty("body")
                            .GetString()
                            ?? "";

                    Console.WriteLine(
                        $"HTTP: {statusHttp}"
                    );

                    if (!respostaOk)
                    {
                        Console.WriteLine(
                            "LinkedIn rejeitou a consulta."
                        );

                        Console.WriteLine(
                            json.Length > 500
                                ? json.Substring(0, 500)
                                : json
                        );

                        break;
                    }

                    // --------------------------------------------------------
                    // DESCOBRE QUANTOS VIERAM
                    // --------------------------------------------------------

                    int quantidadeRecebida =
                        0;

                    int novos =
                        0;

                    using (
                        var documento =
                            JsonDocument.Parse(
                                json
                            )
                    )
                    {
                        var raiz =
                            documento.RootElement;

                        JsonElement dados =
                            raiz.TryGetProperty(
                                "data",
                                out var dataElement
                            )
                                ? dataElement
                                : raiz;

                        if (
                            dados.TryGetProperty(
                                "elements",
                                out var elements
                            )
                        )
                        {
                            quantidadeRecebida =
                                elements.GetArrayLength();
                        }

                        if (
                            dados.TryGetProperty(
                                "paging",
                                out var paging
                            )
                            &&
                            paging.TryGetProperty(
                                "count",
                                out var countElement
                            )
                        )
                        {
                            quantidadeRecebida =
                                countElement.GetInt32();
                        }
                    }

                    novos =
                        ProcessarResposta(
                            json,
                            true
                        );

                    Console.WriteLine(
                        $"Recebidas: {quantidadeRecebida} | " +
                        $"Novas: {novos} | " +
                        $"Total acumulado: {vagasPorId.Count} | " +
                        $"Total LinkedIn: {totalResultados}"
                    );

                    if (novos == 0)
                    {
                        semNovidade++;
                    }
                    else
                    {
                        semNovidade = 0;
                    }

                    if (
                        quantidadeRecebida <= 0
                    )
                    {
                        Console.WriteLine(
                            "LinkedIn não retornou mais vagas."
                        );

                        break;
                    }

                    // --------------------------------------------------------
                    // AVANÇA COM A QUANTIDADE REAL DEVOLVIDA
                    // --------------------------------------------------------

                    start += quantidadeRecebida;

                    // --------------------------------------------------------
                    // PROTEÇÃO CONTRA LOOP
                    // --------------------------------------------------------

                    if (semNovidade >= 3)
                    {
                        Console.WriteLine(
                            "Três consultas sem novas vagas. " +
                            "Finalizando."
                        );

                        break;
                    }

                    // --------------------------------------------------------
                    // TERMINOU O TOTAL INFORMADO
                    // --------------------------------------------------------

                    if (
                        totalResultados > 0
                        &&
                        start >= totalResultados
                    )
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro na consulta API: {ex.Message}"
                    );

                    break;
                }
            }

            // ============================================================
            // 7. ENRIQUECE AS VAGAS VISÍVEIS DA PRIMEIRA PÁGINA
            //
            // PUBLICAÇÃO
            // STATUS LINKEDIN
            // ============================================================

            try
            {
                string jsonVisiveis =
                    await pagina.EvaluateAsync<string>(
                        @"() => {

                    const cards = [
                        ...document.querySelectorAll(
                            'li[data-occludable-job-id]'
                        )
                    ];

                    return JSON.stringify(
                        cards.map(card => {

                            const container =
                                card.querySelector(
                                    'div[data-job-id]'
                                );

                            const id =
                                container?.getAttribute(
                                    'data-job-id'
                                )
                                ||
                                card.getAttribute(
                                    'data-occludable-job-id'
                                )
                                ||
                                '';

                            const time =
                                card.querySelector(
                                    'time'
                                )?.innerText
                                ||
                                '';

                            const status =
                                card.querySelector(
                                    '.job-card-container__footer-job-state'
                                )?.innerText
                                ||
                                '';

                            return {
                                id,
                                time,
                                status
                            };
                        })
                    );
                }"
                    );

                using var documentoVisiveis =
                    JsonDocument.Parse(
                        jsonVisiveis
                    );

                foreach (
                    var item
                    in documentoVisiveis.RootElement.EnumerateArray()
                )
                {
                    string id =
                        item.TryGetProperty(
                            "id",
                            out var idElement
                        )
                            ? idElement.GetString() ?? ""
                            : "";

                    if (
                        string.IsNullOrWhiteSpace(id)
                    )
                    {
                        continue;
                    }

                    if (
                        !vagasPorId.TryGetValue(
                            id,
                            out var vaga
                        )
                    )
                    {
                        continue;
                    }

                    string publicacao =
                        item.TryGetProperty(
                            "time",
                            out var timeElement
                        )
                            ? timeElement.GetString() ?? ""
                            : "";

                    string status =
                        item.TryGetProperty(
                            "status",
                            out var statusElement
                        )
                            ? statusElement.GetString() ?? ""
                            : "";

                    if (
                        !string.IsNullOrWhiteSpace(
                            publicacao
                        )
                    )
                    {
                        vaga.Publicacao =
                            publicacao.Trim();
                    }

                    if (
                        !string.IsNullOrWhiteSpace(
                            status
                        )
                    )
                    {
                        vaga.StatusLinkedIn =
                            status.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Aviso ao enriquecer status/publicação: " +
                    $"{ex.Message}"
                );
            }

            // ============================================================
            // 8. RESULTADO FINAL
            // ============================================================

            var resultado =
                vagasPorId.Values.ToList();

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine(
                $"TOTAL INFORMADO PELO LINKEDIN: " +
                $"{totalResultados}"
            );
            Console.WriteLine(
                $"TOTAL DE VAGAS COLETADAS: " +
                $"{resultado.Count}"
            );
            Console.WriteLine("==========================================");

            return resultado;
        }

    }
}