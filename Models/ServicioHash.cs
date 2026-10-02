using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FigaroBarbershop.Models
{
    public class ServicioHash
    {
        private readonly string salt;

        public ServicioHash(IConfiguration configuration)
        {
            salt = configuration["Salt"]
                ?? throw new InvalidOperationException("No se encontró la clave 'Salt' en la configuración.");
        }

        public string Hashear(string clave)
        {
            var hash = KeyDerivation.Pbkdf2(
                password: clave,
                salt: System.Text.Encoding.UTF8.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            );
            return Convert.ToBase64String(hash);
        }

        public bool Verificar(string claveIngresada, string hashGuardado)
            => Hashear(claveIngresada) == hashGuardado;
    }
}