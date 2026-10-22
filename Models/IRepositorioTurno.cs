namespace FigaroBarbershop.Models
{
    public interface IRepositorioTurno
    {
        int Reservar(Turno t);
        int CambiarEstado(int idTurno, string nuevoEstado);
        List<TurnoListado> ObtenerLista(TurnoFiltro filtro, int pagina = 1, int tamPagina = 10);
        int ObtenerCantidad(TurnoFiltro filtro);
        TurnoListado? ObtenerDetalle(int idTurno);
    }
}