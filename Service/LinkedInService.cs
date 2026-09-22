using Microsoft.Playwright;
using JobApply.Models;

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


        // =================================================
        // Buscar Vagas Async
        // =================================================
        public async Task<List<VagaLinkedIn>> BuscarVagasAsync(
    string termo,
    string? localizacao,
    string? periodo)
        {
            var pagina = await ObterPaginaAsync();

            var vagas = new List<VagaLinkedIn>();
            var idsEncontrados = new HashSet<string>();

            // ============================================================
            // PERÍODO
            // ============================================================

            string periodoFiltro = periodo?.Trim().ToLowerInvariant() switch
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

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("       INICIANDO BUSCA NO LINKEDIN");
            Console.WriteLine("==========================================");
            Console.WriteLine($"TERMO: [{termo}]");
            Console.WriteLine($"LOCALIZAÇÃO: [{localizacao}]");
            Console.WriteLine($"PERÍODO: [{periodo}]");
            Console.WriteLine($"FILTRO LINKEDIN: [{periodoFiltro}]");

            const string geoIdSaoPaulo = "105871508";

            // 4 páginas = até 100 cards analisados
            const int maxPaginas = 4;
            const int vagasPorPagina = 25;

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
        "front end",
        "front-end",
        "frontend",
        "back end",
        "back-end",
        "backend",

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

        "suporte técnico",
        "suporte ti",
        "suporte de ti",
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

        "inteligência artificial",
        "artificial intelligence",
        "machine learning",
        "ia",

        "lgpd",
        "privacidade",
        "dpo",
        "governança de dados",
        "governança de ti",
        "governança corporativa e ti"
    };

            // ============================================================
            // NORMALIZA TEXTO
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

            // ============================================================
            // VERIFICA PALAVRA/EXPRESSÃO SEM CASAR SUBSTRING ERRADA
            //
            // Exemplo:
            // "ti" NÃO casa com "logistica"
            // "ti" CASA com "estágio em TI"
            // ============================================================

            bool ContemTermo(string texto, string termoTI)
            {
                string textoNormalizado = Normalizar(texto);
                string termoNormalizado = Normalizar(termoTI);

                if (string.IsNullOrWhiteSpace(termoNormalizado))
                    return false;

                string padrao =
                    $@"(?<![\p{{L}}\p{{N}}])" +
                    System.Text.RegularExpressions.Regex.Escape(
                        termoNormalizado
                    ) +
                    $@"(?![\p{{L}}\p{{N}}])";

                return System.Text.RegularExpressions.Regex.IsMatch(
                    textoNormalizado,
                    padrao
                );
            }

            // ============================================================
            // IDENTIFICA SE A BUSCA É RELACIONADA A TI
            // ============================================================

            bool buscaRelacionadaATI =
                termosTI.Any(
                    termoTI =>
                        ContemTermo(termo, termoTI)
                )
                ||
                Normalizar(termo).Contains("estagio");

            // ============================================================
            // PÁGINAS
            // ============================================================

            for (int paginaNumero = 0;
                 paginaNumero < maxPaginas;
                 paginaNumero++)
            {
                int start =
                    paginaNumero * vagasPorPagina;

                string url =
                    "https://www.linkedin.com/jobs/search/?" +
                    $"keywords={Uri.EscapeDataString(termo)}" +
                    $"&location={Uri.EscapeDataString(localizacao ?? "São Paulo")}" +
                    $"&geoId={geoIdSaoPaulo}" +
                    $"&f_TPR={periodoFiltro}" +
                    $"&start={start}";

                Console.WriteLine();
                Console.WriteLine("------------------------------------------");
                Console.WriteLine(
                    $"PÁGINA {paginaNumero + 1}/{maxPaginas}"
                );
                Console.WriteLine(url);

                try
                {
                    await pagina.GotoAsync(
                        url,
                        new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded,
                            Timeout = 30000
                        }
                    );

                    var cards =
                        pagina.Locator(
                            "li[data-occludable-job-id]"
                        );

                    try
                    {
                        await cards.First.WaitForAsync(
                            new LocatorWaitForOptions
                            {
                                State = WaitForSelectorState.Attached,
                                Timeout = 10000
                            }
                        );
                    }
                    catch
                    {
                        Console.WriteLine(
                            "Nenhum card encontrado."
                        );

                        continue;
                    }

                    await pagina.WaitForTimeoutAsync(1000);

                    int quantidadeCards =
                        await cards.CountAsync();

                    Console.WriteLine(
                        $"Cards encontrados: {quantidadeCards}"
                    );

                    // ========================================================
                    // PROCESSAMENTO
                    // ========================================================

                    for (int i = 0;
                         i < quantidadeCards;
                         i++)
                    {
                        try
                        {
                            var card =
                                cards.Nth(i);

                            // ------------------------------------------------
                            // ID DO CARD
                            // ------------------------------------------------

                            string id =
                                await card.GetAttributeAsync(
                                    "data-occludable-job-id"
                                ) ?? "";

                            var container =
                                card.Locator(
                                    "div[data-job-id]"
                                ).First;

                            if (await container.CountAsync() > 0)
                            {
                                string? idInterno =
                                    await container.GetAttributeAsync(
                                        "data-job-id"
                                    );

                                if (!string.IsNullOrWhiteSpace(idInterno))
                                {
                                    id = idInterno;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(id))
                            {
                                continue;
                            }

                            if (idsEncontrados.Contains(id))
                            {
                                continue;
                            }

                            // ------------------------------------------------
                            // LÊ O CARD INTEIRO DE UMA VEZ
                            //
                            // Isso evita dezenas de chamadas Playwright
                            // para cada card.
                            // ------------------------------------------------

                            string jsonCard =
                                await card.EvaluateAsync<string>(
                                    @"el => {
                                const link =
                                    el.querySelector(
                                        ""a[href*='/jobs/view/']""
                                    );

                                const titulo =
                                    (
                                        link?.getAttribute(
                                            ""aria-label""
                                        )
                                        || el.querySelector(
                                            "".artdeco-entity-lockup__title""
                                        )?.innerText
                                        || el.querySelector(
                                            ""strong""
                                        )?.innerText
                                        || """"
                                    )
                                    .replace(
                                        "" with verification"",
                                        """"
                                    )
                                    .trim();

                                const empresa =
                                    (
                                        el.querySelector(
                                            "".artdeco-entity-lockup__subtitle span""
                                        )?.innerText
                                        || """"
                                    ).trim();

                                const local =
                                    (
                                        el.querySelector(
                                            "".job-card-container__metadata-wrapper li""
                                        )?.innerText
                                        || """"
                                    ).trim();

                                const publicacao =
                                    (
                                        el.querySelector(
                                            ""time""
                                        )?.innerText
                                        || """"
                                    ).trim();

                                const status =
                                    (
                                        el.querySelector(
                                            "".job-card-container__footer-job-state""
                                        )?.innerText
                                        || """"
                                    ).trim();

                                return JSON.stringify({
                                    titulo,
                                    empresa,
                                    local,
                                    publicacao,
                                    status
                                });
                            }"
                                );

                            using var documento =
                                System.Text.Json.JsonDocument.Parse(
                                    jsonCard
                                );

                            var root =
                                documento.RootElement;

                            string titulo =
                                root.TryGetProperty(
                                    "titulo",
                                    out var tituloElement
                                )
                                    ? tituloElement.GetString() ?? ""
                                    : "";

                            string empresa =
                                root.TryGetProperty(
                                    "empresa",
                                    out var empresaElement
                                )
                                    ? empresaElement.GetString() ?? ""
                                    : "";

                            string local =
                                root.TryGetProperty(
                                    "local",
                                    out var localElement
                                )
                                    ? localElement.GetString() ?? ""
                                    : "";

                            string publicacao =
                                root.TryGetProperty(
                                    "publicacao",
                                    out var publicacaoElement
                                )
                                    ? publicacaoElement.GetString() ?? ""
                                    : "";

                            string statusLinkedIn =
                                root.TryGetProperty(
                                    "status",
                                    out var statusElement
                                )
                                    ? statusElement.GetString() ?? ""
                                    : "";

                            if (string.IsNullOrWhiteSpace(titulo))
                            {
                                Console.WriteLine(
                                    $"Card {i + 1}: título não encontrado."
                                );

                                continue;
                            }

                            // ------------------------------------------------
                            // FILTRO DE RELEVÂNCIA
                            // ------------------------------------------------

                            bool ehTI =
                                termosTI.Any(
                                    termoTI =>
                                        ContemTermo(
                                            titulo,
                                            termoTI
                                        )
                                );

                            if (
                                buscaRelacionadaATI
                                &&
                                !ehTI
                            )
                            {
                                Console.WriteLine(
                                    $"DESCARTADA POR RELEVÂNCIA: {titulo}"
                                );

                                continue;
                            }

                            // ------------------------------------------------
                            // LINK
                            //
                            // Usa o próprio ID.
                            // Não depende do <a> existir.
                            // ------------------------------------------------

                            string link =
                                $"https://www.linkedin.com/jobs/view/{id}/";

                            idsEncontrados.Add(id);

                            // ------------------------------------------------
                            // LOG
                            // ------------------------------------------------

                            Console.WriteLine(
                                $"VAGA: {titulo} | " +
                                $"EMPRESA: {empresa} | " +
                                $"LOCAL: {local} | " +
                                $"PUBLICAÇÃO: {publicacao} | " +
                                $"STATUS: {statusLinkedIn}"
                            );

                            // ------------------------------------------------
                            // RESULTADO
                            // ------------------------------------------------

                            vagas.Add(
                                new VagaLinkedIn
                                {
                                    Titulo = titulo,
                                    Empresa = empresa,
                                    Localizacao = local,
                                    Link = link,
                                    Publicacao = publicacao,
                                    StatusLinkedIn = statusLinkedIn
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"Erro no card {i + 1}: " +
                                $"{ex.Message}"
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erro na página {paginaNumero + 1}: " +
                        $"{ex.Message}"
                    );
                }
            }

            // ============================================================
            // REMOVE DUPLICADAS
            // ============================================================

            vagas =
                vagas
                    .GroupBy(v => v.Link)
                    .Select(g => g.First())
                    .ToList();

            // ============================================================
            // RESULTADO
            // ============================================================

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine(
                $"TOTAL FINAL DE VAGAS: {vagas.Count}"
            );
            Console.WriteLine("==========================================");

            return vagas;
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
    }
}