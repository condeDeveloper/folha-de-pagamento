namespace Folha.Core.Comum;

/// <summary>
/// Mês de referência da folha. Sabe contar os dias úteis e os repousos do mês,
/// que é o que o cálculo do DSR precisa.
/// </summary>
public readonly record struct Competencia(int Ano, int Mes) : IComparable<Competencia>
{
    public DateOnly PrimeiroDia => new(Ano, Mes, 1);

    public DateOnly UltimoDia => new(Ano, Mes, DateTime.DaysInMonth(Ano, Mes));

    public int DiasNoMes => DateTime.DaysInMonth(Ano, Mes);

    /// <summary>Todos os dias do mês, do primeiro ao último.</summary>
    public IEnumerable<DateOnly> Dias()
    {
        for (var dia = PrimeiroDia; dia <= UltimoDia; dia = dia.AddDays(1)) yield return dia;
    }

    public bool Contem(DateOnly data) => data.Year == Ano && data.Month == Mes;

    /// <summary>
    /// Dias úteis para efeito de DSR: de segunda a sábado, tirando os feriados.
    /// </summary>
    public int DiasUteis(IEnumerable<DateOnly>? feriados = null)
    {
        var lista = Feriados(feriados);
        return Dias().Count(d => d.DayOfWeek != DayOfWeek.Sunday && !lista.Contains(d));
    }

    /// <summary>Repousos remunerados do mês: domingos mais feriados que não caem no domingo.</summary>
    public int Repousos(IEnumerable<DateOnly>? feriados = null)
    {
        var lista = Feriados(feriados);
        return Dias().Count(d => d.DayOfWeek == DayOfWeek.Sunday || lista.Contains(d));
    }

    private HashSet<DateOnly> Feriados(IEnumerable<DateOnly>? feriados) =>
        feriados is null ? [] : [.. feriados.Where(Contem)];

    public Competencia Anterior => Mes == 1 ? new Competencia(Ano - 1, 12) : new Competencia(Ano, Mes - 1);

    public Competencia Proxima => Mes == 12 ? new Competencia(Ano + 1, 1) : new Competencia(Ano, Mes + 1);

    public static Competencia De(DateOnly data) => new(data.Year, data.Month);

    public int CompareTo(Competencia outra) => (Ano * 12 + Mes).CompareTo(outra.Ano * 12 + outra.Mes);

    public override string ToString() => $"{Mes:00}/{Ano}";
}
