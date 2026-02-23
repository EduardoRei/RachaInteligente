using RachaInteligente.Dto;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace RachaInteligente.Service;

public static class ArquivoService
{
    public static List<DespesaDto> ProcessarArquivo(IFormFile file)
    {
        if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return LerArquivoCsv(file);
        }
        else if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return LerArquivoJson(file);
        }
        else
        {
            throw new NotSupportedException("Formato de arquivo não suportado. Por favor, envie um arquivo CSV.");
        }
    }

    private static List<DespesaDto> LerArquivoJson(IFormFile file)
    {
        var despesas = new List<DespesaDto>();
        using (var stream = file.OpenReadStream()) 
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            despesas = System.Text.Json.JsonSerializer.Deserialize<List<DespesaDto>>(json);
            foreach (var despesa in despesas)
            {
                DefinirNomesEDevedoresDespesas(despesa);
            }
        }
        return despesas;
    }

    private static List<DespesaDto> LerArquivoCsv(IFormFile file)
    {
        var primeiraLinha = true;
        var despesas = new List<DespesaDto>();
        using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (primeiraLinha)
                {
                    primeiraLinha = false;
                    continue;
                }

                var parts = line.Split(';');
                if (parts.Length >= 5)
                {
                    var valorTexto = parts[2].Trim().Trim('"');

                    var despesa = new DespesaDto
                    {
                        Descricao = parts[0],
                        Data = DateTime.TryParse(parts[1], out var data) ? data : (DateTime?)null,
                        Valor = decimal.TryParse(
                            valorTexto,
                            NumberStyles.Any,
                        new CultureInfo("pt-BR"),
                            out var valor
                            ) ? valor : 0,
                        PagoPor = parts[3],
                        Nomes = parts[4]
                    };

                    DefinirNomesEDevedoresDespesas(despesa);

                    despesas.Add(despesa);
                }
            }
        }
        return despesas;
    }

    private static void DefinirNomesEDevedoresDespesas(DespesaDto despesa)
    {
           despesa.Nomes = despesa.Nomes.Trim().Trim('"');

        var nomes = despesa.Nomes
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .ToList();

        despesa.QuantidadePessoas = nomes.Count;
        foreach (var nome in nomes)
        {
            despesa.AdicionarDevedor(nome, despesa.CalcularValorUnitario());
        }

    }

    public static MemoryStream GerarArquivoFinal(List<string> logs, List<TransacaoDto> transacoes)
    {
        var resultado = GerarResultadoFinal(logs, transacoes);

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
        writer.Write(resultado);
        writer.Flush();
        stream.Position = 0; // Resetar a posição do stream para o início

        return stream;
    }

    private static string GerarResultadoFinal(List<string> logs, List<TransacaoDto> transacoes)
    {
        var sb = new StringBuilder();

        sb.AppendLine("************************************************************");
        sb.AppendLine("*            RACHA INTELIGENTE - RELATÓRIO FINAL           *");
        sb.AppendLine("************************************************************");
        sb.AppendLine();

        sb.AppendLine("--- LOG DE PROCESSAMENTO ---");
        foreach (var log in logs)
        {
            sb.AppendLine(log);
        }
        sb.AppendLine();

        sb.AppendLine("--- TRANSAÇÕES FINAIS RECOMENDADAS ---");
        if (transacoes.Any())
        {
            foreach (var transacao in transacoes)
            {
                sb.AppendLine($"{transacao.QuemPaga} deve pagar {transacao.Valor:C} para {transacao.QuemRecebe}.");
            }
        }
        else
        {
            sb.AppendLine("Nenhuma transação necessária. Todos estão quites.");
        }

        sb.AppendLine();
        sb.AppendLine("************************************************************");
        sb.AppendLine($"Relatório gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("************************************************************");

        return sb.ToString();
    }
}
