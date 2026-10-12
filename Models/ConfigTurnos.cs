namespace FigaroBarbershop.Models
{
    public static class ConfigTurnos
    {
        public const int MinutosPorSlot = 30;
        public const decimal SenaFija = 5000m;

        public static int CalcularSlots(int duracionMinutos)
            => (int)Math.Ceiling(duracionMinutos / (double)MinutosPorSlot);

        public static decimal CalcularSena(decimal precioServicio)
            => Math.Min(SenaFija, precioServicio);
    }
}