using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Relatorio;
using Folha.Core.Rubricas;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <summary>Pedido de cálculo da gratificação natalina de um ano.</summary>
public sealed record PedidoDeDecimoTerceiro
{
    public required Empregado Empregado { get; init; }

    public required int Ano { get; init; }

    /// <summary>Último dia do contrato, quando o empregado saiu no meio do ano.</summary>
    public DateOnly? Desligamento { get; init; }

    /// <summary>Média das verbas variáveis do ano, que integra a gratificação (Súmula 45 do TST).</summary>
    public decimal MediaDeVariaveis { get; init; }

    /// <summary>Quanto já foi pago na primeira parcela. Zero calcula as duas.</summary>
    public decimal AdiantamentoJaPago { get; init; }
}

/// <param name="Avos">Meses com quinze dias ou mais trabalhados, de zero a doze.</param>
/// <param name="Remuneracao">Salário mais média de variáveis.</param>
/// <param name="Integral">Gratificação cheia do ano.</param>
/// <param name="PrimeiraParcela">Adiantamento, pago até 30 de novembro.</param>
/// <param name="SegundaParcela">Quitação, paga até 20 de dezembro.</param>
public sealed record ResultadoDoDecimoTerceiro(
    int Avos,
    decimal Remuneracao,
    decimal Integral,
    Demonstrativo PrimeiraParcela,
    Demonstrativo SegundaParcela);

/// <summary>
/// Décimo terceiro salário. A conta tem duas armadilhas: os avos contam meses com
/// quinze dias ou mais de trabalho, e os impostos incidem sobre a gratificação
/// inteira mas só são descontados na segunda parcela — o adiantamento sai bruto.
/// </summary>
public sealed class DecimoTerceiro(TabelaInss inss, TabelaIrrf irrf, Parametros parametros)
{
    public static DecimoTerceiro Vigente2025 { get; } =
        new(TabelaInss.Vigente2025, TabelaIrrf.Vigente2025, Parametros.Vigente2025);

    /// <summary>
    /// Avos do ano: cada mês em que o empregado trabalhou quinze dias ou mais vale
    /// um doze avos (artigo 1º, §2º da Lei 4.090).
    /// </summary>
    public static int Avos(Empregado empregado, int ano, DateOnly? desligamento = null)
    {
        var avos = 0;
        for (var mes = 1; mes <= 12; mes++)
        {
            var competencia = new Competencia(ano, mes);
            var inicio = empregado.Admissao > competencia.PrimeiroDia ? empregado.Admissao : competencia.PrimeiroDia;
            var fim = desligamento is { } d && d < competencia.UltimoDia ? d : competencia.UltimoDia;
            if (fim >= inicio && fim.DayNumber - inicio.DayNumber + 1 >= 15) avos++;
        }

        return avos;
    }

    public ResultadoDoDecimoTerceiro Calcular(PedidoDeDecimoTerceiro pedido)
    {
        var empregado = pedido.Empregado;
        var competencia = new Competencia(pedido.Ano, 12);
        var avos = Avos(empregado, pedido.Ano, pedido.Desligamento);

        var remuneracao = Dinheiro.Centavos(empregado.SalarioBase + pedido.MediaDeVariaveis);
        var integral = Dinheiro.Centavos(remuneracao / 12m * avos);

        // Primeira parcela: metade da gratificação, sem INSS e sem imposto. O FGTS,
        // esse sim, é depositado já no mês do adiantamento.
        var primeira = new Demonstrativo(empregado, competencia, "Décimo terceiro — primeira parcela");
        var adiantamento = pedido.AdiantamentoJaPago > 0m
            ? Dinheiro.Centavos(pedido.AdiantamentoJaPago)
            : Dinheiro.Centavos(integral / 2m);
        primeira.Lancar(Catalogo.AdiantamentoDecimoTerceiro, adiantamento, avos, "avos");
        primeira.Informar(Catalogo.BaseFgts, primeira.BaseFgts);
        primeira.Informar(Catalogo.FgtsDoMes, Fgts.Deposito(primeira.BaseFgts, empregado, parametros));

        // Segunda parcela: a gratificação inteira entra como provento, o adiantamento
        // volta como desconto e os impostos incidem sobre o total, não sobre a metade.
        var segunda = new Demonstrativo(empregado, competencia, "Décimo terceiro — segunda parcela");
        segunda.Lancar(Catalogo.DecimoTerceiro, integral, avos, "avos");
        segunda.Lancar(Catalogo.DecimoTerceiroAdiantado, adiantamento);

        var contribuicao = inss.Contribuicao(integral);
        segunda.Lancar(Catalogo.Inss, contribuicao, inss.AliquotaEfetiva(integral) * 100m, "%");

        var resultadoIrrf = Irrf.Calcular(
            integral,
            contribuicao,
            empregado.DependentesParaIrrf(competencia.UltimoDia),
            irrf,
            parametros);
        segunda.Lancar(Catalogo.Irrf, resultadoIrrf.Imposto, resultadoIrrf.Aliquota * 100m, "%");

        segunda.Informar(Catalogo.BaseInss, integral);
        segunda.Informar(Catalogo.BaseIrrf, resultadoIrrf.BaseTributavel);
        var baseFgtsSegunda = Dinheiro.NaoNegativo(integral - adiantamento);
        segunda.Informar(Catalogo.BaseFgts, baseFgtsSegunda);
        segunda.Informar(Catalogo.FgtsDoMes, Fgts.Deposito(baseFgtsSegunda, empregado, parametros));

        return new ResultadoDoDecimoTerceiro(avos, remuneracao, integral, primeira, segunda);
    }
}
