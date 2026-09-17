using Folha.Core.Comum;

namespace Folha.Core.Tabelas;

/// <param name="Ate">Limite superior da faixa.</param>
/// <param name="Aliquota">Alíquota aplicada só sobre a parte do salário dentro da faixa.</param>
public sealed record FaixaInss(decimal Ate, decimal Aliquota);

/// <summary>
/// Tabela de contribuição do INSS do empregado. Desde a reforma de 2019 ela é
/// progressiva: cada faixa incide só sobre a parte do salário que cai nela, e o
/// resultado é uma alíquota efetiva menor do que a nominal da última faixa.
/// </summary>
public sealed class TabelaInss
{
    public TabelaInss(DateOnly vigencia, IReadOnlyList<FaixaInss> faixas)
    {
        if (faixas.Count == 0) throw new ArgumentException("A tabela precisa de pelo menos uma faixa.", nameof(faixas));
        Vigencia = vigencia;
        Faixas = faixas;
    }

    public DateOnly Vigencia { get; }

    public IReadOnlyList<FaixaInss> Faixas { get; }

    /// <summary>Teto do salário de contribuição: o limite da última faixa.</summary>
    public decimal Teto => Faixas[^1].Ate;

    /// <summary>Contribuição de quem ganha no teto ou acima dele.</summary>
    public decimal ContribuicaoMaxima => Contribuicao(Teto);

    /// <summary>Desconto do empregado sobre o salário de contribuição.</summary>
    public decimal Contribuicao(decimal salarioDeContribuicao)
    {
        var salario = Math.Min(Dinheiro.NaoNegativo(salarioDeContribuicao), Teto);
        var total = 0m;
        var piso = 0m;
        foreach (var faixa in Faixas)
        {
            if (salario <= piso) break;
            total += (Math.Min(salario, faixa.Ate) - piso) * faixa.Aliquota;
            piso = faixa.Ate;
        }

        return Dinheiro.Centavos(total);
    }

    /// <summary>Alíquota efetiva: o que o empregado de fato paga sobre o salário cheio.</summary>
    public decimal AliquotaEfetiva(decimal salarioDeContribuicao) =>
        salarioDeContribuicao <= 0m ? 0m : Math.Round(Contribuicao(salarioDeContribuicao) / salarioDeContribuicao, 6);

    /// <summary>
    /// Tabela vigente desde janeiro de 2025, com o salário mínimo de R$ 1.518,00
    /// na primeira faixa (Portaria Interministerial MPS/MF 6/2025).
    /// </summary>
    public static TabelaInss Vigente2025 { get; } = new(
        new DateOnly(2025, 1, 1),
        [
            new FaixaInss(1_518.00m, 0.075m),
            new FaixaInss(2_793.88m, 0.090m),
            new FaixaInss(4_190.83m, 0.120m),
            new FaixaInss(8_157.41m, 0.140m),
        ]);
}
