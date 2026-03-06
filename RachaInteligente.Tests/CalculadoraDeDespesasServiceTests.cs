using FluentAssertions;
using RachaInteligente.Shared.Dto;
using RachaInteligente.Service;

namespace RachaInteligente.Tests;

public class CalculadoraDeDespesasServiceTests
{
    [Fact]
    public void GerarTransacoes_DeveDividirCorretamente_QuandoUmaPessoaPagaParaVarios()
    {
        // Arrange
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("A", 90, "A", "B", "C")
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().HaveCount(2);
        transacoes.Should().Contain(t => t.QuemPaga == "B" && t.QuemRecebe == "A" && t.Valor == 30);
        transacoes.Should().Contain(t => t.QuemPaga == "C" && t.QuemRecebe == "A" && t.Valor == 30);
    }

    [Fact]
    public void GerarTransacoes_DeveAgruparDividas_QuandoExistemMultiplasDespesas()
    {
        // Arrange
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("A", 60, "A", "B"), // B deve 30 para A
            CriarDespesa("A", 40, "A", "B")  // B deve 20 para A
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().HaveCount(1);
        transacoes.Should().Contain(t => t.QuemPaga == "B" && t.QuemRecebe == "A" && t.Valor == 50);
    }

    [Fact]
    public void GerarTransacoes_NaoDeveGerarDivida_QuandoPagadorNaoEstaNaListaDeNomes()
    {
        // Arrange
        var despesas = new List<DespesaDto>
        {
            CriarDespesa("A", 100, "B", "C") 
        };

        // Act
        var (_, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);

        // Assert
        transacoes.Should().HaveCount(2);
        transacoes.Should().Contain(t => t.QuemPaga == "B" && t.QuemRecebe == "A" && t.Valor == 50);
        transacoes.Should().Contain(t => t.QuemPaga == "C" && t.QuemRecebe == "A" && t.Valor == 50);
    }
    
    [Fact]
    public void GerarTransacoesOtimizado_DeveResolverDividaCircular()
    {
        // A deve B 30, B deve C 30, C deve A 30 -> Ninguem deve nada
        var transacoes = new List<TransacaoDto>
        {
            new() { QuemPaga = "A", QuemRecebe = "B", Valor = 30 },
            new() { QuemPaga = "B", QuemRecebe = "C", Valor = 30 },
            new() { QuemPaga = "C", QuemRecebe = "A", Valor = 30 }
        };

        var (_, otimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);

        otimizadas.Should().BeEmpty();
    }

    [Fact]
    public void GerarTransacoesOtimizado_DeveSimplificarCadeiaDeDividas()
    {
        // A deve B 50, B deve C 50 -> A deve C 50
        var transacoes = new List<TransacaoDto>
        {
            new() { QuemPaga = "A", QuemRecebe = "B", Valor = 50 },
            new() { QuemPaga = "B", QuemRecebe = "C", Valor = 50 }
        };

        var (_, otimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);

        otimizadas.Should().HaveCount(1);
        otimizadas.Should().Contain(t => t.QuemPaga == "A" && t.QuemRecebe == "C" && t.Valor == 50);
    }

    [Fact]
    public void GerarTransacoesOtimizado_DeveCalcularCenarioComplexo()
    {
        // A deve B 100 (A: -100, B: +100)
        // B deve C 50  (B: +50, C: +50)
        // B deve D 50  (B: 0, D: +50)
        // Otimizado: A paga C 50, A paga D 50
        var transacoes = new List<TransacaoDto>
        {
            new() { QuemPaga = "A", QuemRecebe = "B", Valor = 100 },
            new() { QuemPaga = "B", QuemRecebe = "C", Valor = 50 },
            new() { QuemPaga = "B", QuemRecebe = "D", Valor = 50 }
        };

        var (_, otimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);

        otimizadas.Should().HaveCount(2);
        otimizadas.Should().Contain(t => t.QuemPaga == "A" && t.QuemRecebe == "C" && t.Valor == 50);
        otimizadas.Should().Contain(t => t.QuemPaga == "A" && t.QuemRecebe == "D" && t.Valor == 50);
    }
    
    [Fact]
    public void GerarTransacoesOtimizado_DeveManterCasasDecimaisCorretamente()
    {
        var transacoes = new List<TransacaoDto>
        {
            new() { QuemPaga = "A", QuemRecebe = "B", Valor = 33.33m },
            new() { QuemPaga = "B", QuemRecebe = "C", Valor = 33.33m }
        };

        var (_, otimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);

        otimizadas.Should().HaveCount(1);
        otimizadas.Should().Contain(t => t.QuemPaga == "A" && t.QuemRecebe == "C" && t.Valor == 33.33m);
    }

    private DespesaDto CriarDespesa(string pagoPor, decimal valor, params string[] nomes)
    {
        var despesa = new DespesaDto
        {
            Descricao = "Teste",
            Data = DateTime.Now,
            PagoPor = pagoPor,
            Valor = valor,
            QuantidadePessoas = nomes.Length,
            Nomes = string.Join(", ", nomes)
        };

        foreach (var nome in nomes)
        {
            despesa.AdicionarDevedor(nome, despesa.CalcularValorUnitario());
        }

        return despesa;
    }
}
