using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class Agenda
    {
        public int IdAgenda { get; set; }

        public int IdUsuario { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan HoraInicio { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan HoraFin { get; set; }

        public string Estado { get; set; } = "Disponible"; // Disponible | No disponible | Reservado

        // Navegación, se completa al hacer JOIN con Usuario
        public Usuario? Barbero { get; set; }
    }
}