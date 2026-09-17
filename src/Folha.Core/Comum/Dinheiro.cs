namespace Folha.Core.Comum;

/// <summary>
/// Arredondamento monetário da folha. Tudo fecha em centavos e o critério é o
/// comercial (meio para cima), que é o usado nos demonstrativos de pagamento.
/// </summary>
public static class Dinheiro
{
    /// <summary>Arredonda para centavos, meio sempre para longe do zero.</summary>
    public static decimal Centavos(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Aplica uma alíquota sobre uma base e arredonda o resultado.</summary>
    public static decimal Aplicar(decimal baseCalculo, decimal aliquota) => Centavos(baseCalculo * aliquota);

    /// <summary>Nunca deixa um valor ficar negativo (imposto, base tributável, saldo).</summary>
    public static decimal NaoNegativo(decimal valor) => valor < 0m ? 0m : valor;

    /// <summary>Divide por 30 avos, que é como a CLT conta o dia de salário.</summary>
    public static decimal PorDia(decimal salarioMensal, int dias) => Centavos(salarioMensal / 30m * dias);
}
