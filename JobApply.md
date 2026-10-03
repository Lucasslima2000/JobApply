# 🚀 JobApply

Sistema web desenvolvido em **C# / ASP.NET Core MVC** para pesquisar, organizar e automatizar candidaturas a vagas de emprego e estágio.

O projeto tem como foco principal a **pesquisa de vagas no LinkedIn e a automação controlada do processo de candidatura**, mantendo a interação humana quando a candidatura exigir decisões ou respostas específicas do candidato.

---

# 🎯 Objetivo atual

O JobApply deve permitir:

* Pesquisar vagas no LinkedIn;
* Filtrar vagas por termo, localização e período;
* Exibir informações encontradas das vagas;
* Identificar informações como empresa, título, localização, publicação e link;
* Abrir candidaturas pelo próprio sistema;
* Automatizar etapas simples da candidatura;
* Utilizar os dados do usuário autenticado;
* Selecionar currículos cadastrados;
* Identificar quando uma candidatura exige interação humana;
* Pausar a automação quando houver perguntas da vaga;
* Permitir que o usuário assuma o controle da candidatura;
* Continuar a automação posteriormente;
* Registrar informações importantes do processo.

O sistema deve ser preparado para trabalhar com **múltiplos usuários**, portanto dados pessoais e informações de candidatura não devem ficar fixos no código.

---

# 🛠️ Tecnologias

## Backend

* C#
* ASP.NET Core MVC
* Entity Framework Core
* LINQ
* ASP.NET Core Authentication
* Playwright

## Banco de dados

* SQL Server
* Entity Framework Core
* Migrations

## Frontend

* Razor Views
* HTML
* CSS
* Bootstrap
* JavaScript

## Automação

* Microsoft Playwright
* Chromium
* LinkedIn
* Perfil persistente do navegador para manter a sessão

## Ferramentas

* .NET SDK
* Visual Studio / VS Code
* SQL Server Management Studio
* Git / GitHub

---

# ✅ ESTADO ATUAL DO PROJETO

O projeto já possui uma aplicação ASP.NET Core MVC funcional.

Estrutura principal:

```text
JobApply
│
├── Controllers
├── Models
├── Data
├── Views
├── Services
├── wwwroot
└── Program.cs
```

A aplicação já possui:

* Login;
* Logout;
* Usuário autenticado;
* Layout autenticado;
* Página inicial;
* Página de vagas;
* Página de automação;
* Página de usuários;
* Integração com SQL Server;
* Entity Framework Core configurado;
* Serviço de integração com LinkedIn;
* Playwright;
* Perfil persistente do LinkedIn;
* Busca de vagas;
* Automação de candidatura.

---

# 🔐 AUTENTICAÇÃO

A aplicação já possui autenticação.

O usuário consegue:

* Fazer login;
* Encerrar sessão;
* Acessar páginas protegidas;
* Visualizar seu nome na aplicação.

A autenticação será utilizada também para identificar **qual usuário está executando uma candidatura**.

---

# 👤 DADOS DO USUÁRIO

Os dados utilizados durante a candidatura **não devem ficar fixos no código**.

Atualmente estamos trabalhando com dados como:

* Nome;
* E-mail;
* Telefone;
* País;
* Currículo.

A arquitetura planejada é:

```text
Usuário faz login
        ↓
Usuário autenticado
        ↓
Banco de dados
        ↓
Dados do perfil
        ↓
LinkedInService
        ↓
Preenchimento da candidatura
```

Exemplo:

```text
Nome: usuário autenticado
E-mail: usuário autenticado
Telefone: usuário autenticado
País: usuário autenticado
```

Isso permitirá que o mesmo código funcione para diferentes usuários.

---

# 🔎 BUSCA DE VAGAS NO LINKEDIN

A integração com o LinkedIn já está funcionando em nível inicial.

O sistema consegue realizar pesquisas utilizando:

* Termo da vaga;
* Localização;
* Período de publicação.

Exemplo:

```text
Termo:
estagio tecnologia

Localização:
SP

Período:
24h
```

O sistema utiliza o identificador geográfico de São Paulo:

```text
105871508
```

Filtros de período utilizados:

```text
24 horas → r86400
1 semana → r604800
1 mês → r2592000
```

A busca já consegue retornar vagas válidas encontradas no LinkedIn.

---

# 📋 INFORMAÇÕES DAS VAGAS

A aplicação consegue trabalhar com informações como:

* Título;
* Empresa;
* Localização;
* Link;
* Data/período de publicação;
* Informações relacionadas ao LinkedIn.

Exemplo:

```text
Título:
Estágio em Tecnologia

Empresa:
Empresa XYZ

Localização:
São Paulo

Publicação:
Anunciada há 21 horas

Link:
LinkedIn
```

---

# 🤖 AUTOMAÇÃO DE CANDIDATURA

