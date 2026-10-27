namespace FigaroBarbershop.Models
{
    public record HorarioDisponible(int IdAgenda, string Hora);

    public class ServicioDisponibilidad
    {
        private readonly IRepositorioAgenda repoAgenda;

        public ServicioDisponibilidad(IRepositorioAgenda repoAgenda) => this.repoAgenda = repoAgenda;

        // Horarios de inicio con N franjas consecutivas libres, según la duración del servicio
        public List<HorarioDisponible> ObtenerInicios(int idBarbero, DateTime fecha, Servicio servicio)
        {
            var ahora = DateTime.Now;
            var resultado = new List<HorarioDisponible>();
            if (fecha.Date < ahora.Date) return resultado;

            int necesarios = ConfigTurnos.CalcularSlots(servicio.DuracionMinutos);
            var libres = repoAgenda.ObtenerDisponiblesPorBarberoYFecha(idBarbero, fecha);

            for (int i = 0; i + necesarios <= libres.Count; i++)
            {
                var bloque = libres.Skip(i).Take(necesarios).ToList();
                bool contiguo = Enumerable.Range(1, necesarios - 1)
                    .All(k => bloque[k].HoraInicio == bloque[k - 1].HoraFin);
                if (!contiguo) continue;
                if (fecha.Date == ahora.Date && bloque[0].HoraInicio <= ahora.TimeOfDay) continue;

                resultado.Add(new HorarioDisponible(bloque[0].IdAgenda, bloque[0].HoraInicio.ToString(@"hh\:mm")));
            }
            return resultado;
        }
    }
}