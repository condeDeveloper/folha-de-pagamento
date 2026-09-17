using Folha.Core.Comum;

namespace Folha.Core.Tabelas;

/// <param name="Ate">Limite superior da faixa. A última usa decimal.MaxValue.</param>
/// <param name="Aliquota">Alíquota nominal da faixa.</param>
/// <param name="Deducao">Parcela a deduzir, que transforma a tabela progressiva numa conta só.</param>
public sealed record FaixaIrrf(decimal Ate, decimal Aliquota, decimal Deducao);

/// <summary>
/// Tabela progressiva mensal do imposto de renda retido na fonte. Ao contrário do
/// INSS, aqui a alíquota da faixa incide sobre a base inteira e a progressividade
/// entra pela parcela a deduzir.
/// </summary>
public sealed class TabelaIrrf
{
    public TabelaIrrf(DateOnly vigencia, IReadOnlyList<FaixaIrrf> faixas)
    {
        if (faixas.Count == 0) throw new ArgumentException("A tabela precisa de pelo menos uma faixa.", nameof(faixas));
        Vigencia = vigencia;
        Faixas = faixas;
    }

    public DateOnly Vigencia { get; }

    public IReadOnlyList<FaixaIrrf> Faixas { get; }

    /// <summary>Limite da faixa isenta.</summary>
    public decimal LimiteDeIsencao => Faixas[0].Ate;

    public FaixaIrrf FaixaDe(decimal baseTributavel) =>
        Faixas.First(f => Dinheiro.NaoNegativo(baseTributavel) <= f.Ate);

    /// <summary>Imposto sobre a base já deduzida. Nunca devolve valor negativo.</summary>
    public decimal Imposto(decimal baseTributavel)
    {
        var baseCalculo = Dinheiro.NaoNegativo(baseTributavel);
        var faixa = FaixaDe(baseCalculo);
        return Dinheiro.NaoNegativo(Dinheiro.Centavos(baseCalculo * faixa.Aliquota - faixa.Deducao));
    }

    /// <summary>
    /// Tabela vigente desde maio de 2025 (Lei 15.191/2025 e MP 1.294/2025), que
    /// elevou a faixa isenta para R$ 2.428,80.
    /// </summary>
    public static TabelaIrrf Vigente2025 { get; } = new(
        new DateOnly(2025, 5, 1),
        [
            new FaixaIrrf(2_428.80m, 0.000m, 0.00m),
            new FaixaIrrf(2_826.65m, 0.075m, 182.16m),
            new FaixaIrrf(3_751.05m, 0.150m, 394.16m),
            new FaixaIrrf(4_664.68m, 0.225m, 675.49m),
            new FaixaIrrf(decimal.MaxValue, 0.275m, 908.73m),
        ]);
}
