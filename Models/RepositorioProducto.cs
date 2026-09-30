using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioProducto : RepositorioBase, IRepositorioProducto
    {
        public RepositorioProducto(IConfiguration configuration) : base(configuration) { }

        public int Alta(Producto p)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"INSERT INTO Producto ({nameof(Producto.Nombre)}, {nameof(Producto.Precio)}, {nameof(Producto.ImagenUrl)}, {nameof(Producto.Activo)})
                         VALUES (@{nameof(Producto.Nombre)}, @{nameof(Producto.Precio)}, @{nameof(Producto.ImagenUrl)}, 1);
                         SELECT LAST_INSERT_ID();";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Producto.Nombre), p.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Producto.Precio), p.Precio);
            command.Parameters.AddWithValue("@" + nameof(Producto.ImagenUrl), (object?)p.ImagenUrl ?? DBNull.Value);

            connection.Open();
            p.IdProducto = Convert.ToInt32(command.ExecuteScalar());
            return p.IdProducto;
        }

        public int Baja(Producto p)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Producto SET {nameof(Producto.Activo)} = 0 WHERE {nameof(Producto.IdProducto)} = @{nameof(Producto.IdProducto)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Producto.IdProducto), p.IdProducto);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        // Reactivar usa la misma lógica que Baja, pero en 1. La agregamos
        // como método propio (no forma parte de IRepositorio<T>) porque
        // solo Producto la necesita por ahora.
        public int Reactivar(Producto p)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Producto SET {nameof(Producto.Activo)} = 1 WHERE {nameof(Producto.IdProducto)} = @{nameof(Producto.IdProducto)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Producto.IdProducto), p.IdProducto);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public int Modificacion(Producto p)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"UPDATE Producto SET
                            {nameof(Producto.Nombre)} = @{nameof(Producto.Nombre)},
                            {nameof(Producto.Precio)} = @{nameof(Producto.Precio)},
                            {nameof(Producto.ImagenUrl)} = @{nameof(Producto.ImagenUrl)}
                         WHERE {nameof(Producto.IdProducto)} = @{nameof(Producto.IdProducto)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@" + nameof(Producto.Nombre), p.Nombre);
            command.Parameters.AddWithValue("@" + nameof(Producto.Precio), p.Precio);
            command.Parameters.AddWithValue("@" + nameof(Producto.ImagenUrl), (object?)p.ImagenUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@" + nameof(Producto.IdProducto), p.IdProducto);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        // Delega al overload con filtro, trayendo solo activos por defecto
        public IList<Producto> ObtenerLista(int paginaNro = 1, int tamPagina = 10)
            => ObtenerLista(paginaNro, tamPagina, activo: true);

        public IList<Producto> ObtenerLista(int paginaNro, int tamPagina, bool? activo)
        {
            var lista = new List<Producto>();
            using var connection = new MySqlConnection(connectionString);
            var sql = @"SELECT * FROM Producto
                        WHERE (@activo IS NULL OR Activo = @activo)
                        ORDER BY Nombre LIMIT @tam OFFSET @offset";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@activo", (object?)activo ?? DBNull.Value);
            command.Parameters.AddWithValue("@tam", tamPagina);
            command.Parameters.AddWithValue("@offset", (paginaNro - 1) * tamPagina);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearProducto(reader));
            }
            return lista;
        }

        public int ObtenerCantidad() => ObtenerCantidad(activo: true);

        public int ObtenerCantidad(bool? activo)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = "SELECT COUNT(*) FROM Producto WHERE (@activo IS NULL OR Activo = @activo)";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@activo", (object?)activo ?? DBNull.Value);

            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public Producto? ObtenerPorId(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Producto WHERE {nameof(Producto.IdProducto)} = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapearProducto(reader);
            }
            return null;
        }

        private static Producto MapearProducto(MySqlDataReader reader)
        {
            return new Producto
            {
                IdProducto = reader.GetInt32(nameof(Producto.IdProducto)),
                Nombre = reader.GetString(nameof(Producto.Nombre)),
                Precio = reader.GetDecimal(nameof(Producto.Precio)),
                ImagenUrl = reader.IsDBNull(reader.GetOrdinal(nameof(Producto.ImagenUrl))) ? null : reader.GetString(nameof(Producto.ImagenUrl)),
                Activo = reader.GetBoolean(nameof(Producto.Activo)),
            };
        }
    }
}