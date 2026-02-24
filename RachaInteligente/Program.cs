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

app.MapPost("/Rachar", async ([FromForm] IFormFile file) =>
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
}).DisableAntiforgery();

app.Run();

