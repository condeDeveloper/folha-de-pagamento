namespace Folha.Core.Tabelas;

/// <summary>
/// Todo valor legal que muda de ano em ano fica aqui. Trocar de exercício é
/// trocar este objeto — nenhuma regra de cálculo carrega número cravado.
/// </summary>
public sealed record Parametros
{
    public required DateOnly Vigencia { get; init; }

    public required decimal SalarioMinimo { get; init; }

    /// <summary>Valor da cota mensal de salário-família por filho.</summary>
    public required decimal CotaSalarioFamilia { get; init; }

    /// <summary>Remuneração máxima para ter direito ao salário-família.</summary>
    public required decimal TetoSalarioFamilia { get; init; }

    /// <summary>Dedução mensal por dependente no imposto de renda.</summary>
    public required decimal DeducaoPorDependente { get; init; }

    /// <summary>Desconto simplificado, que substitui todas as deduções quando é mais vantajoso.</summary>
    public required decimal DescontoSimplificado { get; init; }

    /// <summary>Parcela isenta da aposentadoria para quem tem 65 anos ou mais.</summary>
    public required decimal IsencaoMaiorDeSessentaECinco { get; init; }

    public required decimal AliquotaFgts { get; init; }

    public required decimal AliquotaFgtsAprendiz { get; init; }

    /// <summary>Multa do FGTS na dispensa sem justa causa.</summary>
    public required decimal MultaFgtsDispensa { get; init; }

    /// <summary>Multa do FGTS no acordo entre as partes (artigo 484-A da CLT).</summary>
    public required decimal MultaFgtsAcordo { get; init; }

    /// <summary>Teto do desconto de vale-transporte: 6% do salário básico.</summary>
    public required decimal LimiteDescontoValeTransporte { get; init; }

    /// <summary>Contribuição previdenciária patronal sobre a folha.</summary>
    public required decimal InssPatronal { get; init; }

    /// <summary>Contribuições a terceiros: Sistema S, Incra, Sebrae e salário-educação.</summary>
    public required decimal Terceiros { get; init; }

    public static Parametros Vigente2025 { get; } = new()
    {
        Vigencia = new DateOnly(2025, 5, 1),
        SalarioMinimo = 1_518.00m,
        CotaSalarioFamilia = 65.00m,
        TetoSalarioFamilia = 1_906.04m,
        DeducaoPorDependente = 189.59m,
        DescontoSimplificado = 607.20m,
        IsencaoMaiorDeSessentaECinco = 1_903.98m,
        AliquotaFgts = 0.08m,
        AliquotaFgtsAprendiz = 0.02m,
        MultaFgtsDispensa = 0.40m,
        MultaFgtsAcordo = 0.20m,
        LimiteDescontoValeTransporte = 0.06m,
        InssPatronal = 0.20m,
        Terceiros = 0.058m,
    };
}
