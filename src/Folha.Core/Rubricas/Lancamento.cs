namespace Folha.Core.Rubricas;

/// <summary>
/// Uma linha do demonstrativo: a rubrica, a referência que explica de onde veio
/// o número (horas, dias, avos, percentual) e o valor.
/// </summary>
/// <param name="Rubrica">Verba lançada.</param>
/// <param name="Valor">Valor em reais, sempre positivo — o sinal vem da natureza.</param>
/// <param name="Referencia">Quantidade que gerou o valor.</param>
/// <param name="Unidade">Unidade da referência: "h", "d", "avos", "%".</param>
public sealed record Lancamento(Rubrica Rubrica, decimal Valor, decimal Referencia = 0m, string Unidade = "")
{
    public bool EhProvento => Rubrica.Natureza == Natureza.Provento;

    public bool EhDesconto => Rubrica.Natureza == Natureza.Desconto;

    public bool EhInformativa => Rubrica.Natureza == Natureza.Informativa;

    /// <summary>Valor com sinal: provento soma, desconto abate, informativa não entra no líquido.</summary>
    public decimal ValorComSinal => Rubrica.Natureza switch
    {
        Natureza.Provento => Valor,
        Natureza.Desconto => -Valor,
        _ => 0m,
    };

    public string ReferenciaFormatada =>
        Referencia == 0m ? string.Empty : $"{Referencia:0.##}{(Unidade.Length > 0 ? " " + Unidade : string.Empty)}";
}
