using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioAgenda : RepositorioBase, IRepositorioAgenda
    {
        private static readonly TimeSpan HoraApertura = new(9, 0, 0);
        private static readonly TimeSpan HoraCierre = new(20, 0, 0);
        private static readonly TimeSpan DuracionSlot = TimeSpan.FromMinutes(30);

        public RepositorioAgenda(IConfiguration configuration) : base(configuration) { }

        public void GenerarSlots(int idUsuario, DateTime fechaDesde, DateTime fechaHasta)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            for (var fecha = fechaDesde.Date; fecha <= fechaHasta.Date; fecha = fecha.AddDays(1))
            {
                // Domingo cerrado
                if (fecha.DayOfWeek == DayOfWeek.Sunday) continue;

                for (var hora = HoraApertura; hora < HoraCierre; hora += DuracionSlot)
                {
                    var horaFin = hora + DuracionSlot;

                    // Idempotencia: no insertar si ya existe esa franja para ese barbero/fecha/hora
                    var sqlExiste = $@"SELECT COUNT(*) FROM Agenda
                                        WHERE {nameof(Agenda.IdUsuario)} = @IdUsuario
                                        AND {nameof(Agenda.Fecha)} = @Fecha
                                        AND {nameof(Agenda.HoraInicio)} = @HoraInicio";
                    using var cmdExiste = new MySqlCommand(sqlExiste, connection);
                    cmdExiste.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmdExiste.Parameters.AddWithValue("@Fecha", fecha);
                    cmdExiste.Parameters.AddWithValue("@HoraInicio", hora);
                    var existe = Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0;
                    if (existe) continue;

                    var sqlInsert = $@"INSERT INTO Agenda ({nameof(Agenda.IdUsuario)}, {nameof(Agenda.Fecha)}, {nameof(Agenda.HoraInicio)}, {nameof(Agenda.HoraFin)}, {nameof(Agenda.Estado)})
                                        VALUES (@IdUsuario, @Fecha, @HoraInicio, @HoraFin, 'Disponible')";
                    using var cmdInsert = new MySqlCommand(sqlInsert, connection);
                    cmdInsert.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmdInsert.Parameters.AddWithValue("@Fecha", fecha);
                    cmdInsert.Parameters.AddWithValue("@HoraInicio", hora);
                    cmdInsert.Parameters.AddWithValue("@HoraFin", horaFin);
                    cmdInsert.ExecuteNonQuery();
                }
            }
        }

        public IList<Agenda> ObtenerPorBarberoYRango(int idUsuario, DateTime fechaDesde, DateTime fechaHasta)
        {
            var lista = new List<Agenda>();
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"SELECT * FROM Agenda
                        WHERE {nameof(Agenda.IdUsuario)} = @IdUsuario
                        AND {nameof(Agenda.Fecha)} BETWEEN @Desde AND @Hasta
                        ORDER BY {nameof(Agenda.Fecha)}, {nameof(Agenda.HoraInicio)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            command.Parameters.AddWithValue("@Desde", fechaDesde.Date);
            command.Parameters.AddWithValue("@Hasta", fechaHasta.Date);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearAgenda(reader));
            }
            return lista;
        }

        public IList<Agenda> ObtenerDisponiblesPorBarberoYFecha(int idUsuario, DateTime fecha)
        {
            var lista = new List<Agenda>();
            using var connection = new MySqlConnection(connectionString);
            var sql = $@"SELECT * FROM Agenda
                        WHERE {nameof(Agenda.IdUsuario)} = @IdUsuario
                        AND {nameof(Agenda.Fecha)} = @Fecha
                        AND {nameof(Agenda.Estado)} = 'Disponible'
                        ORDER BY {nameof(Agenda.HoraInicio)}";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            command.Parameters.AddWithValue("@Fecha", fecha.Date);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearAgenda(reader));
            }
            return lista;
        }

        public Agenda? ObtenerPorId(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"SELECT * FROM Agenda WHERE {nameof(Agenda.IdAgenda)} = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapearAgenda(reader);
            }
            return null;
        }

        public int CambiarEstado(int idAgenda, string nuevoEstado)
        {
            using var connection = new MySqlConnection(connectionString);
            var sql = $"UPDATE Agenda SET {nameof(Agenda.Estado)} = @Estado WHERE {nameof(Agenda.IdAgenda)} = @Id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Estado", nuevoEstado);
            command.Parameters.AddWithValue("@Id", idAgenda);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        private static Agenda MapearAgenda(MySqlDataReader reader)
        {
            return new Agenda
            {
                IdAgenda = reader.GetInt32(nameof(Agenda.IdAgenda)),
                IdUsuario = reader.GetInt32(nameof(Agenda.IdUsuario)),
                Fecha = reader.GetDateTime(nameof(Agenda.Fecha)),
                HoraInicio = reader.GetTimeSpan(nameof(Agenda.HoraInicio)),
                HoraFin = reader.GetTimeSpan(nameof(Agenda.HoraFin)),
                Estado = reader.GetString(nameof(Agenda.Estado)),
            };
        }
    }
}