using System.Text.Json.Serialization;
using Folha.Core.Cadastro;
using Folha.Core.Calculo;
using Folha.Core.Comum;
using Folha.Core.Encargos;
using Folha.Core.Relatorio;
using Folha.Core.Rubricas;
using Folha.Core.Tabelas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Folha de pagamento", Version = "v1" }));
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton(Parametros.Vigente2025);
builder.Services.AddSingleton(TabelaInss.Vigente2025);
builder.Services.AddSingleton(TabelaIrrf.Vigente2025);
builder.Services.AddSingleton<FolhaMensal>();
builder.Services.AddSingleton<DecimoTerceiro>();
builder.Services.AddSingleton<Ferias>();
builder.Services.AddSingleton<Rescisao>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Folha de pagamento v1");
    c.RoutePrefix = "docs";
});

app.MapGet("/", () => Results.Redirect("/docs"));

app.MapGet("/api/tabelas", (TabelaInss inss, TabelaIrrf irrf, Parametros parametros) => Results.Ok(new
{
    inss = new
    {
        vigencia = inss.Vigencia,
        teto = inss.Teto,
        contribuicaoMaxima = inss.ContribuicaoMaxima,
        faixas = inss.Faixas,
    },
    irrf = new
    {
        vigencia = irrf.Vigencia,
        limiteDeIsencao = irrf.LimiteDeIsencao,
        faixas = irrf.Faixas.Select(f => new
        {
            ate = f.Ate == decimal.MaxValue ? (decimal?)null : f.Ate,
            f.Aliquota,
            f.Deducao,
        }),
    },
    parametros,
}))
.WithSummary("Tabelas legais em vigor");

app.MapGet("/api/rubricas", () => Results.Ok(Catalogo.Todas.Select(r => new
{
    r.Codigo,
    r.Nome,
    natureza = r.Natureza.ToString(),
    r.IncideInss,
    r.IncideIrrf,
    r.IncideFgts,
})))
.WithSummary("Catálogo de verbas e suas incidências");

app.MapPost("/api/folha/mensal", (FolhaMensalPedido pedido, FolhaMensal motor) =>
{
    var demonstrativo = motor.Calcular(pedido.ParaMovimento());
    return Results.Ok(Resposta.De(demonstrativo));
})
.WithSummary("Calcula o demonstrativo mensal");

app.MapPost("/api/folha/decimo-terceiro", (DecimoTerceiroPedido pedido, DecimoTerceiro motor) =>
{
    var resultado = motor.Calcular(new PedidoDeDecimoTerceiro
    {
        Empregado = pedido.Empregado.ParaEmpregado(),
        Ano = pedido.Ano,
        Desligamento = pedido.Desligamento,
        MediaDeVariaveis = pedido.MediaDeVariaveis,
        AdiantamentoJaPago = pedido.AdiantamentoJaPago,
    });

    return Results.Ok(new
    {
        resultado.Avos,
        resultado.Remuneracao,
        resultado.Integral,
        primeiraParcela = Resposta.De(resultado.PrimeiraParcela),
        segundaParcela = Resposta.De(resultado.SegundaParcela),
    });
})
.WithSummary("Calcula as duas parcelas do décimo terceiro");

app.MapPost("/api/folha/ferias", (FeriasPedido pedido, Ferias motor) =>
{
    var resultado = motor.Calcular(new PedidoDeFerias
    {
        Empregado = pedido.Empregado.ParaEmpregado(),
        Inicio = pedido.Inicio,
        DiasDeGozo = pedido.DiasDeGozo,
        FaltasNoPeriodoAquisitivo = pedido.FaltasNoPeriodoAquisitivo,
        AbonoPecuniario = pedido.AbonoPecuniario,
        AdiantarDecimoTerceiro = pedido.AdiantarDecimoTerceiro,
        MediaDeVariaveis = pedido.MediaDeVariaveis,
    });

    return Results.Ok(new
    {
        resultado.DiasDeDireito,
        resultado.DiasDeGozo,
        resultado.DiasVendidos,
        resultado.Fim,
        demonstrativo = Resposta.De(resultado.Demonstrativo),
    });
})
.WithSummary("Calcula o recibo de férias");

app.MapPost("/api/folha/rescisao", (RescisaoPedido pedido, Rescisao motor) =>
{
    var resultado = motor.Calcular(new PedidoDeRescisao
    {
        Empregado = pedido.Empregado.ParaEmpregado(),
        Desligamento = pedido.Desligamento,
        Motivo = pedido.Motivo,
        AvisoTrabalhado = pedido.AvisoTrabalhado,
        DiasDeFeriasVencidas = pedido.DiasDeFeriasVencidas,
        FaltasNoPeriodoAquisitivo = pedido.FaltasNoPeriodoAquisitivo,
        SaldoDaContaDoFgts = pedido.SaldoDaContaDoFgts,
        MediaDeVariaveis = pedido.MediaDeVariaveis,
        DecimoTerceiroJaAdiantado = pedido.DecimoTerceiroJaAdiantado,
    });

    return Results.Ok(new
    {
        resultado.DiasDeAvisoPrevio,
        resultado.DataDeProjecao,
        resultado.AvosDeDecimoTerceiro,
        resultado.AvosDeFeriasProporcionais,
        resultado.MultaDoFgts,
        resultado.PercentualDeSaque,
        resultado.TemSeguroDesemprego,
        resultado.Observacoes,
        demonstrativo = Resposta.De(resultado.Demonstrativo),
    });
})
.WithSummary("Calcula o termo de rescisão");

