using Microsoft.AspNetCore.Mvc;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers
{
    public class ProductosController : Controller
    {
        private readonly IRepositorioProducto repositorio;
        private readonly ILogger<ProductosController> logger;
        private readonly IWebHostEnvironment env;

        public ProductosController(IRepositorioProducto repositorio, ILogger<ProductosController> logger, IWebHostEnvironment env)
        {
            this.repositorio = repositorio;
            this.logger = logger;
            this.env = env;
        }

        public IActionResult Index(int pagina = 1, bool? activo = true)
        {
            try
            {
                const int tamPagina = 10;
                var lista = repositorio.ObtenerLista(pagina, tamPagina, activo);
                ViewBag.PaginaActual = pagina;
                ViewBag.FiltroActivo = activo;
                ViewBag.TotalPaginas = (int)Math.Ceiling(repositorio.ObtenerCantidad(activo) / (double)tamPagina);
                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al listar productos");
                TempData["Error"] = "Ocurrió un error al obtener el listado de productos.";
                return View(new List<Producto>());
            }
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto producto)
        {
            if (!ModelState.IsValid) return View(producto);
            try
            {
                if (producto.ImagenFile != null)
                {
                    producto.ImagenUrl = GuardarImagen(producto.ImagenFile);
                }
                repositorio.Alta(producto);
                TempData["Mensaje"] = "Producto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear producto");
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el producto.");
                return View(producto);
            }
        }

        public IActionResult Edit(int id)
        {
            var producto = repositorio.ObtenerPorId(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Producto producto)
        {
            if (id != producto.IdProducto) return NotFound();
            if (!ModelState.IsValid) return View(producto);
            try
            {
                if (producto.ImagenFile != null)
                {
                    producto.ImagenUrl = GuardarImagen(producto.ImagenFile);
                }
                else
                {
                    // conservar la imagen existente si no subieron una nueva
                    var actual = repositorio.ObtenerPorId(id);
                    producto.ImagenUrl = actual?.ImagenUrl;
                }
                repositorio.Modificacion(producto);
                TempData["Mensaje"] = "Producto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al editar producto {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el producto.");
                return View(producto);
            }
        }

        public IActionResult Eliminar(int id)
        {
            var producto = repositorio.ObtenerPorId(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarConfirmado(int id)
        {
            try
            {
                var producto = repositorio.ObtenerPorId(id);
                if (producto == null) return NotFound();
                repositorio.Baja(producto);
                TempData["Mensaje"] = "Producto dado de baja correctamente.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al dar de baja producto {Id}", id);
                TempData["Error"] = "Ocurrió un error al dar de baja el producto.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reactivar(int id)
        {
            try
            {
                var producto = repositorio.ObtenerPorId(id);
                if (producto == null) return NotFound();
                repositorio.Reactivar(producto);
                TempData["Mensaje"] = "Producto reactivado correctamente.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al reactivar producto {Id}", id);
                TempData["Error"] = "Ocurrió un error al reactivar el producto.";
            }
            return RedirectToAction(nameof(Index));
        }

        private string GuardarImagen(IFormFile archivo)
        {
            var carpeta = Path.Combine(env.WebRootPath, "uploads", "productos");
            Directory.CreateDirectory(carpeta);
            var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            archivo.CopyTo(stream);

            return $"/uploads/productos/{nombreArchivo}";
        }
    }
}