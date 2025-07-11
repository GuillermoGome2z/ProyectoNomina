﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoNomina.Backend.Data;
using ProyectoNomina.Backend.Models;
using ProyectoNomina.Shared.Models.DTOs;

namespace ProyectoNomina.Backend.Controllers
{
    [Authorize(Roles = "Admin,RRHH")]
    [ApiController]
    [Route("api/[controller]")]
    public class DepartamentosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartamentosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Departamentos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartamentoDto>>> GetDepartamentos()
        {
            var lista = await _context.Departamentos
                .Select(d => new DepartamentoDto
                {
                    Id = d.Id,
                    Nombre = d.Nombre
                })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/Departamentos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartamentoDto>> GetDepartamento(int id)
        {
            var departamento = await _context.Departamentos.FindAsync(id);

            if (departamento == null)
                return NotFound(new { mensaje = "Departamento no encontrado." });

            return new DepartamentoDto
            {
                Id = departamento.Id,
                Nombre = departamento.Nombre
            };
        }

        // POST: api/Departamentos
        [HttpPost]
        public async Task<ActionResult> PostDepartamento([FromBody] DepartamentoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(new { mensaje = "El nombre del departamento es obligatorio." });

            var existe = await _context.Departamentos.AnyAsync(d => d.Nombre == dto.Nombre);
            if (existe)
                return BadRequest(new { mensaje = "Ya existe un departamento con ese nombre." });

            var nuevo = new Departamento
            {
                Nombre = dto.Nombre
            };

            _context.Departamentos.Add(nuevo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/Departamentos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartamento(int id, [FromBody] DepartamentoDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { mensaje = "ID de departamento no válido." });

            var departamento = await _context.Departamentos.FindAsync(id);
            if (departamento == null)
                return NotFound(new { mensaje = "Departamento no encontrado." });

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(new { mensaje = "El nombre del departamento es obligatorio." });

            departamento.Nombre = dto.Nombre;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/Departamentos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartamento(int id)
        {
            var departamento = await _context.Departamentos
                                             .Include(d => d.Empleados)
                                             .FirstOrDefaultAsync(d => d.Id == id);

            if (departamento == null)
                return NotFound(new { mensaje = "Departamento no encontrado." });

            if (departamento.Empleados.Any())
                return BadRequest(new { mensaje = "El departamento no se puede eliminar porque tiene empleados asociados." });

            _context.Departamentos.Remove(departamento);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
