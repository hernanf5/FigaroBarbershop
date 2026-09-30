using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class Servicio
    {
        public int IdServicio { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La duración es obligatoria")]
        [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos")]
        [Display(Name = "Duración (min)")]
        public int DuracionMinutos { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 9999999, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        public bool Activo { get; set; } = true;

        public override string ToString() => Nombre;
    }
}