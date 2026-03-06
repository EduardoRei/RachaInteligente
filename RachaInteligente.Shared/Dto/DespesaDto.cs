namespace RachaInteligente.Shared.Dto;

public class DespesaDto
{
    public string Descricao { get; set; } = string.Empty;

    public DateTime? Data { get; set; }

    public decimal Valor { get; set; }

    public string PagoPor { get; set; } = string.Empty;

    public int QuantidadePessoas { get; set; }

    public string Nomes { get; set; } = string.Empty;

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
