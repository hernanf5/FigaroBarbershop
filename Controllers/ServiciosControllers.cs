using Microsoft.AspNetCore.Mvc;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly IRepositorioServicio repositorio;
        private readonly ILogger<ServiciosController> logger;

        public ServiciosController(IRepositorioServicio repositorio, ILogger<ServiciosController> logger)
        {
            this.repositorio = repositorio;
            this.logger = logger;
        }

        public IActionResult Index(int pagina = 1)
        {
            try
            {
                const int tamPagina = 10;
                var lista = repositorio.ObtenerLista(pagina, tamPagina);
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling(repositorio.ObtenerCantidad() / (double)tamPagina);
                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al listar servicios");
                TempData["Error"] = "Ocurrió un error al obtener el listado de servicios.";
                return View(new List<Servicio>());
            }
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Servicio servicio)
        {
            if (!ModelState.IsValid) return View(servicio);
            try
            {
                repositorio.Alta(servicio);
                TempData["Mensaje"] = "Servicio creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear servicio");
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el servicio.");
                return View(servicio);
            }
        }

        public IActionResult Edit(int id)
        {
            var servicio = repositorio.ObtenerPorId(id);
            if (servicio == null) return NotFound();
            return View(servicio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Servicio servicio)
        {
            if (id != servicio.IdServicio) return NotFound();
            if (!ModelState.IsValid) return View(servicio);
            try
            {
                repositorio.Modificacion(servicio);
                TempData["Mensaje"] = "Servicio actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al editar servicio {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el servicio.");
                return View(servicio);
            }
        }

        public IActionResult Eliminar(int id)
        {
            var servicio = repositorio.ObtenerPorId(id);
            if (servicio == null) return NotFound();
            return View(servicio);
        }

        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarConfirmado(int id)
        {
            try
            {
                var servicio = repositorio.ObtenerPorId(id);
                if (servicio == null) return NotFound();
                repositorio.Baja(servicio);
                TempData["Mensaje"] = "Servicio dado de baja correctamente.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al dar de baja servicio {Id}", id);
                TempData["Error"] = "Ocurrió un error al dar de baja el servicio.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}