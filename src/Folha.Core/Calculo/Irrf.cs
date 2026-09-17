using Folha.Core.Comum;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <param name="BaseBruta">Soma das verbas tributáveis antes de qualquer dedução.</param>
/// <param name="DeducaoLegal">INSS, dependentes, pensão e a isenção dos 65 anos.</param>
/// <param name="DeducaoSimplificada">Desconto simplificado, que substitui todas as deduções.</param>
/// <param name="UsouSimplificado">Qual dos dois caminhos pagou menos imposto.</param>
/// <param name="BaseTributavel">Base depois da dedução escolhida.</param>
/// <param name="Aliquota">Alíquota nominal da faixa.</param>
/// <param name="Imposto">Imposto retido.</param>
/// <param name="AliquotaEfetiva">Imposto sobre a base bruta.</param>
public sealed record ResultadoDoIrrf(
    decimal BaseBruta,
    decimal DeducaoLegal,
    decimal DeducaoSimplificada,
    bool UsouSimplificado,
    decimal BaseTributavel,
    decimal Aliquota,
    decimal Imposto,
    decimal AliquotaEfetiva);

/// <summary>
/// Imposto de renda retido na fonte. Desde 2023 a folha calcula o imposto dos dois
/// jeitos — com as deduções legais e com o desconto simplificado — e retém o menor,
/// que é o que a Receita chama de opção mais benéfica ao contribuinte.
/// </summary>
public static class Irrf
{
    public static ResultadoDoIrrf Calcular(
        decimal baseBruta,
        decimal inss,
        int dependentes,
        TabelaIrrf tabela,
        Parametros parametros,
        decimal pensaoAlimenticia = 0m,
        bool aposentadoComSessentaECinco = false)
    {
        var bruta = Dinheiro.NaoNegativo(Dinheiro.Centavos(baseBruta));

        var deducaoLegal = Dinheiro.Centavos(
            inss
            + dependentes * parametros.DeducaoPorDependente
            + pensaoAlimenticia
            + (aposentadoComSessentaECinco ? parametros.IsencaoMaiorDeSessentaECinco : 0m));

        // O simplificado nunca passa do próprio rendimento: quem ganha pouco não
        // gera base negativa, só zera o imposto.
        var deducaoSimplificada = Math.Min(parametros.DescontoSimplificado, bruta);

        var basePelaLegal = Dinheiro.NaoNegativo(Dinheiro.Centavos(bruta - deducaoLegal));
        var basePelaSimplificada = Dinheiro.NaoNegativo(Dinheiro.Centavos(bruta - deducaoSimplificada));

        var impostoPelaLegal = tabela.Imposto(basePelaLegal);
        var impostoPelaSimplificada = tabela.Imposto(basePelaSimplificada);

        var usouSimplificado = impostoPelaSimplificada < impostoPelaLegal;
        var baseEscolhida = usouSimplificado ? basePelaSimplificada : basePelaLegal;
        var imposto = usouSimplificado ? impostoPelaSimplificada : impostoPelaLegal;

        return new ResultadoDoIrrf(
            bruta,
            deducaoLegal,
            deducaoSimplificada,
            usouSimplificado,
            baseEscolhida,
            tabela.FaixaDe(baseEscolhida).Aliquota,
            imposto,
            bruta == 0m ? 0m : Math.Round(imposto / bruta, 6));
    }
}
