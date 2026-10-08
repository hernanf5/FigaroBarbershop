using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioCliente : RepositorioBase, IRepositorioCliente
    {
        public RepositorioCliente(IConfiguration configuration) : base(configuration) { }

        public int Alta(Cliente c)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"INSERT INTO Cliente ({nameof(Cliente.Nombre)}, {nameof(Cliente.Apellido)}, {nameof(Cliente.Email)}, {nameof(Cliente.Telefono)})
                         VALUES (@{nameof(Cliente.Nombre)}, @{nameof(Cliente.Apellido)}, @{nameof(Cliente.Email)}, @{nameof(Cliente.Telefono)});
                         SELECT LAST_INSERT_ID();";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Nombre), c.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Apellido), c.Apellido);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Email), c.Email.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@" + nameof(Cliente.Telefono), c.Telefono);

            connection.Open();
            c.IdCliente = Convert.ToInt32(command.ExecuteScalar());
            return c.IdCliente;
        }

        public int Modificacion(Cliente c)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"UPDATE Cliente SET
                            {nameof(Cliente.Nombre)} = @{nameof(Cliente.Nombre)},
                            {nameof(Cliente.Apellido)} = @{nameof(Cliente.Apellido)},
                            {nameof(Cliente.Telefono)} = @{nameof(Cliente.Telefono)}
                         WHERE {nameof(Cliente.IdCliente)} = @{nameof(Cliente.IdCliente)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Nombre), c.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Apellido), c.Apellido);
            command.Parameters.AddWithValue("@" + nameof(Cliente.Telefono), c.Telefono);
            command.Parameters.AddWithValue("@" + nameof(Cliente.IdCliente), c.IdCliente);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public Cliente? ObtenerPorId(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Cliente WHERE {nameof(Cliente.IdCliente)} = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() ? MapearCliente(reader) : null;
        }

        public Cliente? ObtenerPorEmail(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Cliente WHERE {nameof(Cliente.Email)} = @email";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());

            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() ? MapearCliente(reader) : null;
        }

        private static Cliente MapearCliente(MySqlDataReader reader)
        {
            return new Cliente
            {
                IdCliente = reader.GetInt32(nameof(Cliente.IdCliente)),
                Nombre = reader.GetString(nameof(Cliente.Nombre)),
                Apellido = reader.GetString(nameof(Cliente.Apellido)),
                Email = reader.GetString(nameof(Cliente.Email)),
                Telefono = reader.GetString(nameof(Cliente.Telefono)),
            };
        }
    }
}