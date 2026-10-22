namespace FigaroBarbershop.Models
{
    public class TurnoListado
    {
        public int IdTurno { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public int IdBarbero { get; set; }
        public string Barbero { get; set; } = "";
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = "";
        public string Email { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Servicio { get; set; } = "";
        public int CantidadSlots { get; set; }
        public decimal PrecioServicio { get; set; }
        public decimal Sena { get; set; }
        public string Estado { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
    }
}