using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers
{
    [AllowAnonymous]
    public class ReservarController : Controller
    {
        private readonly IRepositorioUsuario repoUsuario;
        private readonly IRepositorioServicio repoServicio;
        private readonly IRepositorioAgenda repoAgenda;
        private readonly IRepositorioCliente repoCliente;
        private readonly IRepositorioTurno repoTurno;
        private readonly ServicioDisponibilidad disponibilidad;
        private readonly ILogger<ReservarController> logger;

        public ReservarController(IRepositorioUsuario repoUsuario, IRepositorioServicio repoServicio,
            IRepositorioAgenda repoAgenda, IRepositorioCliente repoCliente, IRepositorioTurno repoTurno,
            ServicioDisponibilidad disponibilidad, ILogger<ReservarController> logger)
        {
            this.repoUsuario = repoUsuario;
            this.repoServicio = repoServicio;
            this.repoAgenda = repoAgenda;
            this.repoCliente = repoCliente;
            this.repoTurno = repoTurno;
            this.disponibilidad = disponibilidad;
            this.logger = logger;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Barberos()
            => Json(repoUsuario.ObtenerPorRol("Barbero")
                .Select(u => new { u.IdUsuario, nombre = $"{u.Nombre} {u.Apellido}", u.AvatarUrl }));

        [HttpGet]
        public IActionResult Servicios()
            => Json(repoServicio.ObtenerLista(1, 100)
                .Select(s => new { s.IdServicio, s.Nombre, s.DuracionMinutos, s.Precio, sena = ConfigTurnos.CalcularSena(s.Precio) }));

        [HttpGet]
        public IActionResult Horarios(int idBarbero, DateTime fecha, int idServicio)
        {
            var servicio = repoServicio.ObtenerPorId(idServicio);
            if (servicio == null || !servicio.Activo) return NotFound();
            return Json(disponibilidad.ObtenerInicios(idBarbero, fecha, servicio));
        }

        // Autocompletado por email. Es público, así que NO devuelve el teléfono completo
        [HttpGet]
        public IActionResult BuscarCliente(string email)
        {
            var c = string.IsNullOrWhiteSpace(email) ? null : repoCliente.ObtenerPorEmail(email);
            if (c == null) return Json(new { existe = false });

            var terminacion = c.Telefono.Length >= 4 ? c.Telefono[^4..] : c.Telefono;
            return Json(new { existe = true, nombre = c.Nombre, apellido = c.Apellido, telefonoTermina = terminacion });
        }

        [HttpPost]
        public IActionResult Confirmar([FromBody] ReservaRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(new { error = "Revisá los datos ingresados." });

            try
            {
                var servicio = repoServicio.ObtenerPorId(req.IdServicio);
                if (servicio == null || !servicio.Activo) return BadRequest(new { error = "Servicio inválido." });

                var slot = repoAgenda.ObtenerPorId(req.IdAgenda);
                if (slot == null) return BadRequest(new { error = "Horario inválido." });
                if (slot.Fecha.Date + slot.HoraInicio <= DateTime.Now)
                    return BadRequest(new { error = "El horario elegido ya pasó." });

                var cliente = repoCliente.ObtenerPorEmail(req.Email);
                if (cliente == null)
                {
                    if (string.IsNullOrWhiteSpace(req.Telefono))
                        return BadRequest(new { error = "El teléfono es obligatorio." });

                    cliente = new Cliente { Nombre = req.Nombre, Apellido = req.Apellido, Email = req.Email, Telefono = req.Telefono };
                    repoCliente.Alta(cliente);
                }
                else
                {
                    cliente.Nombre = req.Nombre;
                    cliente.Apellido = req.Apellido;
                    if (!string.IsNullOrWhiteSpace(req.Telefono)) cliente.Telefono = req.Telefono;
                    repoCliente.Modificacion(cliente);
                }

                var turno = new Turno
                {
                    IdAgenda = req.IdAgenda,
                    IdCliente = cliente.IdCliente,
                    IdServicio = servicio.IdServicio,
                    CantidadSlots = ConfigTurnos.CalcularSlots(servicio.DuracionMinutos),
                    PrecioServicio = servicio.Precio,
                    Sena = ConfigTurnos.CalcularSena(servicio.Precio),
                };
                repoTurno.Reservar(turno);

                return Json(new { ok = true, idTurno = turno.IdTurno, sena = turno.Sena });
            }
            catch (InvalidOperationException ex)
            {
                // Horario tomado por otra persona entre que lo eligió y confirmó
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al confirmar reserva");
                return StatusCode(500, new { error = "Ocurrió un error al confirmar la reserva." });
            }
        }
    }
}