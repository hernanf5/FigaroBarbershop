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
        private readonly IRepositorioServicio repoServicio;
        private readonly IRepositorioCliente repoCliente;
        private readonly IRepositorioAgenda repoAgenda;
        private readonly ServicioDisponibilidad disponibilidad;
        private readonly ILogger<TurnosApiController> logger;

        public TurnosApiController(IRepositorioTurno repositorio, IRepositorioUsuario repoUsuario,
            IRepositorioServicio repoServicio, IRepositorioCliente repoCliente, IRepositorioAgenda repoAgenda,
            ServicioDisponibilidad disponibilidad, ILogger<TurnosApiController> logger)
        {
            this.repositorio = repositorio;
            this.repoUsuario = repoUsuario;
            this.repoServicio = repoServicio;
            this.repoCliente = repoCliente;
            this.repoAgenda = repoAgenda;
            this.disponibilidad = disponibilidad;
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

        [HttpPost]
        public IActionResult Crear([FromBody] ReservaRequest req)
        {
            try
            {
                var servicio = repoServicio.ObtenerPorId(req.IdServicio);
                if (servicio == null || !servicio.Activo)
                    return BadRequest(new { error = "Servicio inválido." });

                var slot = repoAgenda.ObtenerPorId(req.IdAgenda);
                if (slot == null) return BadRequest(new { error = "Horario inválido." });
                if (!EsAdmin && slot.IdUsuario != IdUsuarioActual) return Forbid();
                if (slot.Fecha.Date + slot.HoraInicio <= DateTime.Now)
                    return BadRequest(new { error = "El horario elegido ya pasó." });

                var email = req.Email.Trim();
                var cliente = repoCliente.ObtenerPorEmail(email);
                if (cliente == null)
                {
                    if (string.IsNullOrWhiteSpace(req.Telefono))
                        return BadRequest(new { error = "El teléfono es obligatorio para un cliente nuevo." });

                    repoCliente.Alta(new Cliente
                    {
                        Nombre = req.Nombre.Trim(),
                        Apellido = req.Apellido.Trim(),
                        Email = email,
                        Telefono = req.Telefono.Trim()
                    });
                    cliente = repoCliente.ObtenerPorEmail(email)!;
                }
                else
                {
                    cliente.Nombre = req.Nombre.Trim();
                    cliente.Apellido = req.Apellido.Trim();
                    if (!string.IsNullOrWhiteSpace(req.Telefono)) cliente.Telefono = req.Telefono.Trim();
                    repoCliente.Modificacion(cliente);
                }

                var turno = new Turno
                {
                    IdAgenda = req.IdAgenda,
                    IdCliente = cliente.IdCliente,
                    IdServicio = servicio.IdServicio,
                    CantidadSlots = ConfigTurnos.CalcularSlots(servicio.DuracionMinutos),
                    PrecioServicio = servicio.Precio,
                    Sena = ConfigTurnos.CalcularSena(servicio.Precio)
                };
                repositorio.Reservar(turno);

                return CreatedAtAction(nameof(Obtener), new { id = turno.IdTurno }, repositorio.ObtenerDetalle(turno.IdTurno));
            }
            catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear turno");
                return StatusCode(500, new { error = "Error interno." });
            }
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

        // Auxiliares para el formulario de alta 

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

        [HttpGet("clientes")]
        public IActionResult Clientes(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Ok(Array.Empty<object>());
            return Ok(repoCliente.Buscar(q, 8)
                .Select(c => new { c.IdCliente, c.Nombre, c.Apellido, c.Email, c.Telefono }));
        }

        [HttpGet("servicios")]
        public IActionResult Servicios()
            => Ok(repoServicio.ObtenerLista(1, 100)
                .Select(s => new { s.IdServicio, s.Nombre, s.DuracionMinutos, s.Precio }));

        [HttpGet("horarios")]
        public IActionResult Horarios(int idBarbero, DateTime fecha, int idServicio)
        {
            if (!EsAdmin) idBarbero = IdUsuarioActual;
            var servicio = repoServicio.ObtenerPorId(idServicio);
            if (servicio == null || !servicio.Activo) return NotFound();
            return Ok(disponibilidad.ObtenerInicios(idBarbero, fecha, servicio));
        }
    }
}