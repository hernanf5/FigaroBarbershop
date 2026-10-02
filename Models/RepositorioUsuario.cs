using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioUsuario : RepositorioBase, IRepositorioUsuario
    {
        private readonly ServicioHash servicioHash;

        public RepositorioUsuario(IConfiguration configuration, ServicioHash servicioHash) : base(configuration)
        {
            this.servicioHash = servicioHash;
        }

        public int Alta(Usuario u)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"INSERT INTO Usuario ({nameof(Usuario.Nombre)}, {nameof(Usuario.Apellido)}, {nameof(Usuario.Email)}, Clave, {nameof(Usuario.Rol)}, {nameof(Usuario.AvatarUrl)}, {nameof(Usuario.Activo)})
                         VALUES (@{nameof(Usuario.Nombre)}, @{nameof(Usuario.Apellido)}, @{nameof(Usuario.Email)}, @Clave, @{nameof(Usuario.Rol)}, @{nameof(Usuario.AvatarUrl)}, 1);
                         SELECT LAST_INSERT_ID();";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Nombre), u.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Apellido), u.Apellido);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Email), u.Email);
            command.Parameters.AddWithValue("@Clave", servicioHash.Hashear(u.Clave!));
            command.Parameters.AddWithValue("@" + nameof(Usuario.Rol), u.Rol);
            command.Parameters.AddWithValue("@" + nameof(Usuario.AvatarUrl), (object?)u.AvatarUrl ?? DBNull.Value);

            connection.Open();
            u.IdUsuario = Convert.ToInt32(command.ExecuteScalar());
            return u.IdUsuario;
        }

        public int Baja(Usuario u)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Usuario SET {nameof(Usuario.Activo)} = 0 WHERE {nameof(Usuario.IdUsuario)} = @{nameof(Usuario.IdUsuario)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Usuario.IdUsuario), u.IdUsuario);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        // No cambia Clave ni Rol: eso lo maneja un flujo aparte (cambio de contraseña / gestión de roles por admin)
        public int Modificacion(Usuario u)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"UPDATE Usuario SET
                            {nameof(Usuario.Nombre)} = @{nameof(Usuario.Nombre)},
                            {nameof(Usuario.Apellido)} = @{nameof(Usuario.Apellido)},
                            {nameof(Usuario.Email)} = @{nameof(Usuario.Email)},
                            {nameof(Usuario.AvatarUrl)} = @{nameof(Usuario.AvatarUrl)}
                         WHERE {nameof(Usuario.IdUsuario)} = @{nameof(Usuario.IdUsuario)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Nombre), u.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Apellido), u.Apellido);
            command.Parameters.AddWithValue("@" + nameof(Usuario.Email), u.Email);
            command.Parameters.AddWithValue("@" + nameof(Usuario.AvatarUrl), (object?)u.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@" + nameof(Usuario.IdUsuario), u.IdUsuario);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public int CambiarClave(int idUsuario, string nuevaClave)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Usuario SET Clave = @Clave WHERE {nameof(Usuario.IdUsuario)} = @{nameof(Usuario.IdUsuario)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Clave", servicioHash.Hashear(nuevaClave));
            command.Parameters.AddWithValue("@" + nameof(Usuario.IdUsuario), idUsuario);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public IList<Usuario> ObtenerLista(int paginaNro = 1, int tamPagina = 10)
        {
            var lista = new List<Usuario>();
            using var connection = new MySqlConnection(connectionString);
            var sql = "SELECT * FROM Usuario WHERE Activo = 1 ORDER BY Apellido, Nombre LIMIT @tam OFFSET @offset";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tam", tamPagina);
            command.Parameters.AddWithValue("@offset", (paginaNro - 1) * tamPagina);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearUsuario(reader));
            }
            return lista;
        }

        public int ObtenerCantidad()
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = "SELECT COUNT(*) FROM Usuario WHERE Activo = 1";
            using var command = new MySqlCommand(sql, connection);

            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public Usuario? ObtenerPorId(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Usuario WHERE {nameof(Usuario.IdUsuario)} = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapearUsuario(reader);
            }
            return null;
        }

        public Usuario? ObtenerPorEmail(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Usuario WHERE {nameof(Usuario.Email)} = @email AND {nameof(Usuario.Activo)} = 1";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var u = MapearUsuario(reader);
                u.Clave = reader.GetString("Clave"); // acá sí traemos el hash, para validar login
                return u;
            }
            return null;
        }

        private static Usuario MapearUsuario(MySqlDataReader reader)
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(nameof(Usuario.IdUsuario)),
                Nombre = reader.GetString(nameof(Usuario.Nombre)),
                Apellido = reader.GetString(nameof(Usuario.Apellido)),
                Email = reader.GetString(nameof(Usuario.Email)),
                Rol = reader.GetString(nameof(Usuario.Rol)),
                AvatarUrl = reader.IsDBNull(reader.GetOrdinal(nameof(Usuario.AvatarUrl))) ? null : reader.GetString(nameof(Usuario.AvatarUrl)),
                Activo = reader.GetBoolean(nameof(Usuario.Activo)),
            };
        }
    }
}