app.MapPost("/api/encargos", (EncargosPedido pedido, Parametros parametros) =>
{
    var resultado = CustoDoEmpregador.Calcular(
        pedido.Remuneracao,
        pedido.Empregado.ParaEmpregado(),
        parametros,
        new PerfilDeEncargos(pedido.Rat, pedido.Fap, pedido.ProvisionarFeriasEDecimo));

    return Results.Ok(resultado);
})
.WithSummary("Calcula o custo do empregado para a empresa");

app.Run();

/// <summary>Empregado como chega no corpo da requisição.</summary>
public sealed record EmpregadoPedido(
    string Matricula,
    string Nome,
    DateOnly Admissao,
    decimal SalarioBase,
    string? Cargo = null,
    DateOnly? Nascimento = null,
    decimal CargaHorariaMensal = 220m,
    GrauDeInsalubridade Insalubridade = GrauDeInsalubridade.Nenhum,
    bool Periculosidade = false,
    bool Aposentado = false,
    Categoria Categoria = Categoria.Normal,
    decimal CustoDiarioDeTransporte = 0m,
    IReadOnlyList<DependentePedido>? Dependentes = null)
{
    public Empregado ParaEmpregado() => new()
    {
        Matricula = Matricula,
        Nome = Nome,
        Admissao = Admissao,
        SalarioBase = SalarioBase,
        Cargo = Cargo ?? string.Empty,
        Nascimento = Nascimento,
        CargaHorariaMensal = CargaHorariaMensal,
        Insalubridade = Insalubridade,
        Periculosidade = Periculosidade,
        Aposentado = Aposentado,
        Categoria = Categoria,
        CustoDiarioDeTransporte = CustoDiarioDeTransporte,
        Dependentes = Dependentes?.Select(d => d.ParaDependente()).ToList() ?? [],
    };
}

public sealed record DependentePedido(string Nome, DateOnly Nascimento, TipoDependente Tipo, bool Invalido = false)
{
    public Dependente ParaDependente() => new(Nome, Nascimento, Tipo, Invalido);
}

public sealed record FolhaMensalPedido(
    EmpregadoPedido Empregado,
    int Ano,
    int Mes,
    decimal HorasExtras50 = 0m,
    decimal HorasExtras100 = 0m,
    decimal HorasNoturnas = 0m,
    decimal MinutosDeAtraso = 0m,
    IReadOnlyList<DateOnly>? Faltas = null,
    IReadOnlyList<DateOnly>? Feriados = null,
    DateOnly? Desligamento = null,
    decimal PensaoAlimenticia = 0m,
    decimal AdiantamentoSalarial = 0m,
    decimal OutrosProventos = 0m,
    decimal OutrosDescontos = 0m)
{
    public Movimento ParaMovimento() => new()
    {
        Empregado = Empregado.ParaEmpregado(),
        Competencia = new Competencia(Ano, Mes),
        Jornada = new Jornada
        {
            HorasExtras50 = HorasExtras50,
            HorasExtras100 = HorasExtras100,
            HorasNoturnas = HorasNoturnas,
            MinutosDeAtraso = MinutosDeAtraso,
            Faltas = Faltas ?? [],
            Feriados = Feriados ?? [],
        },
        Desligamento = Desligamento,
        PensaoAlimenticia = PensaoAlimenticia,
        AdiantamentoSalarial = AdiantamentoSalarial,
        OutrosProventos = OutrosProventos,
        OutrosDescontos = OutrosDescontos,
    };
}

public sealed record DecimoTerceiroPedido(
    EmpregadoPedido Empregado,
    int Ano,
    DateOnly? Desligamento = null,
    decimal MediaDeVariaveis = 0m,
    decimal AdiantamentoJaPago = 0m);

public sealed record FeriasPedido(
    EmpregadoPedido Empregado,
    DateOnly Inicio,
    int DiasDeGozo = 0,
    int FaltasNoPeriodoAquisitivo = 0,
    bool AbonoPecuniario = false,
    bool AdiantarDecimoTerceiro = false,
    decimal MediaDeVariaveis = 0m);

public sealed record RescisaoPedido(
    EmpregadoPedido Empregado,
    DateOnly Desligamento,
    MotivoDoDesligamento Motivo,
    bool AvisoTrabalhado = false,
    int DiasDeFeriasVencidas = 0,
    int FaltasNoPeriodoAquisitivo = 0,
    decimal SaldoDaContaDoFgts = 0m,
    decimal MediaDeVariaveis = 0m,
    decimal DecimoTerceiroJaAdiantado = 0m);

public sealed record EncargosPedido(
    EmpregadoPedido Empregado,
    decimal Remuneracao,
    decimal Rat = 0.01m,
    decimal Fap = 1m,
    bool ProvisionarFeriasEDecimo = true);

/// <summary>Serializa um demonstrativo do jeito que a interface precisa exibir.</summary>
public static class Resposta
{
    public static object De(Demonstrativo demonstrativo) => new
    {
        competencia = demonstrativo.Competencia.ToString(),
        titulo = demonstrativo.Titulo,
        empregado = new { demonstrativo.Empregado.Matricula, demonstrativo.Empregado.Nome },
        itens = demonstrativo.Itens.Select(i => new
        {
            i.Rubrica.Codigo,
            i.Rubrica.Nome,
            natureza = i.Rubrica.Natureza.ToString(),
            referencia = i.Referencia,
            i.Unidade,
            i.Valor,
        }),
        demonstrativo.TotalProventos,
        demonstrativo.TotalDescontos,
        demonstrativo.Liquido,
        demonstrativo.BaseInss,
        demonstrativo.BaseFgts,
        texto = demonstrativo.Imprimir(),
    };
}

/// <summary>Exposto para o WebApplicationFactory dos testes enxergar a aplicação.</summary>
public partial class Program;
