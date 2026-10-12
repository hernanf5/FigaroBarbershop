namespace FigaroBarbershop.Models
{
    public interface IRepositorioTurno
    {
        int Reservar(Turno t);
        int CambiarEstado(int idTurno, string nuevoEstado);
    }
}