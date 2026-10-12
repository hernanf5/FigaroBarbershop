using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class ReservaRequest
    {
        [Range(1, int.MaxValue)]
        public int IdAgenda { get; set; }   // primera franja elegida

        [Range(1, int.MaxValue)]
        public int IdServicio { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = null!;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(100)]
        public string Apellido { get; set; } = null!;

        // Opcional si el cliente ya existe (se conserva el teléfono guardado)
        [StringLength(30)]
        public string? Telefono { get; set; }
    }
}