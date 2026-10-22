using System.Security.Claims;
using FigaroBarbershop.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FigaroBarbershop.Controllers.Api
{
    public class CambiarEstadoRequest
    {
        public string Estado { get; set; } = "";
    }

    [ApiController]
    [Route("api/turnos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TurnosApiController : ControllerBase
    {
        private readonly IRepositorioTurno repositorio;
        private readonly IRepositorioUsuario repoUsuario;
        private readonly ILogger<TurnosApiController> logger;

        public TurnosApiController(IRepositorioTurno repositorio, IRepositorioUsuario repoUsuario,
            ILogger<TurnosApiController> logger)
        {
            this.repositorio = repositorio;
            this.repoUsuario = repoUsuario;
            this.logger = logger;
        }

        private int IdUsuarioActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool EsAdmin => User.IsInRole("Administrador");

        [HttpGet]
        public IActionResult Listar([FromQuery] TurnoFiltro filtro, int pagina = 1, int tamPagina = 10)
        {
            if (pagina < 1) pagina = 1;
            if (tamPagina < 1 || tamPagina > 50) tamPagina = 10;

            // Un barbero solo ve su propia agenda
            if (!EsAdmin) filtro.IdBarbero = IdUsuarioActual;

            var total = repositorio.ObtenerCantidad(filtro);
            var items = repositorio.ObtenerLista(filtro, pagina, tamPagina);
            return Ok(new
            {
                total,
                pagina,
                tamPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamPagina),
                items
            });
        }

        [HttpGet("{id:int}")]
        public IActionResult Obtener(int id)
        {
            var turno = repositorio.ObtenerDetalle(id);
            if (turno == null) return NotFound();
            if (!EsAdmin && turno.IdBarbero != IdUsuarioActual) return Forbid();
            return Ok(turno);
        }

        [HttpPut("{id:int}/estado")]
        public IActionResult CambiarEstado(int id, [FromBody] CambiarEstadoRequest req)
        {
            var turno = repositorio.ObtenerDetalle(id);
            if (turno == null) return NotFound();
            if (!EsAdmin && turno.IdBarbero != IdUsuarioActual) return Forbid();

            try
            {
                repositorio.CambiarEstado(id, req.Estado);
                return Ok(repositorio.ObtenerDetalle(id));
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al cambiar estado del turno {Id}", id);
                return StatusCode(500, new { error = "Error interno." });
            }
        }

        // Buscador de barberos para el filtro (solo admin)
        [HttpGet("barberos")]
        [Authorize(Roles = "Administrador", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Barberos(string? q)
        {
            var lista = repoUsuario.ObtenerPorRol("Barbero")
                .Select(u => new { u.IdUsuario, nombre = $"{u.Nombre} {u.Apellido}" });

            if (!string.IsNullOrWhiteSpace(q))
                lista = lista.Where(b => b.nombre.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase));

            return Ok(lista.Take(10));
        }
    }
}