using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <param name="Insalubridade">Percentual da NR-15 sobre o salário mínimo.</param>
/// <param name="Periculosidade">Trinta por cento sobre o salário contratual.</param>
/// <param name="Acumulados">O que de fato entra na folha depois da regra de não acumulação.</param>
/// <param name="Motivo">Explicação de qual adicional prevaleceu e por quê.</param>
public sealed record ResultadoDeAdicionais(
    decimal Insalubridade,
    decimal Periculosidade,
    decimal Acumulados,
    string Motivo);

/// <summary>
/// Adicionais de condição de trabalho. Insalubridade e periculosidade não se
/// acumulam (artigo 193, §2º da CLT): o empregado opta, e a folha oferece o mais
/// vantajoso — o que quase sempre é a periculosidade, porque a base dela é o
/// salário contratual e não o mínimo.
/// </summary>
public static class Adicionais
{
    public const decimal PericulosidadePercentual = 0.30m;

    public static ResultadoDeAdicionais Calcular(Empregado empregado, Parametros parametros)
    {
        var insalubridade = empregado.Insalubridade == GrauDeInsalubridade.Nenhum
            ? 0m
            : Dinheiro.Aplicar(parametros.SalarioMinimo, (decimal)(int)empregado.Insalubridade / 100m);

        var periculosidade = empregado.Periculosidade
            ? Dinheiro.Aplicar(empregado.SalarioBase, PericulosidadePercentual)
            : 0m;

        if (insalubridade == 0m && periculosidade == 0m)
            return new ResultadoDeAdicionais(0m, 0m, 0m, "Sem adicional de condição de trabalho.");

        if (insalubridade > 0m && periculosidade > 0m)
        {
            var escolhido = Math.Max(insalubridade, periculosidade);
            var nome = periculosidade >= insalubridade ? "periculosidade" : "insalubridade";
            return new ResultadoDeAdicionais(
                insalubridade,
                periculosidade,
                escolhido,
                $"Os dois adicionais são devidos mas não acumulam: prevalece a {nome}, que é mais vantajosa.");
        }

        return periculosidade > 0m
            ? new ResultadoDeAdicionais(0m, periculosidade, periculosidade, "Periculosidade de 30% sobre o salário base.")
            : new ResultadoDeAdicionais(
                insalubridade,
                0m,
                insalubridade,
                $"Insalubridade em grau {empregado.Insalubridade.ToString().ToLowerInvariant()} sobre o salário mínimo.");
    }
}
