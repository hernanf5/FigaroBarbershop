namespace FigaroBarbershop.Models
{
    public interface IRepositorioAgenda
    {
        void GenerarSlots(int idUsuario, DateTime fechaDesde, DateTime fechaHasta);
        IList<Agenda> ObtenerPorBarberoYRango(int idUsuario, DateTime fechaDesde, DateTime fechaHasta);
        IList<Agenda> ObtenerDisponiblesPorBarberoYFecha(int idUsuario, DateTime fecha);
        Agenda? ObtenerPorId(int id);
        int CambiarEstado(int idAgenda, string nuevoEstado);
    }
}