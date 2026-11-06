using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FigaroBarbershop.Models;

namespace FigaroBarbershop.Controllers;

public class HomeController : Controller
{
    private readonly IRepositorioServicio repoServicio;
    private readonly IRepositorioUsuario repoUsuario;

    public HomeController(IRepositorioServicio repoServicio, IRepositorioUsuario repoUsuario)
    {
        this.repoServicio = repoServicio;
        this.repoUsuario = repoUsuario;
    }

    [Authorize]
    public IActionResult Restringido() => View();

    [AllowAnonymous]
    public IActionResult Index()
    {
        ViewBag.Servicios = repoServicio.ObtenerLista(1, 100);
        ViewBag.Barberos = repoUsuario.ObtenerPorRol("Barbero");
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