Essa é atualmente uma das principais funcionalidades do projeto.

Fluxo geral:

```text
Pesquisar vagas
      ↓
Selecionar vaga
      ↓
Candidatar-se
      ↓
Abrir vaga no LinkedIn
      ↓
Verificar login
      ↓
Identificar tipo de candidatura
```

A candidatura pode seguir diferentes caminhos.

---

# 🌐 CANDIDATURA EXTERNA

Quando o LinkedIn direciona para o site da empresa, o sistema deve preservar o fluxo existente.

Exemplos de plataformas:

* Gupy;
* Workday;
* Outros sistemas externos.

O sistema já possui tratamento para diferentes situações, incluindo:

```text
LOGIN_LINKEDIN
LOGIN_SITE_EXTERNO
CANDIDATURA_EXTERNA_ABERTA
CANDIDATURA_GUPY_ABERTA
CANDIDATURA_GUPY_NAO_ENCONTRADA
```

A lógica existente de candidatura externa deve ser preservada enquanto trabalhamos na evolução da candidatura diretamente pelo LinkedIn.

---

# 💼 CANDIDATURA SIMPLIFICADA DO LINKEDIN

A candidatura simplificada do LinkedIn já foi identificada e está sendo automatizada.

Exemplo:

```text
Candidatura simplificada
        ↓
Dados básicos
        ↓
Currículo
        ↓
Perguntas da vaga
        ↓
Interação do usuário
```

---

# 👤 DADOS BÁSICOS DA CANDIDATURA

Os dados básicos devem ser preenchidos utilizando os dados cadastrados pelo usuário.

Não devemos utilizar valores fixos no código.

### Não fazer:

```csharp
telefone = "11949782363";
email = "lucasslima2000@hotmail.com";
```

### Planejado:

```text
Usuário logado
      ↓
Banco de dados
      ↓
Dados do usuário
      ↓
Candidatura LinkedIn
```

Isso é necessário para permitir múltiplos usuários.

---

# 📄 CURRÍCULO

O LinkedIn pode apresentar diferentes currículos cadastrados.

Exemplo:

```text
Currículo Lucas_07_2026.pdf
Currículo Lucas_07_2026.pdf
Curriculo_Lucas_da_Silva_Lima.pdf
```

A automação deve permitir selecionar o currículo definido pelo usuário.

O objetivo é que a etapa de currículo seja automatizada.

Fluxo:

```text
Dados básicos
      ↓
Selecionar currículo
      ↓
Avançar
```

---

# ⚠️ PERGUNTAS DA VAGA

Esta é uma regra importante da arquitetura.

**O sistema não deve responder automaticamente perguntas específicas da vaga.**

As perguntas podem variar completamente entre vagas.

Exemplos:

```text
Have you completed the following level of education:
Bachelor's Degree?

Você possui experiência com Excel?

Você possui CNH?

Possui disponibilidade para trabalho presencial?

Qual sua pretensão salarial?
```

A resposta pode depender do usuário, da vaga e do contexto.

Portanto:

```text
Pergunta específica da vaga
        ↓
PAUSAR AUTOMAÇÃO
        ↓
Usuário assume o controle
```

---

# ⚠️ INTERAÇÃO DO USUÁRIO

Quando aparecer uma pergunta da vaga, a aplicação deve apresentar uma indicação simples:

```text
⚠️ Candidatura aguardando sua interação
```

Não é necessário exibir inicialmente toda a pergunta ou todos os detalhes na tela principal.

O navegador permanecerá na candidatura para que o usuário possa responder diretamente no LinkedIn.

Depois que o usuário concluir a interação, a aplicação poderá permitir que a automação continue.

---

# 🔄 FLUXO INTELIGENTE DA CANDIDATURA

A automação **não deve depender da quantidade de etapas**.

Não podemos assumir:

```text
1 de 4
2 de 4
3 de 4
4 de 4
```

Uma candidatura pode ser:

```text
1 de 1
```

ou:

```text
1 de 2
2 de 2
```

ou:

```text
1 de 4
2 de 4
3 de 4
4 de 4
```

Ou ainda possuir etapas diferentes.

Portanto, a automação deve analisar **o conteúdo da tela atual**, e não apenas o número da etapa.

---

# 🧠 REGRA PRINCIPAL DA AUTOMAÇÃO

Fluxo planejado:

```text
LinkedIn
   ↓
Candidatura simplificada
   ↓
Dados básicos
   ↓
Currículo
   ↓
Existe pergunta da vaga?
   │
   ├── NÃO
   │    ↓
   │  continua automaticamente
   │
   └── SIM
        ↓
⚠️ Candidatura aguardando sua interação
        ↓
Usuário responde
        ↓
Usuário solicita continuação
        ↓
Automação continua
```

---

# ❌ PERGUNTAS DESCONHECIDAS

