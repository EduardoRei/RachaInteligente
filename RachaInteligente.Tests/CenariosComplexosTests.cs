using FluentAssertions;
using RachaInteligente.Shared.Dto;
using RachaInteligente.Service;

namespace RachaInteligente.Tests;

public class CenariosComplexosTests
{
    [Fact]
    public void GerarTransacoes_DeveLidarComArredondamento_QuandoDivisaoNaoEExata()
    {
        // Arrange: 100 / 3 = 33.3333...
        // No sistema atual, cada um paga 33.33. A soma dá 99.99.
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("Alice", 100, "Alice", "Bob", "Charlie")
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().HaveCount(2);
        transacoes.Sum(t => t.Valor).Should().Be(66.66m); // Bob(33.33) + Charlie(33.33)
    }

    [Fact]
    public void GerarTransacoes_NaoDeveGerarDivida_QuandoPessoaPagaParaSiMesma()
    {
        // Arrange
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("Alice", 50, "Alice")
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().BeEmpty();
    }

    [Fact]
    public void GerarTransacoes_DeveIgnorarNomesDuplicadosNaMesmaDespesa()
    {
        // Arrange: Alice paga 100 para "Bob, Bob" -> Bob deve 50 (e não 100 ou erro)
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("Alice", 100, "Alice", "Bob", "Bob")
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().HaveCount(1);
        transacoes.First().Valor.Should().Be(50);
        transacoes.First().QuemPaga.Should().Be("Bob");
    }

    [Fact]
    public void GerarTransacoes_DeveSerInsensivelACasoNosNomes()
    {
        // Arrange: "alice" e "Alice" devem ser a mesma pessoa
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("Alice", 60, "Alice", "bob"),
            CriarDespesa("bob", 40, "BOB", "ALICE")
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);
        var (_, otimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);

        // Assert: Alice deve 30 para Bob, Bob deve 20 para Alice -> Alice deve 10 para Bob
        otimizadas.Should().HaveCount(1);
        otimizadas.First().Valor.Should().Be(10);
    }

    private DespesaDto CriarDespesa(string pagoPor, decimal valor, params string[] nomes)
    {
        var despesa = new DespesaDto
        {
            Descricao = "Teste Complexo",
            Data = DateTime.Now,
            PagoPor = pagoPor,
            Valor = valor,
            QuantidadePessoas = nomes.Distinct().Count(),
            Nomes = string.Join(", ", nomes)
        };

        var nomesUnicos = nomes.Distinct().ToList();
        var valorUnitario = Math.Round(valor / nomesUnicos.Count, 2);

        foreach (var nome in nomesUnicos)
        {
            despesa.AdicionarDevedor(nome, valorUnitario);
        }

        return despesa;
    }
}
