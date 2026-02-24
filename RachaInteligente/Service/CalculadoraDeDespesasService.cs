using RachaInteligente.Dto;

namespace RachaInteligente.Service;

public static class CalculadoraDeDespesasService
{
    public static (List<string> logsTransacoes, List<TransacaoDto> transacoes) GerarTransacoes(List<DespesaDto> despesas)
    {
        var logs = new List<string>();
        var transacoes = new List<TransacaoDto>();

        logs.Add("=== Iniciando o processo de geração de transações ===");
        foreach (var despesa in despesas)
        {
            logs.Add($"Processando despesa: {despesa.Descricao} - Valor: {despesa.Valor:C} - Pago por: {despesa.PagoPor}");
            var valorUnitario = despesa.CalcularValorUnitario();
            logs.Add($"  Valor unitário calculado: {valorUnitario:C} para cada um dos {despesa.QuantidadePessoas} envolvidos.");
            foreach (var devedor in despesa.Devedores)
            {
                if (devedor.Nome.Equals(despesa.PagoPor, StringComparison.OrdinalIgnoreCase))
                {
                    logs.Add($"  - {devedor.Nome} é o pagador, pulando.");
                    continue;
                }
                logs.Add($"  - {devedor.Nome} deve {valorUnitario:C} para {despesa.PagoPor}");
                transacoes.Add(new TransacaoDto
                {
                    QuemPaga = devedor.Nome,
                    QuemRecebe = despesa.PagoPor,
                    Valor = valorUnitario
                });
            }
        }

        logs.Add("=== Agrupando transações quando há multiplas dividas entre pagador e devedor ===");

        var transacoesSimplificadas = transacoes
            .GroupBy(t => new { t.QuemPaga, t.QuemRecebe })
            .Select(g => new TransacaoDto
            {
                QuemPaga = g.Key.QuemPaga,
                QuemRecebe = g.Key.QuemRecebe,
                Valor = g.Sum(t => t.Valor)
            })
            .ToList();

        logs.Add("======= Sumario das dividas atuais =======");
        transacoesSimplificadas.OrderBy(t => t.QuemPaga).ThenBy(t => t.QuemRecebe).ToList().ForEach(t =>
        {
            logs.Add($"  - {t.QuemPaga} deve {t.Valor:C} para {t.QuemRecebe}");
        });

        logs.Add("===========================================");

        return (logs, transacoesSimplificadas);
    }

    public static (List<string>, List<TransacaoDto>) GerarTransacoesOtimizado(List<TransacaoDto> transacoes)
    {
        var logs = new List<string>();

        logs.Add("=== Calculando saldo líquido por pessoa ===");

        var saldos = new Dictionary<string, decimal>();

        foreach (var t in transacoes)
        {
            if (!saldos.ContainsKey(t.QuemPaga))
                saldos[t.QuemPaga] = 0;

            if (!saldos.ContainsKey(t.QuemRecebe))
                saldos[t.QuemRecebe] = 0;

            saldos[t.QuemPaga] -= t.Valor;
            saldos[t.QuemRecebe] += t.Valor;
        }

        foreach (var kv in saldos.OrderBy(k => k.Key))
        {
            var tipo = kv.Value > 0 ? "Credor" : kv.Value < 0 ? "Devedor" : "Saldo zerado";
            logs.Add($"  - {kv.Key}: {kv.Value:C} ({tipo})");
        }
        logs.Add("==================================");

        var devedores = saldos
            .Where(x => x.Value < 0)
            .Select(x => (Nome: x.Key, Valor: -x.Value))
            .OrderByDescending(x => x.Valor)
            .ToList();

        var credores = saldos
            .Where(x => x.Value > 0)
            .Select(x => (Nome: x.Key, Valor: x.Value))
            .OrderByDescending(x => x.Valor)
            .ToList();

        logs.Add("=== Iniciando compensação ===");

        var transacoesOtimizadas = new List<TransacaoDto>();
        int i = 0, j = 0;

        while (i < devedores.Count && j < credores.Count)
        {
            var devedor = devedores[i];
            var credor = credores[j];

            var valor = Math.Min(devedor.Valor, credor.Valor);

            transacoesOtimizadas.Add(new TransacaoDto
            {
                QuemPaga = devedor.Nome,
                QuemRecebe = credor.Nome,
                Valor = valor
            });

            logs.Add($"{devedor.Nome} paga {valor:C} para {credor.Nome}");

            devedor.Valor -= valor;
            credor.Valor -= valor;

            devedores[i] = devedor;
            credores[j] = credor;

            if (devedor.Valor == 0) i++;
            if (credor.Valor == 0) j++;
        }

        logs.Add("=== Finalizado ===");

        return (logs, transacoesOtimizadas);
    }
}
