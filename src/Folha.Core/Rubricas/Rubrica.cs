namespace Folha.Core.Rubricas;

public enum Natureza
{
    /// <summary>Soma no bruto.</summary>
    Provento,

    /// <summary>Abate do bruto.</summary>
    Desconto,

    /// <summary>Não entra no líquido: só aparece no rodapé do demonstrativo.</summary>
    Informativa,
}

/// <summary>
/// Verba do demonstrativo. O que separa uma rubrica da outra não é o valor, é a
/// incidência: o mesmo real pago como hora extra entra em INSS, IRRF e FGTS, e
/// pago como férias indenizadas não entra em nenhum dos três.
/// </summary>
/// <param name="Codigo">Código da rubrica no demonstrativo.</param>
/// <param name="Nome">Descrição impressa.</param>
/// <param name="Natureza">Provento, desconto ou informativa.</param>
/// <param name="IncideInss">Entra no salário de contribuição.</param>
/// <param name="IncideIrrf">Entra na base do imposto de renda.</param>
/// <param name="IncideFgts">Entra na base de depósito do FGTS.</param>
public sealed record Rubrica(
    string Codigo,
    string Nome,
    Natureza Natureza,
    bool IncideInss = false,
    bool IncideIrrf = false,
    bool IncideFgts = false)
{
    /// <summary>Verba salarial cheia: incide nos três.</summary>
    public static Rubrica Salarial(string codigo, string nome) =>
        new(codigo, nome, Natureza.Provento, IncideInss: true, IncideIrrf: true, IncideFgts: true);

    /// <summary>Verba indenizatória: não incide em nada.</summary>
    public static Rubrica Indenizatoria(string codigo, string nome) =>
        new(codigo, nome, Natureza.Provento);

    public static Rubrica Desconto(string codigo, string nome) =>
        new(codigo, nome, Natureza.Desconto);

    /// <summary>
    /// Desconto que reduz as bases antes dos impostos, como faltas e atrasos:
    /// o empregado não recebeu, então também não contribui sobre aquilo.
    /// </summary>
    public static Rubrica DescontoQueReduzBases(string codigo, string nome) =>
        new(codigo, nome, Natureza.Desconto, IncideInss: true, IncideIrrf: true, IncideFgts: true);

    public override string ToString() => $"{Codigo} {Nome}";
}
