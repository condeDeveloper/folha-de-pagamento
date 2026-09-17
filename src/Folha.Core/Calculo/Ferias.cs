using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Relatorio;
using Folha.Core.Rubricas;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <summary>Pedido de férias de um período aquisitivo.</summary>
public sealed record PedidoDeFerias
{
    public required Empregado Empregado { get; init; }

    public required DateOnly Inicio { get; init; }

    /// <summary>Dias de gozo pedidos. Zero usa tudo a que o empregado tem direito.</summary>
    public int DiasDeGozo { get; init; }

    /// <summary>Faltas injustificadas no período aquisitivo, que reduzem o direito.</summary>
    public int FaltasNoPeriodoAquisitivo { get; init; }

    /// <summary>Venda de até um terço das férias (artigo 143 da CLT).</summary>
    public bool AbonoPecuniario { get; init; }

    /// <summary>Pedido do adiantamento da primeira parcela do décimo terceiro junto das férias.</summary>
    public bool AdiantarDecimoTerceiro { get; init; }

    public decimal MediaDeVariaveis { get; init; }
}

/// <param name="DiasDeDireito">Direito depois da tabela de faltas do artigo 130.</param>
/// <param name="DiasDeGozo">Dias efetivamente gozados.</param>
/// <param name="DiasVendidos">Dias convertidos em abono pecuniário.</param>
/// <param name="Fim">Último dia de férias.</param>
/// <param name="Demonstrativo">Recibo de férias.</param>
public sealed record ResultadoDeFerias(
    int DiasDeDireito,
    int DiasDeGozo,
    int DiasVendidos,
    DateOnly Fim,
    Demonstrativo Demonstrativo);

/// <summary>
/// Férias. O detalhe que costuma passar batido é o da incidência: férias gozadas e
/// o terço constitucional sofrem INSS e imposto, enquanto o abono pecuniário e o
/// terço dele são indenizatórios e saem líquidos.
/// </summary>
public sealed class Ferias(TabelaInss inss, TabelaIrrf irrf, Parametros parametros)
{
    public static Ferias Vigente2025 { get; } =
        new(TabelaInss.Vigente2025, TabelaIrrf.Vigente2025, Parametros.Vigente2025);

    /// <summary>Tabela do artigo 130 da CLT: falta demais e o empregado perde dias de férias.</summary>
    public static int DiasDeDireito(int faltasNoPeriodo) => faltasNoPeriodo switch
    {
        <= 5 => 30,
        <= 14 => 24,
        <= 23 => 18,
        <= 32 => 12,
        _ => 0,
    };

    public ResultadoDeFerias Calcular(PedidoDeFerias pedido)
    {
        var empregado = pedido.Empregado;
        var competencia = Competencia.De(pedido.Inicio);
        var direito = DiasDeDireito(pedido.FaltasNoPeriodoAquisitivo);

        // Vender um terço é vender um terço do direito, não dez dias fixos: quem tem
        // 24 dias por faltas vende 8.
        var vendidos = pedido.AbonoPecuniario ? direito / 3 : 0;
        var disponiveis = direito - vendidos;
        var gozados = pedido.DiasDeGozo > 0 ? Math.Min(pedido.DiasDeGozo, disponiveis) : disponiveis;

        var remuneracao = Dinheiro.Centavos(empregado.SalarioBase + pedido.MediaDeVariaveis);
        var demonstrativo = new Demonstrativo(empregado, competencia, "Recibo de férias");

        var valorFerias = Dinheiro.PorDia(remuneracao, gozados);
        demonstrativo.Lancar(Catalogo.Ferias, valorFerias, gozados, "d");
        demonstrativo.Lancar(Catalogo.TercoDeFerias, Dinheiro.Centavos(valorFerias / 3m));

        if (vendidos > 0)
        {
            var valorAbono = Dinheiro.PorDia(remuneracao, vendidos);
            demonstrativo.Lancar(Catalogo.AbonoPecuniario, valorAbono, vendidos, "d");
            demonstrativo.Lancar(Catalogo.TercoDoAbono, Dinheiro.Centavos(valorAbono / 3m));
        }

        if (pedido.AdiantarDecimoTerceiro)
        {
            var avos = DecimoTerceiro.Avos(empregado, pedido.Inicio.Year);
            demonstrativo.Lancar(
                Catalogo.AdiantamentoDecimoTerceiro,
                Dinheiro.Centavos(remuneracao / 12m * avos / 2m),
                avos,
                "avos");
        }

        // As bases do demonstrativo já excluem abono e terço do abono, porque as
        // rubricas deles nascem sem incidência nenhuma.
        var baseInss = demonstrativo.BaseInss;
        var contribuicao = inss.Contribuicao(baseInss);
        demonstrativo.Lancar(Catalogo.Inss, contribuicao, inss.AliquotaEfetiva(baseInss) * 100m, "%");

        var resultadoIrrf = Irrf.Calcular(
            demonstrativo.BaseIrrf,
            contribuicao,
            empregado.DependentesParaIrrf(pedido.Inicio),
            irrf,
            parametros);
        demonstrativo.Lancar(Catalogo.Irrf, resultadoIrrf.Imposto, resultadoIrrf.Aliquota * 100m, "%");

        demonstrativo.Informar(Catalogo.BaseInss, baseInss);
        demonstrativo.Informar(Catalogo.BaseIrrf, resultadoIrrf.BaseTributavel);
        demonstrativo.Informar(Catalogo.BaseFgts, demonstrativo.BaseFgts);
        demonstrativo.Informar(Catalogo.FgtsDoMes, Fgts.Deposito(demonstrativo.BaseFgts, empregado, parametros));

        return new ResultadoDeFerias(
            direito, gozados, vendidos, pedido.Inicio.AddDays(gozados - 1), demonstrativo);
    }
}
