using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Tabelas;

namespace Folha.Core.Encargos;

/// <param name="Rat">Risco ambiental do trabalho: 1%, 2% ou 3% conforme a atividade.</param>
/// <param name="Fap">Fator acidentário de prevenção, de 0,5 a 2,0, que multiplica o RAT.</param>
/// <param name="ProvisionarFeriasEDecimo">Se o custo deve incluir as provisões mensais.</param>
public sealed record PerfilDeEncargos(decimal Rat = 0.01m, decimal Fap = 1m, bool ProvisionarFeriasEDecimo = true)
{
    public decimal RatAjustado => Math.Round(Rat * Fap, 6);
}

/// <param name="Base">Remuneração do mês que serve de base.</param>
/// <param name="InssPatronal">Cota patronal de 20%.</param>
/// <param name="Rat">RAT já multiplicado pelo FAP.</param>
/// <param name="Terceiros">Sistema S, Incra, Sebrae e salário-educação.</param>
/// <param name="Fgts">Depósito do mês.</param>
/// <param name="ProvisaoDeFerias">Um doze avos das férias mais o terço constitucional.</param>
/// <param name="ProvisaoDeDecimoTerceiro">Um doze avos da gratificação natalina.</param>
/// <param name="EncargosSobreProvisoes">INSS, RAT, terceiros e FGTS que vão incidir quando as provisões virarem pagamento.</param>
/// <param name="Total">Custo total do mês.</param>
/// <param name="PercentualSobreSalario">Quanto o empregado custa acima do salário, em percentual.</param>
public sealed record ResultadoDeEncargos(
    decimal Base,
    decimal InssPatronal,
    decimal Rat,
    decimal Terceiros,
    decimal Fgts,
    decimal ProvisaoDeFerias,
    decimal ProvisaoDeDecimoTerceiro,
    decimal EncargosSobreProvisoes,
    decimal Total,
    decimal PercentualSobreSalario);

/// <summary>
/// O que o empregado custa para a empresa, que é bem mais do que o salário: cota
/// patronal, RAT, terceiros, FGTS e as provisões de férias e décimo terceiro, que
/// competem ao mês em que o direito é adquirido e não ao mês em que são pagas.
/// </summary>
public static class CustoDoEmpregador
{
    /// <summary>Um doze avos das férias mais um terço: 11,11% da remuneração ao mês.</summary>
    public const decimal ProvisaoDeFeriasPercentual = 1m / 12m * (4m / 3m);

    /// <summary>Um doze avos do décimo terceiro: 8,33% da remuneração ao mês.</summary>
    public const decimal ProvisaoDeDecimoPercentual = 1m / 12m;

    public static ResultadoDeEncargos Calcular(
        decimal remuneracao,
        Empregado empregado,
        Parametros parametros,
        PerfilDeEncargos? perfil = null)
    {
        perfil ??= new PerfilDeEncargos();
        var baseCalculo = Dinheiro.NaoNegativo(Dinheiro.Centavos(remuneracao));

        var patronal = Dinheiro.Aplicar(baseCalculo, parametros.InssPatronal);
        var rat = Dinheiro.Aplicar(baseCalculo, perfil.RatAjustado);
        var terceiros = Dinheiro.Aplicar(baseCalculo, parametros.Terceiros);
        var fgts = Dinheiro.Aplicar(baseCalculo, Calculo.Fgts.Aliquota(empregado, parametros));

        var provisaoFerias = perfil.ProvisionarFeriasEDecimo
            ? Dinheiro.Aplicar(baseCalculo, ProvisaoDeFeriasPercentual)
            : 0m;
        var provisaoDecimo = perfil.ProvisionarFeriasEDecimo
            ? Dinheiro.Aplicar(baseCalculo, ProvisaoDeDecimoPercentual)
            : 0m;

        // Quando a provisão virar pagamento, ela também vai gerar encargo: provisionar
        // só o principal subestima o custo em cerca de um terço.
        var aliquotaDeEncargos = parametros.InssPatronal + perfil.RatAjustado + parametros.Terceiros
                                 + Calculo.Fgts.Aliquota(empregado, parametros);
        var encargosSobreProvisoes = Dinheiro.Aplicar(provisaoFerias + provisaoDecimo, aliquotaDeEncargos);

        var total = Dinheiro.Centavos(
            baseCalculo + patronal + rat + terceiros + fgts + provisaoFerias + provisaoDecimo + encargosSobreProvisoes);

        return new ResultadoDeEncargos(
            baseCalculo, patronal, rat, terceiros, fgts, provisaoFerias, provisaoDecimo, encargosSobreProvisoes, total,
            baseCalculo == 0m ? 0m : Math.Round((total - baseCalculo) / baseCalculo, 6));
    }
}
