using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class LoginView
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Clave { get; set; } = null!;
    }
}