using Folha.Core.Cadastro;
using Folha.Core.Comum;

namespace Folha.Core.Calculo;

/// <summary>Apontamento do cartão de ponto do mês.</summary>
public sealed record Jornada
{
    public decimal HorasExtras50 { get; init; }

    public decimal HorasExtras100 { get; init; }

    /// <summary>Horas de relógio trabalhadas entre 22h e 5h.</summary>
    public decimal HorasNoturnas { get; init; }

    public decimal MinutosDeAtraso { get; init; }

    /// <summary>Datas de falta injustificada. A data importa porque a falta derruba o DSR da semana.</summary>
    public IReadOnlyList<DateOnly> Faltas { get; init; } = [];

    public IReadOnlyList<DateOnly> Feriados { get; init; } = [];

    public static Jornada Vazia { get; } = new();
}

/// <param name="ValorHoraExtra50">Horas extras a 50%.</param>
/// <param name="ValorHoraExtra100">Horas extras a 100%.</param>
/// <param name="AdicionalNoturno">Só o adicional de 20%, já com a hora reduzida.</param>
/// <param name="HorasNoturnasReduzidas">Horas noturnas convertidas pela razão 60/52,5.</param>
/// <param name="DsrSobreExtras">Reflexo das horas extras no repouso semanal.</param>
/// <param name="DsrSobreNoturno">Reflexo do adicional noturno no repouso semanal.</param>
/// <param name="DescontoDeFaltas">Faltas em trinta avos do salário.</param>
/// <param name="DescontoDeDsr">Repousos perdidos por falta injustificada na semana.</param>
/// <param name="RepousosPerdidos">Quantos repousos foram perdidos.</param>
/// <param name="DescontoDeAtrasos">Atrasos convertidos em horas.</param>
public sealed record ResultadoDaJornada(
    decimal ValorHoraExtra50,
    decimal ValorHoraExtra100,
    decimal AdicionalNoturno,
    decimal HorasNoturnasReduzidas,
    decimal DsrSobreExtras,
    decimal DsrSobreNoturno,
    decimal DescontoDeFaltas,
    decimal DescontoDeDsr,
    int RepousosPerdidos,
    decimal DescontoDeAtrasos);

/// <summary>
/// Converte o cartão de ponto em dinheiro: horas extras, adicional noturno com a
/// hora reduzida de 52 minutos e 30 segundos, o reflexo de tudo isso no descanso
/// semanal remunerado e o desconto de faltas e atrasos.
/// </summary>
public static class CalculoDeJornada
{
    /// <summary>Razão entre a hora de relógio e a hora noturna reduzida do artigo 73, §1º da CLT.</summary>
    public const decimal FatorDaHoraNoturna = 60m / 52.5m;

    public const decimal AdicionalNoturnoPercentual = 0.20m;

    public static ResultadoDaJornada Calcular(Empregado empregado, Jornada jornada, Competencia competencia)
    {
        var valorHora = empregado.ValorHora;

        var extras50 = Dinheiro.Centavos(jornada.HorasExtras50 * valorHora * 1.5m);
        var extras100 = Dinheiro.Centavos(jornada.HorasExtras100 * valorHora * 2m);

        var horasReduzidas = Math.Round(jornada.HorasNoturnas * FatorDaHoraNoturna, 4);
        var noturno = Dinheiro.Centavos(horasReduzidas * valorHora * AdicionalNoturnoPercentual);

        var diasUteis = competencia.DiasUteis(jornada.Feriados);
        var repousos = competencia.Repousos(jornada.Feriados);
        var dsrExtras = Dsr(extras50 + extras100, diasUteis, repousos);
        var dsrNoturno = Dsr(noturno, diasUteis, repousos);

        var faltasNoMes = jornada.Faltas.Where(competencia.Contem).Distinct().ToList();
        var descontoFaltas = Dinheiro.PorDia(empregado.SalarioBase, faltasNoMes.Count);

        var repousosPerdidos = RepousosPerdidos(faltasNoMes);
        var descontoDsr = Dinheiro.PorDia(empregado.SalarioBase, repousosPerdidos);

        var atrasos = Dinheiro.Centavos(jornada.MinutosDeAtraso / 60m * valorHora);

        return new ResultadoDaJornada(
            extras50, extras100, noturno, horasReduzidas, dsrExtras, dsrNoturno,
            descontoFaltas, descontoDsr, repousosPerdidos, atrasos);
    }

    /// <summary>
    /// Reflexo no descanso semanal: o que foi ganho a mais no mês, dividido pelos
    /// dias úteis e multiplicado pelos repousos (Lei 605/49 e Súmula 172 do TST).
    /// </summary>
    public static decimal Dsr(decimal valorVariavel, int diasUteis, int repousos) =>
        diasUteis <= 0 || valorVariavel <= 0m ? 0m : Dinheiro.Centavos(valorVariavel / diasUteis * repousos);

    /// <summary>
    /// A falta injustificada derruba o repouso da própria semana. Contamos as
    /// semanas de segunda a domingo que tiveram pelo menos uma falta.
    /// </summary>
    public static int RepousosPerdidos(IEnumerable<DateOnly> faltas) =>
        faltas.Select(SegundaDaSemana).Distinct().Count();

    private static DateOnly SegundaDaSemana(DateOnly data)
    {
        var deslocamento = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-deslocamento);
    }
}
