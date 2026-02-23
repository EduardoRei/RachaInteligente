namespace RachaInteligente.Dto;

public class DespesaDto
{
    public string Descricao { get; set; }

    public DateTime? Data { get; set; }

    public decimal Valor { get; set; }

    public string PagoPor { get; set; }

    public int QuantidadePessoas { get; set; }

    public string Nomes { get; set; }

    public List<DevedorDto> Devedores { get; private set; } = new List<DevedorDto>();

    public decimal CalcularValorUnitario() => QuantidadePessoas > 0 ? Valor / QuantidadePessoas : 0;
    
    public void AdicionarDevedor(string nome, decimal valor)
    {
        Devedores.Add(new DevedorDto
        {
            Nome = nome,
            Valor = valor
        });
    }
}
