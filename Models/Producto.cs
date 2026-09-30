using System.ComponentModel.DataAnnotations;

namespace FigaroBarbershop.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 9999999, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [Display(Name = "Imagen")]
        public string? ImagenUrl { get; set; }

        // No mapeado a columna: para recibir el archivo subido en el form
        public IFormFile? ImagenFile { get; set; }

        public bool Activo { get; set; } = true;

        public override string ToString() => Nombre;
    }
}