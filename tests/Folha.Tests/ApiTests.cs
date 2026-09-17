using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Folha.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _cliente;

    public ApiTests(WebApplicationFactory<Program> fabrica) => _cliente = fabrica.CreateClient();

    private static object Empregado(decimal salario = 3_000m) => new
    {
        matricula = "0001",
        nome = "Ana Ribeiro",
        admissao = "2020-03-01",
        salarioBase = salario,
    };

    [Fact]
    public async Task Tabelas_expoem_o_teto_e_a_faixa_isenta()
    {
        var resposta = await _cliente.GetFromJsonAsync<JsonElement>("/api/tabelas");

        resposta.GetProperty("inss").GetProperty("teto").GetDecimal().Should().Be(8_157.41m);
        resposta.GetProperty("inss").GetProperty("contribuicaoMaxima").GetDecimal().Should().Be(951.63m);
        resposta.GetProperty("irrf").GetProperty("limiteDeIsencao").GetDecimal().Should().Be(2_428.80m);
    }

    [Fact]
    public async Task Catalogo_de_rubricas_mostra_as_incidencias()
    {
        var rubricas = await _cliente.GetFromJsonAsync<JsonElement>("/api/rubricas");

        var avisoIndenizado = rubricas.EnumerateArray().First(r => r.GetProperty("codigo").GetString() == "031");

        avisoIndenizado.GetProperty("incideInss").GetBoolean().Should().BeFalse();
        avisoIndenizado.GetProperty("incideFgts").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Folha_mensal_devolve_o_demonstrativo_e_o_liquido()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/mensal", new
        {
            empregado = Empregado(),
            ano = 2025,
            mes = 9,
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("liquido").GetDecimal().Should().Be(2_746.59m);
        corpo.GetProperty("competencia").GetString().Should().Be("09/2025");
        corpo.GetProperty("texto").GetString().Should().Contain("Salário base");
    }

    [Fact]
    public async Task Horas_extras_chegam_pelo_corpo_da_requisicao()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/mensal", new
        {
            empregado = Empregado(2_200m),
            ano = 2025,
            mes = 9,
            horasExtras50 = 10,
            feriados = new[] { "2025-09-07" },
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("baseInss").GetDecimal().Should().Be(2_373.08m);
    }

    [Fact]
    public async Task Decimo_terceiro_devolve_as_duas_parcelas()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/decimo-terceiro", new
        {
            empregado = Empregado(),
            ano = 2025,
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("avos").GetInt32().Should().Be(12);
        corpo.GetProperty("primeiraParcela").GetProperty("liquido").GetDecimal().Should().Be(1_500.00m);
        corpo.GetProperty("segundaParcela").GetProperty("liquido").GetDecimal().Should().Be(1_246.59m);
    }

    [Fact]
    public async Task Ferias_com_abono_devolvem_os_dias_vendidos()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/ferias", new
        {
            empregado = Empregado(),
            inicio = "2025-09-01",
            abonoPecuniario = true,
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("diasVendidos").GetInt32().Should().Be(10);
        corpo.GetProperty("diasDeGozo").GetInt32().Should().Be(20);
    }

    [Fact]
    public async Task Rescisao_aceita_o_motivo_pelo_nome_e_explica_a_conta()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/rescisao", new
        {
            empregado = Empregado(),
            desligamento = "2025-06-10",
            motivo = "SemJustaCausa",
            saldoDaContaDoFgts = 10_000m,
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("diasDeAvisoPrevio").GetInt32().Should().Be(45);
        corpo.GetProperty("temSeguroDesemprego").GetBoolean().Should().BeTrue();
        corpo.GetProperty("observacoes").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Justa_causa_zera_a_multa_do_fgts()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/folha/rescisao", new
        {
            empregado = Empregado(),
            desligamento = "2025-06-10",
            motivo = "JustaCausa",
            saldoDaContaDoFgts = 10_000m,
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("multaDoFgts").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Encargos_devolvem_o_custo_total_do_empregado()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/encargos", new
        {
            empregado = Empregado(),
            remuneracao = 3_000m,
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("total").GetDecimal().Should().Be(4_830.33m);
    }

    [Fact]
    public async Task Raiz_leva_para_a_documentacao()
    {
        var resposta = await _cliente.GetAsync("/docs/index.html");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
