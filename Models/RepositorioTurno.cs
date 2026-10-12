using MySqlConnector;

namespace FigaroBarbershop.Models
{
    public class RepositorioTurno : RepositorioBase, IRepositorioTurno
    {
        public RepositorioTurno(IConfiguration configuration) : base(configuration) { }

        // Reserva N franjas consecutivas a partir de t.IdAgenda y crea el turno.
        // Lanza InvalidOperationException si alguna ya no está disponible.
        public int Reservar(Turno t)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                var slots = new List<(int Id, TimeSpan Inicio, TimeSpan Fin, string Estado)>();

                var sqlSlots = $@"SELECT a.{nameof(Agenda.IdAgenda)}, a.{nameof(Agenda.HoraInicio)}, a.{nameof(Agenda.HoraFin)}, a.{nameof(Agenda.Estado)}
                                  FROM Agenda a
                                  JOIN Agenda ini ON ini.{nameof(Agenda.IdAgenda)} = @IdAgenda
                                  WHERE a.{nameof(Agenda.IdUsuario)} = ini.{nameof(Agenda.IdUsuario)}
                                    AND a.{nameof(Agenda.Fecha)} = ini.{nameof(Agenda.Fecha)}
                                    AND a.{nameof(Agenda.HoraInicio)} >= ini.{nameof(Agenda.HoraInicio)}
                                  ORDER BY a.{nameof(Agenda.HoraInicio)}
                                  LIMIT @Cantidad
                                  FOR UPDATE";
                using (var cmd = new MySqlCommand(sqlSlots, connection, tx))
                {
                    cmd.Parameters.AddWithValue("@IdAgenda", t.IdAgenda);
                    cmd.Parameters.AddWithValue("@Cantidad", t.CantidadSlots);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        slots.Add((reader.GetInt32(0), reader.GetTimeSpan(1), reader.GetTimeSpan(2), reader.GetString(3)));
                    }
                }

                bool disponible = t.CantidadSlots > 0
                    && slots.Count == t.CantidadSlots
                    && slots.All(s => s.Estado == "Disponible")
                    && Enumerable.Range(1, slots.Count - 1).All(i => slots[i].Inicio == slots[i - 1].Fin);
                if (!disponible)
                {
                    throw new InvalidOperationException("El horario elegido ya no está disponible.");
                }

                foreach (var s in slots)
                {
                    var sqlUpd = $"UPDATE Agenda SET {nameof(Agenda.Estado)} = 'Reservado' WHERE {nameof(Agenda.IdAgenda)} = @Id";
                    using var cmdUpd = new MySqlCommand(sqlUpd, connection, tx);
                    cmdUpd.Parameters.AddWithValue("@Id", s.Id);
                    cmdUpd.ExecuteNonQuery();
                }

                var sqlIns = $@"INSERT INTO Turno ({nameof(Turno.IdAgenda)}, {nameof(Turno.IdCliente)}, {nameof(Turno.IdServicio)}, {nameof(Turno.CantidadSlots)}, {nameof(Turno.PrecioServicio)}, {nameof(Turno.Sena)}, {nameof(Turno.Estado)})
                                VALUES (@IdAgenda, @IdCliente, @IdServicio, @CantidadSlots, @PrecioServicio, @Sena, @Estado);
                                SELECT LAST_INSERT_ID();";
                using var cmdIns = new MySqlCommand(sqlIns, connection, tx);
                cmdIns.Parameters.AddWithValue("@IdAgenda", t.IdAgenda);
                cmdIns.Parameters.AddWithValue("@IdCliente", t.IdCliente);
                cmdIns.Parameters.AddWithValue("@IdServicio", t.IdServicio);
                cmdIns.Parameters.AddWithValue("@CantidadSlots", t.CantidadSlots);
                cmdIns.Parameters.AddWithValue("@PrecioServicio", t.PrecioServicio);
                cmdIns.Parameters.AddWithValue("@Sena", t.Sena);
                cmdIns.Parameters.AddWithValue("@Estado", EstadoTurno.Confirmado);
                t.IdTurno = Convert.ToInt32(cmdIns.ExecuteScalar());

                tx.Commit();
                return t.IdTurno;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }


        public int CambiarEstado(int idTurno, string nuevoEstado)
        {
            if (nuevoEstado is not (EstadoTurno.Finalizado or EstadoTurno.Cancelado or EstadoTurno.NoSePresento))
            {
                throw new ArgumentException("Estado inválido.", nameof(nuevoEstado));
            }

            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                int idAgenda;
                int cantidadSlots;
                string estadoActual;

                var sqlLeer = $@"SELECT {nameof(Turno.IdAgenda)}, {nameof(Turno.CantidadSlots)}, {nameof(Turno.Estado)}
                                FROM Turno WHERE {nameof(Turno.IdTurno)} = @Id FOR UPDATE";
                using (var cmdLeer = new MySqlCommand(sqlLeer, connection, tx))
                {
                    cmdLeer.Parameters.AddWithValue("@Id", idTurno);
                    using var reader = cmdLeer.ExecuteReader();
                    if (!reader.Read())
                    {
                        tx.Rollback();
                        return 0;
                    }
                    idAgenda = reader.GetInt32(0);
                    cantidadSlots = reader.GetInt32(1);
                    estadoActual = reader.GetString(2);
                }

                if (estadoActual != EstadoTurno.Confirmado)
                {
                    throw new InvalidOperationException("Solo se puede modificar un turno confirmado.");
                }

                var sqlEstado = $"UPDATE Turno SET {nameof(Turno.Estado)} = @Estado WHERE {nameof(Turno.IdTurno)} = @Id";
                int filas;
                using (var cmdEstado = new MySqlCommand(sqlEstado, connection, tx))
                {
                    cmdEstado.Parameters.AddWithValue("@Estado", nuevoEstado);
                    cmdEstado.Parameters.AddWithValue("@Id", idTurno);
                    filas = cmdEstado.ExecuteNonQuery();
                }

                if (nuevoEstado == EstadoTurno.Cancelado)
                {
                    var sqlLiberar = $@"UPDATE Agenda a
                                        JOIN Agenda ini ON ini.{nameof(Agenda.IdAgenda)} = @IdAgenda
                                        SET a.{nameof(Agenda.Estado)} = 'Disponible'
                                        WHERE a.{nameof(Agenda.IdUsuario)} = ini.{nameof(Agenda.IdUsuario)}
                                        AND a.{nameof(Agenda.Fecha)} = ini.{nameof(Agenda.Fecha)}
                                        AND a.{nameof(Agenda.HoraInicio)} >= ini.{nameof(Agenda.HoraInicio)}
                                        AND a.{nameof(Agenda.HoraInicio)} < ADDTIME(ini.{nameof(Agenda.HoraInicio)}, SEC_TO_TIME(@Segundos))
                                        AND a.{nameof(Agenda.Estado)} = 'Reservado'";
                    using var cmdLiberar = new MySqlCommand(sqlLiberar, connection, tx);
                    cmdLiberar.Parameters.AddWithValue("@IdAgenda", idAgenda);
                    cmdLiberar.Parameters.AddWithValue("@Segundos", cantidadSlots * ConfigTurnos.MinutosPorSlot * 60);
                    cmdLiberar.ExecuteNonQuery();
                }

                tx.Commit();
                return filas;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}