namespace FigaroBarbershop.Models
{
    public interface IRepositorioProducto : IRepositorio<Producto>
    {
        IList<Producto> ObtenerLista(int paginaNro, int tamPagina, bool? activo);
        int ObtenerCantidad(bool? activo);
        int Reactivar(Producto p);
    }
}