using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public abstract class RepositorioBase
    {
        protected readonly string connectionString;

        protected RepositorioBase(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("FigaroConnection")
                ?? throw new InvalidOperationException("No se encontró la connection string 'FigaroConnection'.");
        }
    }
}