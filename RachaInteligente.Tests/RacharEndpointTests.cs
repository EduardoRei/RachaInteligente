using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RachaInteligente.Shared.Dto;

namespace RachaInteligente.Tests;

public class RacharEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RacharEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Rachar_SemArquivo_RetornaBadRequest()
    {
        var content = new MultipartFormDataContent();
        
        var response = await _client.PostAsync("/RacharPorArquivo", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Rachar_ArquivoInvalido_RetornaBadRequest()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/RacharPorArquivo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Rachar_ArquivoCsvValido_RetornaTxtComRelatorio()
    {
        var csv = "Despesa;Data;Valor;Pago por; Nomes\n" +
                  "Churrasco;2023-10-01;100.00;Alice;Alice,Bob\n";
        
        var content = new MultipartFormDataContent();
        var fileContent = new StringContent(csv);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "despesas.csv");

        var response = await _client.PostAsync("/RacharPorArquivo", content);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Bob deve pagar");
        text.Should().Contain("Alice");
    }
    
    [Fact]
    public async Task Post_Rachar_ArquivoJsonValido_RetornaTxtComRelatorio()
    {
        var json = @"[
            {
                ""descricao"": ""Churrasco"",
                ""data"": ""2023-10-01T00:00:00"",
                ""valor"": 100.0,
                ""pagoPor"": ""Alice"",
                ""nomes"": ""Alice, Bob""
            }
        ]";
        
        var content = new MultipartFormDataContent();
        var fileContent = new StringContent(json);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        content.Add(fileContent, "file", "despesas.json");

        var response = await _client.PostAsync("/RacharPorArquivo", content);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Bob deve pagar");
        text.Should().Contain("Alice");
    }

    [Fact]
    public async Task Post_Rachar_JsonNoCorpo_RetornaTxtComRelatorio()
    {
        var despesas = new List<DespesaDto>
        {
            new() {
                Descricao = "Lanche",
                Data = DateTime.Now,
                Valor = 60,
                PagoPor = "Alice",
                Nomes = "Alice, Bob"
            }
        };

        var response = await _client.PostAsJsonAsync("/Rachar", despesas);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Bob deve pagar");
        text.Should().Contain("30,00");
    }
}
