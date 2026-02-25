using Microsoft.AspNetCore.Mvc;
using RachaInteligente.Service;
using Scalar.AspNetCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configurar a cultura para pt-BR globalmente
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();


app.UseHttpsRedirection();

app.MapPost("/Rachar", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("Por favor, selecione um arquivo para upload.");
    }

    try
    {
        var despesas = ArquivoService.ProcessarArquivo(file);

        var todosLogs = new List<string>();

        var (logsTransacoes, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);
        todosLogs.AddRange(logsTransacoes);

        var (logsOtimizacao, transacoesOtimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);
        todosLogs.AddRange(logsOtimizacao);

        var stream = ArquivoService.GerarArquivoFinal(todosLogs, transacoesOtimizadas);

        var filename = $"relatorio_racha_inteligente_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

        return Results.File(stream, "text/plain", filename);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ocorreu um erro ao processar o arquivo: {ex.Message}");
    }
})
    .WithSummary("Processar arquivo de despesas e calcular acertos")
    .WithDescription("""
        Este endpoint recebe um arquivo de despesas (CSV ou JSON), processa os dados, 
        calcula as transações necessárias para equilibrar os gastos entre os participantes 
        e retorna um relatório detalhado em formato .txt.
        
        O arquivo é OBRIGATÓRIO.
        Formatos aceitos:
        - .CSV (Separado por ponto e vírgula, com cabeçalhos: Despesa;Data;Valor;Pago por; Nomes;)
        - .JSON (Lista de objetos DespesaDto)

        Estrutura do DespesaDto (JSON):
        {
          "descricao": "string",
          "data": "DateTime?",
          "valor": "decimal",
          "pagoPor": "string",
          "nomes": "string (ex: 'Nome1, Nome2')"
        }
    """)
    .DisableAntiforgery();

app.Run();

