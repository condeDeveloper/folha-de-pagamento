using System.Text;
using Folha.Core.Comum;
using Folha.Core.Rubricas;

namespace Folha.Core.Relatorio;

/// <param name="Empregados">Quantos demonstrativos entraram no fechamento.</param>
/// <param name="TotalProventos">Soma dos proventos.</param>
/// <param name="TotalDescontos">Soma dos descontos.</param>
/// <param name="LiquidoAPagar">O que sai da conta da empresa para os empregados.</param>
/// <param name="InssRetido">INSS descontado dos empregados, a recolher na guia.</param>
/// <param name="IrrfRetido">Imposto de renda retido na fonte.</param>
/// <param name="FgtsADepositar">Depósitos do mês.</param>
/// <param name="BaseInss">Soma dos salários de contribuição.</param>
public sealed record ResumoDaFolha(
    int Empregados,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal LiquidoAPagar,
    decimal InssRetido,
    decimal IrrfRetido,
    decimal FgtsADepositar,
    decimal BaseInss)
{
    /// <summary>Fecha a folha de uma competência somando os demonstrativos.</summary>
    public static ResumoDaFolha De(IEnumerable<Demonstrativo> demonstrativos)
    {
        var lista = demonstrativos.ToList();
        return new ResumoDaFolha(
            lista.Count,
            Somar(lista, d => d.TotalProventos),
            Somar(lista, d => d.TotalDescontos),
            Somar(lista, d => d.Liquido),
            Somar(lista, d => d.Valor(Catalogo.Inss) + d.Valor(Catalogo.InssSobreDecimoTerceiro)),
            Somar(lista, d => d.Valor(Catalogo.Irrf) + d.Valor(Catalogo.IrrfSobreDecimoTerceiro)),
            Somar(lista, d => d.Valor(Catalogo.FgtsDoMes)),
            Somar(lista, d => d.BaseInss));
    }

    private static decimal Somar(IEnumerable<Demonstrativo> lista, Func<Demonstrativo, decimal> campo) =>
        Dinheiro.Centavos(lista.Sum(campo));

    public string Imprimir()
    {
        var texto = new StringBuilder();
        texto.AppendLine($"Empregados .................. {Empregados}");
        texto.AppendLine($"Proventos ................... {TotalProventos:N2}");
        texto.AppendLine($"Descontos ................... {TotalDescontos:N2}");
        texto.AppendLine($"Líquido a pagar ............. {LiquidoAPagar:N2}");
        texto.AppendLine($"Base do INSS ................ {BaseInss:N2}");
        texto.AppendLine($"INSS retido ................. {InssRetido:N2}");
        texto.AppendLine($"IRRF retido ................. {IrrfRetido:N2}");
        texto.AppendLine($"FGTS a depositar ............ {FgtsADepositar:N2}");
        return texto.ToString();
    }
}
