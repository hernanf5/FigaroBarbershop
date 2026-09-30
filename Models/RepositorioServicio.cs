using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioServicio : RepositorioBase, IRepositorioServicio
    {
        public RepositorioServicio(IConfiguration configuration) : base(configuration) { }

        public int Alta(Servicio s)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"INSERT INTO Servicio ({nameof(Servicio.Nombre)}, {nameof(Servicio.DuracionMinutos)}, {nameof(Servicio.Precio)}, {nameof(Servicio.Activo)})
                        VALUES (@{nameof(Servicio.Nombre)}, @{nameof(Servicio.DuracionMinutos)}, @{nameof(Servicio.Precio)}, 1);
                        SELECT LAST_INSERT_ID();";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Servicio.Nombre), s.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Servicio.DuracionMinutos), s.DuracionMinutos);
            command.Parameters.AddWithValue("@" + nameof(Servicio.Precio), s.Precio);

            connection.Open();
            s.IdServicio = Convert.ToInt32(command.ExecuteScalar());
            return s.IdServicio;
        }

        public int Baja(Servicio s)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Servicio SET {nameof(Servicio.Activo)} = 0 WHERE {nameof(Servicio.IdServicio)} = @{nameof(Servicio.IdServicio)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Servicio.IdServicio), s.IdServicio);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public int Modificacion(Servicio s)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"UPDATE Servicio SET
                            {nameof(Servicio.Nombre)} = @{nameof(Servicio.Nombre)},
                            {nameof(Servicio.DuracionMinutos)} = @{nameof(Servicio.DuracionMinutos)},
                            {nameof(Servicio.Precio)} = @{nameof(Servicio.Precio)}
                            WHERE {nameof(Servicio.IdServicio)} = @{nameof(Servicio.IdServicio)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Servicio.Nombre), s.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Servicio.DuracionMinutos), s.DuracionMinutos);
            command.Parameters.AddWithValue("@" + nameof(Servicio.Precio), s.Precio);
            command.Parameters.AddWithValue("@" + nameof(Servicio.IdServicio), s.IdServicio);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public IList<Servicio> ObtenerLista(int paginaNro = 1, int tamPagina = 10)
        {
            var lista = new List<Servicio>();
            using var connection = new MySqlConnection(connectionString);
            var sql = @"SELECT * FROM Servicio WHERE Activo = 1 ORDER BY Nombre LIMIT @tam OFFSET @offset";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tam", tamPagina);
            command.Parameters.AddWithValue("@offset", (paginaNro - 1) * tamPagina);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearServicio(reader));
            }
            return lista;
        }

        public int ObtenerCantidad()
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = "SELECT COUNT(*) FROM Servicio WHERE Activo = 1";
            using var command = new MySqlCommand(sql, connection);

            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public Servicio? ObtenerPorId(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Servicio WHERE {nameof(Servicio.IdServicio)} = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapearServicio(reader);
            }
            return null;
        }

        private static Servicio MapearServicio(MySqlDataReader reader)
        {
            return new Servicio
            {
                IdServicio = reader.GetInt32(nameof(Servicio.IdServicio)),
                Nombre = reader.GetString(nameof(Servicio.Nombre)),
                DuracionMinutos = reader.GetInt32(nameof(Servicio.DuracionMinutos)),
                Precio = reader.GetDecimal(nameof(Servicio.Precio)),
                Activo = reader.GetBoolean(nameof(Servicio.Activo)),
            };
        }
    }
}