namespace FigaroBarbershop.Models
{
    public class TurnoFiltro
    {
        public string? Estado { get; set; }
        public int? IdBarbero { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Q { get; set; }   // busca por nombre, apellido o email del cliente
    }
}