using RachaInteligente.Dto;

namespace RachaInteligente.Service;

public static class PessoaService
{
    public static List<string> DefinirListaPessoas(List<DespesaDto> despesas)
    {
        var pessoasUnicas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var despesa in despesas)
        {
            pessoasUnicas.Add(despesa.PagoPor);
            foreach (var devedor in despesa.Devedores)
            {
                pessoasUnicas.Add(devedor.Nome);
            }
        }
        return pessoasUnicas.ToList();
    }
}
