using JobApply.Data;
using JobApply.Models;
using JobApply.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace JobApply.Controllers
{
    [Authorize]
    public class VagasController : Controller
    {
        private readonly AppDbContext _context;

        public VagasController(AppDbContext context)
        {
            _context = context;
        }       

        public async Task<IActionResult> Index(string? busca, string? status)
        {
            var query = _context.Vagas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(v =>
                    v.Titulo.Contains(busca) ||
                    v.Empresa.Contains(busca));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var vagas = await query
                .OrderByDescending(v => v.Id)
                .ToListAsync();

            ViewBag.Busca = busca;
            ViewBag.Status = status;

            return View(vagas);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Vaga vaga)
        {
            _context.Vagas.Add(vaga);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vaga = await _context.Vagas.FindAsync(id);

            if (vaga == null)
            {
                return NotFound();
            }

            return View(vaga);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Vaga vaga)
        {
            var vagaExistente = await _context.Vagas.FindAsync(vaga.Id);

            if (vagaExistente == null)
            {
                return NotFound();
            }

            vagaExistente.Titulo = vaga.Titulo;
            vagaExistente.Empresa = vaga.Empresa;
            vagaExistente.Localizacao = vaga.Localizacao;
            vagaExistente.Link = vaga.Link;
            vagaExistente.Status = vaga.Status;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vaga = await _context.Vagas.FindAsync(id);

            if (vaga == null)
            {
                return NotFound();
            }

            _context.Vagas.Remove(vaga);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}