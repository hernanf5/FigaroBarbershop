using FigaroBarbershop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FigaroBarbershop.Controllers.Api
{
    public class LoginApiRequest
    {
        public string Email { get; set; } = "";
        public string Clave { get; set; } = "";
    }

    [ApiController]
    [AllowAnonymous]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly IRepositorioUsuario repositorio;
        private readonly ServicioHash servicioHash;
        private readonly ServicioJwt servicioJwt;

        public AuthApiController(IRepositorioUsuario repositorio, ServicioHash servicioHash, ServicioJwt servicioJwt)
        {
            this.repositorio = repositorio;
            this.servicioHash = servicioHash;
            this.servicioJwt = servicioJwt;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginApiRequest req)
        {
            var usuario = repositorio.ObtenerPorEmail(req.Email);
            if (usuario == null || !servicioHash.Verificar(req.Clave, usuario.Clave!))
                return Unauthorized(new { error = "Email o contraseña incorrectos." });

            var (token, expira) = servicioJwt.Generar(usuario);
            return Ok(new
            {
                token,
                expira,
                usuario = new { nombre = $"{usuario.Nombre} {usuario.Apellido}", rol = usuario.Rol }
            });
        }
    }
}