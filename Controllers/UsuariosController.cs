using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IRepositorioUsuario repositorio;
        private readonly ServicioHash servicioHash;
        private readonly ILogger<UsuariosController> logger;
        private readonly IWebHostEnvironment env;

        public UsuariosController(IRepositorioUsuario repositorio, ServicioHash servicioHash,
            ILogger<UsuariosController> logger, IWebHostEnvironment env)
        {
            this.repositorio = repositorio;
            this.servicioHash = servicioHash;
            this.logger = logger;
            this.env = env;
        }


        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Si ya está logueado, no tiene sentido mostrar el login
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Turnos");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView modelo, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(modelo);
            }

            var usuario = repositorio.ObtenerPorEmail(modelo.Email);
            if (usuario == null || !servicioHash.Verificar(modelo.Clave, usuario.Clave!))
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                ViewBag.ReturnUrl = returnUrl;
                return View(modelo);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim("NombreCompleto", $"{usuario.Nombre} {usuario.Apellido}"),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("AvatarUrl", usuario.AvatarUrl ?? string.Empty),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Vuelve a la página que quería ver (solo rutas locales, para evitar redirecciones a sitios externos)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Turnos");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ── ABM (solo Administrador) ──

        [Authorize(Policy = "Administrador")]
        public IActionResult Index(int pagina = 1)
        {
            const int tamPagina = 10;
            var lista = repositorio.ObtenerLista(pagina, tamPagina);
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(repositorio.ObtenerCantidad() / (double)tamPagina);
            return View(lista);
        }

        [Authorize(Policy = "Administrador")]
        public IActionResult Create() => View();

        [Authorize(Policy = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Clave))
                ModelState.AddModelError(nameof(Usuario.Clave), "La contraseña es obligatoria para un usuario nuevo.");

            if (!ModelState.IsValid) return View(usuario);

            try
            {
                repositorio.Alta(usuario);
                TempData["Mensaje"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear usuario");
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el usuario (¿el email ya existe?).");
                return View(usuario);
            }
        }

        [Authorize(Policy = "Administrador")]
        public IActionResult Eliminar(int id)
        {
            var usuario = repositorio.ObtenerPorId(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [Authorize(Policy = "Administrador")]
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarConfirmado(int id)
        {
            var usuario = repositorio.ObtenerPorId(id);
            if (usuario == null) return NotFound();
            repositorio.Baja(usuario);
            TempData["Mensaje"] = "Usuario dado de baja correctamente.";
            return RedirectToAction(nameof(Index));
        }


        [Authorize]
        public IActionResult Perfil()
        {
            var id = ObtenerIdUsuarioLogueado();
            var usuario = repositorio.ObtenerPorId(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(Usuario usuario)
        {
            var id = ObtenerIdUsuarioLogueado();
            if (id != usuario.IdUsuario) return Forbid();
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                // Se parte del usuario real de la base y solo se copian los campos que el perfil puede cambiar.
                // Así Rol, Activo y Clave no se pueden alterar desde el formulario.
                var actual = repositorio.ObtenerPorId(id);
                if (actual == null) return NotFound();

                actual.Nombre = usuario.Nombre;
                actual.Apellido = usuario.Apellido;
                actual.Email = usuario.Email;
                if (usuario.AvatarFile != null)
                {
                    actual.AvatarUrl = GuardarAvatar(usuario.AvatarFile, actual.IdUsuario);
                }

                repositorio.Modificacion(actual);

                // Se renueva la sesión para que el menú muestre el nombre y el avatar nuevos
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, actual.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, actual.Email),
                    new Claim("NombreCompleto", $"{actual.Nombre} {actual.Apellido}"),
                    new Claim(ClaimTypes.Role, actual.Rol),
                    new Claim("AvatarUrl", actual.AvatarUrl ?? string.Empty),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["Mensaje"] = "Perfil actualizado correctamente.";
                return RedirectToAction(nameof(Perfil));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al actualizar perfil {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el perfil (¿el email ya está en uso?).");
                return View(usuario);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarClave(string claveActual, string claveNueva)
        {
            var id = ObtenerIdUsuarioLogueado();
            var usuario = repositorio.ObtenerPorEmail(User.Identity!.Name!);
            if (usuario == null || !servicioHash.Verificar(claveActual, usuario.Clave!))
            {
                TempData["Error"] = "La contraseña actual no es correcta.";
                return RedirectToAction(nameof(Perfil));
            }

            repositorio.CambiarClave(id, claveNueva);
            TempData["Mensaje"] = "Contraseña actualizada correctamente.";
            return RedirectToAction(nameof(Perfil));
        }



        private int ObtenerIdUsuarioLogueado()
            => int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        private string GuardarAvatar(IFormFile archivo, int idUsuario)
        {
            var carpeta = Path.Combine(env.WebRootPath, "uploads", "avatares");
            Directory.CreateDirectory(carpeta);
            var nombreArchivo = $"avatar_{idUsuario}{Path.GetExtension(archivo.FileName)}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            archivo.CopyTo(stream);

            return $"/uploads/avatares/{nombreArchivo}";
        }
    }
}