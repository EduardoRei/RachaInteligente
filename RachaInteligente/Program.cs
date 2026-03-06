using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using RachaInteligente.Service;
using RachaInteligente.Shared.Dto;
using Scalar.AspNetCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configurar a cultura para pt-BR globalmente
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".dll"] = "application/octet-stream";

app.MapOpenApi();
app.MapScalarApiReference();

// Endpoint leve para Keep-Alive (Ping)
app.MapGet("/health", () => Results.Ok("Server is running"));

// Ordem correta de middlewares para Blazor Hosted
app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

app.MapPost("/RacharPorArquivo", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("Por favor, selecione um arquivo.");

    try
    {
        var despesas = ArquivoService.ProcessarArquivo(file);
        return ProcessarGerarRelatorio(despesas);
    }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
})
.WithSummary("Processar arquivo de despesas")
.DisableAntiforgery();

app.MapPost("/Rachar", async ([FromBody] List<DespesaDto> despesas) =>
{
    if (despesas == null || !despesas.Any())
        return Results.BadRequest("A lista de despesas não pode estar vazia.");

    try
    {
        foreach (var despesa in despesas)
        {
            ArquivoService.DefinirNomesEDevedoresDespesas(despesa);
        }
        return ProcessarGerarRelatorio(despesas);
    }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
})
.WithSummary("Processar lista JSON de despesas")
.DisableAntiforgery();

app.MapFallbackToFile("index.html");

app.Run();

IResult ProcessarGerarRelatorio(List<DespesaDto> despesas)
{
    var todosLogs = new List<string>();
    var (logsTransacoes, transacoes) = CalculadoraDeDespesasService.GerarTransacoes(despesas);
    todosLogs.AddRange(logsTransacoes);

    var (logsOtimizacao, transacoesOtimizadas) = CalculadoraDeDespesasService.GerarTransacoesOtimizado(transacoes);
    todosLogs.AddRange(logsOtimizacao);

    var stream = ArquivoService.GerarArquivoFinal(todosLogs, transacoesOtimizadas);
    var filename = $"Resultado_Otimizado_Racha_Inteligente_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

    return Results.File(stream, "text/plain", filename);
}

public partial class Program { }
