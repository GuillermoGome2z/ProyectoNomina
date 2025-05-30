﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoNomina.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using ProyectoNomina.Shared.Models.DTOs;

namespace ProyectoNomina.Backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin,RRHH")]
    [Route("api/[controller]")]
    public class ValidacionExpedienteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ValidacionExpedienteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ValidacionExpediente/5
        [HttpGet("{empleadoId}")]
        public async Task<ActionResult<ValidacionExpedienteDto>> VerificarExpediente(int empleadoId)
        {
            var empleado = await _context.Empleados.FindAsync(empleadoId);
            if (empleado == null)
                return NotFound("Empleado no encontrado.");

            var tiposRequeridos = await _context.TiposDocumento
                .Where(t => t.EsRequerido)
                .Select(t => t.Id)
                .ToListAsync();

            var documentosEntregados = await _context.DocumentosEmpleado
                .Where(d => d.EmpleadoId == empleadoId)
                .Select(d => d.TipoDocumentoId)
                .Distinct()
                .ToListAsync();

            var faltantes = tiposRequeridos.Except(documentosEntregados).ToList();

            var estado = faltantes.Count == 0 ? "Completo"
                        : documentosEntregados.Count == 0 ? "Incompleto"
                        : "En proceso";

            return Ok(new ValidacionExpedienteDto
            {
                Empleado = empleado.NombreCompleto,
                EstadoExpediente = estado,
                DocumentosRequeridos = tiposRequeridos.Count,
                DocumentosPresentados = documentosEntregados.Count,
                DocumentosFaltantes = faltantes.Count
            });
        }
    }
}
