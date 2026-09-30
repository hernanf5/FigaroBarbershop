namespace FigaroBarbershop.Models
{
    public interface IRepositorio<T>
    {
        int Alta(T entidad);
        int Baja(T entidad);
        int Modificacion(T entidad);
        IList<T> ObtenerLista(int paginaNro = 1, int tamPagina = 10);
        int ObtenerCantidad();
        T? ObtenerPorId(int id);
    }
}