Não devemos tentar adivinhar respostas.

Se aparecer uma pergunta que o sistema não conhece:

```text
Pergunta desconhecida
        ↓
Não responder
        ↓
Não inventar resposta
        ↓
Pausar candidatura
        ↓
Solicitar interação do usuário
```

Isso evita o envio de informações incorretas.

---

# 🎯 RESPONSABILIDADE DA AUTOMAÇÃO

## A automação pode:

* Abrir a vaga;
* Verificar login;
* Preencher dados básicos cadastrados;
* Selecionar currículo;
* Avançar em etapas estruturais;
* Detectar perguntas;
* Identificar quando precisa da intervenção do usuário;
* Continuar após a interação;
* Detectar o envio da candidatura.

## A automação não deve decidir pelo usuário:

* Formação concluída;
* Experiência profissional;
* Pretensão salarial;
* Disponibilidade;
* CNH;
* Mudança de cidade;
* Autorização de trabalho;
* Respostas pessoais;
* Outras perguntas específicas do processo seletivo.

---

# 🏗️ ESTRUTURA ATUAL DO FLUXO

```text
JobApply
   │
   ├── Login
   │
   ├── Home
   │
   ├── Vagas
   │      ↓
   │   Buscar vagas
   │      ↓
   │   LinkedIn
   │
   ├── Automação
   │      ↓
   │   Candidatar-se
   │      ↓
   │   LinkedInService
   │
   └── Usuários
          ↓
       Perfil / dados
```

---

# 🔧 LINKEDINSERVICE

O `LinkedInService` é responsável pela comunicação com o navegador e pelo fluxo de automação do LinkedIn.

Principais funcionalidades:

```text
BuscarVagasAsync()
CandidatarAsync()
InspecionarCandidaturaAsync()
```

O Playwright utiliza um perfil persistente:

```text
linkedin-profile
```

Isso permite manter a sessão do LinkedIn entre execuções.

---

# 📝 INSPEÇÃO DA CANDIDATURA

Foi criada uma funcionalidade para inspecionar a página atual da candidatura.

Ela registra:

* URL;
* Título;
* Dialog;
* Inputs;
* Textareas;
* Selects;
* Botões;
* Links.

Os logs são armazenados em:

```text
bin\Debug\net10.0\logs\
```

Exemplo:

```text
candidatura-linkedin-2026-10-02-11-08-51.txt
```

Isso será importante para identificar novos formatos de candidatura sem alterar a automação às cegas.

---

# 📌 SITUAÇÃO ATUAL DA CANDIDATURA LINKEDIN

Já conseguimos chegar a diferentes etapas da candidatura simplificada.

Exemplo real identificado:

```text
1 de 4
Contact info
```

Depois:

```text
2 de 4
Currículo
```

E posteriormente:

```text
3 de 4
Additional Questions
```

Exemplo de pergunta:

```text
Have you completed the following level of education:
Bachelor's Degree?
```

Essa experiência mostrou que não devemos criar uma automação rígida baseada em etapas específicas.

---

# 🚧 PRÓXIMO PASSO DA AUTOMAÇÃO

Antes de adicionar respostas automáticas às perguntas, precisamos criar uma estrutura genérica que:

1. Analise a tela atual;
2. Identifique se é uma etapa estrutural;
3. Preencha dados básicos;
4. Selecione currículo;
5. Identifique perguntas da vaga;
6. Pause quando necessário;
7. Mostre na aplicação:

```text
⚠️ Candidatura aguardando sua interação
```

8. Permita que o usuário assuma o controle;
9. Permita continuar a automação posteriormente;
10. Detecte o encerramento/envio da candidatura.

---

# 🗃️ BANCO DE DADOS E PERFIL

O banco continuará sendo utilizado para armazenar informações dos usuários.

A estrutura deve evoluir para permitir:

```text
Usuário
 ├── Dados pessoais
 ├── Formação
 ├── Competências
 ├── Currículos
 └── Configurações da automação
```

O objetivo é evitar informações fixas no código.

---

# 📄 CURRÍCULOS

Planejamento:

```text
Usuário
   ↓
Meus Currículos
   ├── Currículo — Estágio TI
   ├── Currículo — Suporte
   └── Currículo — Desenvolvimento
```

Cada usuário poderá possuir seus próprios currículos.

Futuramente:

* Upload de PDF;
* Seleção de currículo padrão;
* Associação do currículo à candidatura.

---

# 📊 STATUS INTERNOS DA AUTOMAÇÃO

Devemos diferenciar:

### Status da vaga/candidatura no LinkedIn

Informações reais fornecidas pela plataforma.

### Status interno do JobApply

Informações relacionadas à execução da nossa automação.

Exemplo:

```text
Aguardando interação do usuário
```

Esse status significa que **o JobApply pausou a automação**, e não que o LinkedIn tenha criado esse status.

