using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers
{
    [Authorize]
    public class AgendaController : Controller
    {
        private readonly IRepositorioAgenda repositorio;
        private readonly IRepositorioUsuario repositorioUsuario;
        private readonly ILogger<AgendaController> logger;

        public AgendaController(IRepositorioAgenda repositorio, IRepositorioUsuario repositorioUsuario, ILogger<AgendaController> logger)
        {
            this.repositorio = repositorio;
            this.repositorioUsuario = repositorioUsuario;
            this.logger = logger;
        }

        // Vista semanal. idUsuario: si no se pasa, se usa el del usuario logueado.
        // Un Barbero solo puede ver/editar la suya; el Administrador puede elegir cualquiera.
        public IActionResult Index(int? idUsuario, DateTime? semana)
        {
            var idLogueado = ObtenerIdUsuarioLogueado();
            var esAdmin = User.IsInRole("Administrador");

            var idObjetivo = idUsuario ?? idLogueado;
            if (!esAdmin && idObjetivo != idLogueado)
            {
                return Forbid();
            }

            var inicioSemana = ObtenerLunes(semana ?? DateTime.Today);
            var finSemana = inicioSemana.AddDays(6);

            var slots = repositorio.ObtenerPorBarberoYRango(idObjetivo, inicioSemana, finSemana);

            ViewBag.IdUsuario = idObjetivo;
            ViewBag.InicioSemana = inicioSemana;
            ViewBag.EsAdmin = esAdmin;

            if (esAdmin)
            {
                // Para el selector de barbero en la vista (admin puede ver la agenda de cualquiera)
                ViewBag.Barberos = repositorioUsuario.ObtenerLista(1, 100);
            }

            return View(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generar(int idUsuario, int semanas = 4)
        {
            var idLogueado = ObtenerIdUsuarioLogueado();
            var esAdmin = User.IsInRole("Administrador");
            if (!esAdmin && idUsuario != idLogueado)
            {
                return Forbid();
            }

            try
            {
                var desde = DateTime.Today;
                var hasta = desde.AddDays(7 * semanas);
                repositorio.GenerarSlots(idUsuario, desde, hasta);
                TempData["Mensaje"] = $"Agenda generada para las próximas {semanas} semanas.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al generar slots de agenda para usuario {Id}", idUsuario);
                TempData["Error"] = "Ocurrió un error al generar la agenda.";
            }

            return RedirectToAction(nameof(Index), new { idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int idAgenda, string nuevoEstado)
        {
            var idLogueado = ObtenerIdUsuarioLogueado();
            var esAdmin = User.IsInRole("Administrador");

            var slot = repositorio.ObtenerPorId(idAgenda);
            if (slot == null) return NotFound();

            if (!esAdmin && slot.IdUsuario != idLogueado)
            {
                return Forbid();
            }

            // No se puede bloquear una franja ya reservada por un cliente:
            // primero hay que cancelar el turno asociado (lo resolvemos cuando tengamos Turno).
            if (slot.Estado == "Reservado")
            {
                TempData["Error"] = "No se puede modificar una franja reservada. Cancelá el turno primero.";
                return RedirectToAction(nameof(Index), new { idUsuario = slot.IdUsuario });
            }

            if (nuevoEstado != "Disponible" && nuevoEstado != "No disponible")
            {
                TempData["Error"] = "Estado inválido.";
                return RedirectToAction(nameof(Index), new { idUsuario = slot.IdUsuario });
            }

            repositorio.CambiarEstado(idAgenda, nuevoEstado);
            return RedirectToAction(nameof(Index), new { idUsuario = slot.IdUsuario });
        }

        private int ObtenerIdUsuarioLogueado()
            => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        private static DateTime ObtenerLunes(DateTime fecha)
        {
            int diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
            return fecha.AddDays(-diff).Date;
        }
    }
}