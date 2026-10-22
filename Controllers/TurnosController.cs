using System.Security.Claims;
using FigaroBarbershop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FigaroBarbershop.Controllers
{
    [Authorize]
    public class TurnosController : Controller
    {
        private readonly IRepositorioUsuario repoUsuario;
        private readonly ServicioJwt servicioJwt;

        public TurnosController(IRepositorioUsuario repoUsuario, ServicioJwt servicioJwt)
        {
            this.repoUsuario = repoUsuario;
            this.servicioJwt = servicioJwt;
        }

        public IActionResult Index()
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usuario = repoUsuario.ObtenerPorId(id);
            if (usuario == null) return RedirectToAction("Login", "Usuarios");

            ViewBag.Token = servicioJwt.Generar(usuario).Token;
            ViewBag.EsAdmin = User.IsInRole("Administrador");
            return View();
        }
    }
}