Não devemos inventar informações sobre o status real de uma candidatura no LinkedIn.

---

# 🗺️ PRÓXIMAS FUNCIONALIDADES

## Prioridade 1 — Automação LinkedIn

* [x] Busca de vagas;
* [x] Filtros de período;
* [x] Filtro de localização;
* [x] Identificação das vagas;
* [x] Abertura da candidatura;
* [x] Verificação de login;
* [x] Tratamento de candidatura externa;
* [x] Tratamento de Gupy;
* [x] Identificação de candidatura simplificada;
* [x] Preenchimento inicial da candidatura;
* [x] Seleção de currículo em desenvolvimento;
* [ ] Buscar dados do usuário no banco;
* [ ] Criar fluxo genérico de etapas;
* [ ] Detectar perguntas da vaga;
* [ ] Pausar candidatura;
* [ ] Mostrar "Candidatura aguardando sua interação";
* [ ] Permitir continuar após interação;
* [ ] Detectar conclusão da candidatura.

---

# 🔜 FUTURAS PLATAFORMAS

Depois que o fluxo do LinkedIn estiver estável:

```text
LinkedIn
   ↓
Gupy
   ↓
Vagas.com
   ↓
InfoJobs
```

Cada plataforma deverá possuir sua própria lógica de automação.

---

# 🧩 FUTURAS FUNCIONALIDADES

Depois da automação principal:

* Perfil profissional completo;
* Múltiplos currículos;
* Filtros avançados;
* Histórico de candidaturas;
* Estatísticas;
* Análise de compatibilidade;
* Gerador de apresentação;
* Integrações adicionais;
* Notificações;
* Lembretes;
* Melhorias de interface;
* Dashboard.

---

# 📈 DASHBOARD FUTURO

Exemplo:

```text
=====================================
             DASHBOARD
=====================================

Vagas encontradas
Candidaturas
Em análise
Entrevistas
Aguardando interação

=====================================
```

As informações devem ser baseadas nos dados reais armazenados pelo sistema.

---

# 🧪 TESTES

Depois da estabilização da automação:

* Testar busca de vagas;
* Testar login;
* Testar candidatura externa;
* Testar Gupy;
* Testar candidatura simplificada;
* Testar preenchimento de dados;
* Testar seleção de currículo;
* Testar perguntas;
* Testar pausa da automação;
* Testar continuação;
* Testar diferentes quantidades de etapas;
* Testar diferentes usuários.

---

# 🧹 ARQUITETURA

Conforme o projeto crescer, devemos manter a separação de responsabilidades:

```text
Controller
    ↓
Service
    ↓
Automação / regras
    ↓
Data
    ↓
SQL Server
```

Evitar colocar toda a lógica dentro dos Controllers.

O `LinkedInService` deve ficar responsável pela automação do LinkedIn, enquanto dados do usuário devem vir da camada apropriada da aplicação.

---

# 🚀 VISÃO FINAL

O JobApply deve evoluir para:

```text
Usuário faz login
       ↓
Perfil carregado
       ↓
Pesquisa vagas
       ↓
Filtra vagas
       ↓
Seleciona candidatura
       ↓
JobApply abre LinkedIn
       ↓
Automação preenche dados
       ↓
Seleciona currículo
       ↓
Analisa candidatura
       ↓
┌─────────────────────────────┐
│ Existe pergunta da vaga?    │
└──────────────┬──────────────┘
               │
        ┌──────┴──────┐
        ↓             ↓
       NÃO           SIM
        ↓             ↓
   Continua       PAUSA
   automático        ↓
                 ⚠️ Candidatura
                 aguardando
                 sua interação
                       ↓
                  Usuário assume
                       ↓
                  Usuário continua
                       ↓
                  Automação
                       ↓
               Candidatura enviada
```

---

# 🎯 OBJETIVO DO PROJETO

O JobApply deve ser um projeto pessoal que demonstre conhecimentos práticos em:

* C#;
* ASP.NET Core MVC;
* Entity Framework Core;
* SQL Server;
* MVC;
* LINQ;
* Autenticação;
* HTML/CSS;
* Bootstrap;
* JavaScript;
* Playwright;
* Automação de navegador;
* Integração com plataformas;
* Tratamento de erros;
* Arquitetura de software;
* Banco de dados;
* Git/GitHub.

O projeto também deve demonstrar preocupação com **segurança, flexibilidade, múltiplos usuários e controle humano em decisões importantes da candidatura**.

---

# 📌 REGRA PRINCIPAL DO PROJETO

> **Primeiro fazer funcionar. Depois melhorar.**

E, para a automação de candidaturas:

> **Automatizar tarefas mecânicas, mas não tomar decisões pessoais pelo usuário.**

O sistema deve reconhecer quando precisa da participação humana e devolver o controle ao usuário.
