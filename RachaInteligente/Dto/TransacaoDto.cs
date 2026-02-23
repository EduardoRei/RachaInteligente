namespace RachaInteligente.Dto
{
    public class TransacaoDto
    {
        public string QuemPaga { get; set; } = string.Empty;
        public string QuemRecebe { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}
