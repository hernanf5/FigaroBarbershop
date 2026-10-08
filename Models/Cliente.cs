using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class Cliente
    {
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; } = null!;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = null!;

        public override string ToString() => $"{Nombre} {Apellido}";
    }
}