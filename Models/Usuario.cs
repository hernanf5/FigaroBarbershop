using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

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

        // Solo se usa en el form de alta/cambio de contraseña, no se mapea directo a la columna
        [DataType(DataType.Password)]
        public string? Clave { get; set; }

        [Required]
        public string Rol { get; set; } = "Barbero"; // "Administrador" | "Barbero"

        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public bool Activo { get; set; } = true;

        public override string ToString() => $"{Nombre} {Apellido}";
    }
}