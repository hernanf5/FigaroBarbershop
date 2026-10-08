namespace FigaroBarbershop.Models
{
    public interface IRepositorioCliente
    {
        int Alta(Cliente c);
        int Modificacion(Cliente c);
        Cliente? ObtenerPorId(int id);
        Cliente? ObtenerPorEmail(string email);
    }
}