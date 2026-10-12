namespace FigaroBarbershop.Models
{
    public class Turno
    {
        public int IdTurno { get; set; }
        public int IdAgenda { get; set; }   // primera franja que ocupa
        public int IdCliente { get; set; }
        public int IdServicio { get; set; }
        public int CantidadSlots { get; set; }
        public decimal PrecioServicio { get; set; }
        public decimal Sena { get; set; }
        public string Estado { get; set; } = EstadoTurno.Confirmado;
        public DateTime FechaCreacion { get; set; }

        // Navegación (se completa con JOIN cuando haga falta)
        public Agenda? Agenda { get; set; }
        public Cliente? Cliente { get; set; }
        public Servicio? Servicio { get; set; }
    }
}