namespace FigaroBarbershop.Models
{
    public interface IRepositorioUsuario : IRepositorio<Usuario>
    {
        Usuario? ObtenerPorEmail(string email);
        int CambiarClave(int idUsuario, string nuevaClave);
    }
}