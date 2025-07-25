﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoNomina.Backend.Data;
using ProyectoNomina.Backend.Services;
using ProyectoNomina.Shared.Models.DTOs;

namespace ProyectoNomina.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,RRHH")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ReporteService _reporteService;

        public ReportesController(AppDbContext context, ReporteService reporteService)
        {
            _context = context;
            _reporteService = reporteService;
        }

        [HttpGet("Nominas")]
        public async Task<ActionResult<IEnumerable<ReporteNominaDto>>> ObtenerReporteNominas()
        {
            var nominas = await _context.Nominas
                .Include(n => n.Detalles)
                .ToListAsync();

            if (!nominas.Any())
                return NotFound("No se encontraron nóminas registradas.");

            var reporte = nominas.Select(n => new ReporteNominaDto
            {
                NominaId = n.Id,
                Descripcion = n.Descripcion,
                FechaGeneracion = n.FechaGeneracion,
                TotalSalarios = n.Detalles.Sum(d => d.SalarioBruto),
                TotalBonificaciones = n.Detalles.Sum(d => d.Bonificaciones),
                TotalDeducciones = n.Detalles.Sum(d => d.Deducciones),
                TotalNeto = n.Detalles.Sum(d => d.SalarioNeto)
            }).ToList();

            return Ok(reporte);
        }

        [HttpGet("Expedientes")]
        public async Task<ActionResult<IEnumerable<ReporteExpedienteDto>>> ObtenerReporteExpedientes()
        {
            var empleados = await _context.Empleados.ToListAsync();
            var tiposRequeridos = await _context.TiposDocumento
                .Where(t => t.EsRequerido)
                .Select(t => t.Id)
                .ToListAsync();

            var reporte = new List<ReporteExpedienteDto>();

            foreach (var emp in empleados)
            {
                var entregados = await _context.DocumentosEmpleado
                    .Where(d => d.EmpleadoId == emp.Id)
                    .Select(d => d.TipoDocumentoId)
                    .Distinct()
                    .ToListAsync();

                var faltantes = tiposRequeridos.Except(entregados).ToList();

                var estado = faltantes.Count == 0 ? "Completo"
                           : entregados.Count == 0 ? "Incompleto"
                           : "En proceso";

                reporte.Add(new ReporteExpedienteDto
                {
                    Empleado = emp.NombreCompleto,
                    EstadoExpediente = estado,
                    DocumentosRequeridos = tiposRequeridos.Count,
                    DocumentosPresentados = entregados.Count,
                    DocumentosFaltantes = faltantes.Count
                });
            }

            return Ok(reporte);
        }

        [HttpGet("Expedientes/pdf")]
        public async Task<IActionResult> GenerarPdfExpedientes()
        {
            var empleados = await _context.Empleados.ToListAsync();
            var tiposRequeridos = await _context.TiposDocumento
                .Where(t => t.EsRequerido)
                .Select(t => t.Id)
                .ToListAsync();

            var reporte = new List<ReporteExpedienteDto>();

            foreach (var emp in empleados)
            {
                var entregados = await _context.DocumentosEmpleado
                    .Where(d => d.EmpleadoId == emp.Id)
                    .Select(d => d.TipoDocumentoId)
                    .Distinct()
                    .ToListAsync();

                var faltantes = tiposRequeridos.Except(entregados).ToList();

                var estado = faltantes.Count == 0 ? "Completo"
                           : entregados.Count == 0 ? "Incompleto"
                           : "En proceso";

                reporte.Add(new ReporteExpedienteDto
                {
                    Empleado = emp.NombreCompleto,
                    EstadoExpediente = estado,
                    DocumentosRequeridos = tiposRequeridos.Count,
                    DocumentosPresentados = entregados.Count,
                    DocumentosFaltantes = faltantes.Count
                });
            }

            var pdf = _reporteService.GenerarReporteExpediente(reporte);
            return File(pdf, "application/pdf", "ReporteExpediente.pdf");
        }

        [HttpGet("InformacionAcademica/pdf")]
        public async Task<IActionResult> GenerarReporteInformacionAcademicaPdf()
        {
            var datos = await _context.InformacionAcademica.Include(i => i.Empleado).ToListAsync();

            if (!datos.Any())
                return NotFound("No hay registros de información académica.");

            var pdf = _reporteService.GenerarReporteInformacionAcademica(datos);
            return File(pdf, "application/pdf", "ReporteInformacionAcademica.pdf");
        }

        [HttpGet("Ajustes/pdf")]
        public async Task<IActionResult> GenerarReporteAjustesPdf()
        {
            var ajustes = await _context.AjustesManuales.Include(a => a.Empleado).ToListAsync();

            if (!ajustes.Any())
                return NotFound("No hay ajustes manuales registrados.");

            var pdf = _reporteService.GenerarReporteAjustesManuales(ajustes);
            return File(pdf, "application/pdf", "ReporteAjustes.pdf");
        }

        [HttpGet("Auditoria/pdf")]
        public async Task<IActionResult> GenerarReporteAuditoriaPdf()
        {
            var auditoria = await _context.Auditoria.ToListAsync();

            if (!auditoria.Any())
                return NotFound("No hay registros de auditoría.");

            var pdf = _reporteService.GenerarReporteAuditoria(auditoria);
            return File(pdf, "application/pdf", "ReporteAuditoria.pdf");
        }

        [HttpGet("PorTipoDocumento")]
        public async Task<ActionResult<List<ItemDocumentoDto>>> ObtenerReportePorTipoDocumento()
        {
            var resultado = await _context.TiposDocumento
                .Select(td => new ItemDocumentoDto
                {
                    Tipo = td.Nombre,
                    TotalRequeridos = td.DocumentosEmpleados.Count(),
                    Entregados = td.DocumentosEmpleados.Count(d => d.RutaArchivo != null),
                    Faltantes = td.DocumentosEmpleados.Count(d => d.RutaArchivo == null)
                }).ToListAsync();

            return Ok(resultado);
        }

        [HttpGet("DocumentosPorEmpleado")]
        public async Task<ActionResult<List<ReporteDocumentosEmpleadoDto>>> ObtenerDocumentosPorEmpleado()
        {
            var empleados = await _context.Empleados
                .Include(e => e.Documentos)
                .ThenInclude(d => d.TipoDocumento)
                .ToListAsync();

            var resultado = empleados.Select(e => new ReporteDocumentosEmpleadoDto
            {
                NombreEmpleado = e.NombreCompleto,
                Documentos = e.Documentos.Select(d => new ItemDocumentoResumenDto
                {
                    Tipo = d.TipoDocumento.Nombre,
                    Fecha = d.FechaSubida
                }).ToList()
            }).ToList();

            return Ok(resultado);
        }
    }
}
