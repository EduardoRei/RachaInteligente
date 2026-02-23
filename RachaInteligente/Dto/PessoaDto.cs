namespace RachaInteligente.Dto
{
    public class PessoaDto
    {
        public string Nome { get; set; } = string.Empty;
        public List<DevedorDto> Devedores { get; set; } = new List<DevedorDto>();
    }
}
