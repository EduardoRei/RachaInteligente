using RachaInteligente.Dto;

namespace RachaInteligente.Service;

public static class CalculadoraDeDespesasService
{
public static List<string> AtribuirDespesas(List<DespesaDto> despesas, List<PessoaDto> pessoas)
    {
        var logs = new List<string>();
        logs.Add("--- Início do Processamento de Despesas ---");
        foreach (var despesa in despesas)
        {
            logs.Add($"Processando despesa: {despesa.Descricao} - Valor: {despesa.Valor:C} - Pago por: {despesa.PagoPor}");
            var pagador = pessoas.FirstOrDefault(p => p.Nome.Equals(despesa.PagoPor, StringComparison.OrdinalIgnoreCase));
            if (pagador == null)
            {
                logs.Add($"  [ERRO] Pagador '{despesa.PagoPor}' não encontrado na lista de pessoas.");
                continue;
            }

            foreach (var devedor in despesa.Devedores)
            {
                if (pagador.Nome.Equals(devedor.Nome, StringComparison.OrdinalIgnoreCase))
                {
                    logs.Add($"  - {devedor.Nome} é o próprio pagador, pulando débito de {devedor.Valor:C}.");
                    continue;
                }

                var devedorExistente = pagador.Devedores.FirstOrDefault(p => p.Nome.Equals(devedor.Nome, StringComparison.OrdinalIgnoreCase));
                if (devedorExistente != null)
                {
                    logs.Add($"  - {devedor.Nome} já devia {devedorExistente.Valor:C} para {pagador.Nome}. Adicionando {devedor.Valor:C}. Total: {(devedorExistente.Valor + devedor.Valor):C}");
                    devedorExistente.Valor += devedor.Valor;
                }
                else
                {
                    logs.Add($"  - {devedor.Nome} agora deve {devedor.Valor:C} para {pagador.Nome}.");
                    pagador.Devedores.Add(new DevedorDto
                    {
                        Nome = devedor.Nome,
                        Valor = devedor.Valor
                    });
                }
            }
        }
        logs.Add("--- Fim do Processamento de Despesas ---");
        return logs;
    }

    public static List<string> ResolverDividasMutuas(List<PessoaDto> pessoas)
    {
        var logs = new List<string>();
        logs.Add("\n--- Resolvendo Dívidas Mútuas ---");
        foreach (var pessoa in pessoas)
        {
            foreach (var devedor in pessoa.Devedores.ToList())
            {
                var pessoaDevedora = pessoas.FirstOrDefault(p => p.Nome.Equals(devedor.Nome, StringComparison.OrdinalIgnoreCase));
                var devedorReciproco = pessoaDevedora?.Devedores.FirstOrDefault(d => d.Nome.Equals(pessoa.Nome, StringComparison.OrdinalIgnoreCase));

                if (devedorReciproco != null && devedorReciproco.Valor > 0)
                {
                    logs.Add($"Conflito de dívida: {devedor.Nome} deve {devedor.Valor:C} para {pessoa.Nome}, mas {pessoa.Nome} deve {devedorReciproco.Valor:C} para {devedor.Nome}.");
                    if (devedor.Valor > devedorReciproco.Valor)
                    {
                        logs.Add($"  - {devedor.Nome} paga o líquido: {(devedor.Valor - devedorReciproco.Valor):C} para {pessoa.Nome}. {pessoa.Nome} não deve mais nada.");
                        devedor.Valor -= devedorReciproco.Valor;
                        devedorReciproco.Valor = 0;
                    }
                    else if (devedorReciproco.Valor > devedor.Valor)
                    {
                        logs.Add($"  - {pessoa.Nome} paga o líquido: {(devedorReciproco.Valor - devedor.Valor):C} para {devedor.Nome}. {devedor.Nome} não deve mais nada.");
                        devedorReciproco.Valor -= devedor.Valor;
                        devedor.Valor = 0;
                    }
                    else
                    {
                        logs.Add($"  - As dívidas se anulam completamente.");
                        devedor.Valor = 0;
                        devedorReciproco.Valor = 0;
                    }
                }
            }
        }

        int removidos = pessoas.Sum(p => p.Devedores.RemoveAll(d => d.Valor == 0));
        logs.Add($"Removidas {removidos} entradas de dívidas zeradas.");
        logs.Add("--- Fim da Resolução de Dívidas Mútuas ---");
        return logs;
    }

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
