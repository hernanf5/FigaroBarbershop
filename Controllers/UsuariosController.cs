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
        public IActionResult Login() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            var usuario = repositorio.ObtenerPorEmail(modelo.Email);
            if (usuario == null || !servicioHash.Verificar(modelo.Clave, usuario.Clave!))
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
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

            return RedirectToAction("Index", "Home");
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
        public IActionResult Perfil(Usuario usuario)
        {
            var id = ObtenerIdUsuarioLogueado();
            if (id != usuario.IdUsuario) return Forbid();
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                if (usuario.AvatarFile != null)
                {
                    usuario.AvatarUrl = GuardarAvatar(usuario.AvatarFile, usuario.IdUsuario);
                }
                else
                {
                    usuario.AvatarUrl = repositorio.ObtenerPorId(id)?.AvatarUrl;
                }
                repositorio.Modificacion(usuario);
                TempData["Mensaje"] = "Perfil actualizado correctamente.";
                return RedirectToAction(nameof(Perfil));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al actualizar perfil {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el perfil.");
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