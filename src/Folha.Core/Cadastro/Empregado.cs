using Folha.Core.Comum;

namespace Folha.Core.Cadastro;

/// <summary>Graus de insalubridade da NR-15, com o percentual sobre o salário mínimo.</summary>
public enum GrauDeInsalubridade
{
    Nenhum = 0,
    Minimo = 10,
    Medio = 20,
    Maximo = 40,
}

/// <summary>Categoria para efeito de FGTS: o aprendiz recolhe 2% em vez de 8%.</summary>
public enum Categoria
{
    Normal,
    Aprendiz,
}

/// <summary>Contrato de trabalho com o que a folha precisa saber para calcular.</summary>
public sealed record Empregado
{
    public required string Matricula { get; init; }

    public required string Nome { get; init; }

    public required DateOnly Admissao { get; init; }

    /// <summary>Salário contratual mensal.</summary>
    public required decimal SalarioBase { get; init; }

    public DateOnly? Nascimento { get; init; }

    public string Cargo { get; init; } = string.Empty;

    /// <summary>Carga horária mensal do contrato. 220 horas é a jornada de 44 horas semanais.</summary>
    public decimal CargaHorariaMensal { get; init; } = 220m;

    public IReadOnlyList<Dependente> Dependentes { get; init; } = [];

    public GrauDeInsalubridade Insalubridade { get; init; } = GrauDeInsalubridade.Nenhum;

    public bool Periculosidade { get; init; }

    /// <summary>Aposentado que já fez 65 anos tem uma parcela isenta a mais no imposto de renda.</summary>
    public bool Aposentado { get; init; }

    public Categoria Categoria { get; init; } = Categoria.Normal;

    /// <summary>Custo diário do transporte declarado pelo empregado. Zero significa que não optou.</summary>
    public decimal CustoDiarioDeTransporte { get; init; }

    /// <summary>Valor da hora normal, base de horas extras e de adicional noturno.</summary>
    public decimal ValorHora => SalarioBase / CargaHorariaMensal;

    public int DependentesParaIrrf(DateOnly data) => Dependentes.Count(d => d.DeduzIrrf(data));

    public int CotasDeSalarioFamilia(DateOnly data) => Dependentes.Count(d => d.DaSalarioFamilia(data));

    public bool TemSessentaECincoAnos(DateOnly data) =>
        Nascimento is { } n && new Dependente(Nome, n, TipoDependente.Outro).IdadeEm(data) >= 65;

    /// <summary>
    /// Anos completos de casa na data, usado no aviso prévio proporcional e nos avos.
    /// </summary>
    public int AnosDeCasaEm(DateOnly data)
    {
        var anos = data.Year - Admissao.Year;
        if (Admissao.AddYears(anos) > data) anos--;
        return anos < 0 ? 0 : anos;
    }

    /// <summary>
    /// Dias de salário devidos no mês. Quem foi admitido durante a competência
    /// recebe do dia da admissão até o fim do mês, contado em trinta avos.
    /// </summary>
    public int DiasDeSalarioNa(Competencia competencia, DateOnly? desligamento = null)
    {
        var primeiro = competencia.Contem(Admissao) ? Admissao.Day : 1;
        var ultimo = desligamento is { } d && competencia.Contem(d) ? d.Day : 30;
        if (primeiro > 30) primeiro = 30;
        if (ultimo > 30) ultimo = 30;
        return Math.Max(0, ultimo - primeiro + 1);
    }
}
