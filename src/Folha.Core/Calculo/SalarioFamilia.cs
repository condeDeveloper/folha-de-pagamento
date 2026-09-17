using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <param name="Cotas">Filhos que dão direito à cota.</param>
/// <param name="Valor">Total a pagar no mês.</param>
/// <param name="TemDireito">Se a remuneração ficou dentro do teto.</param>
public sealed record ResultadoDoSalarioFamilia(int Cotas, decimal Valor, bool TemDireito);

/// <summary>
/// Salário-família: cota por filho de até 14 anos para quem ganha pouco. Quem paga
/// é o INSS, a empresa só adianta e compensa na guia — por isso a verba não sofre
/// nenhuma incidência.
/// </summary>
public static class SalarioFamilia
{
    public static ResultadoDoSalarioFamilia Calcular(
        Empregado empregado,
        decimal remuneracaoDoMes,
        Competencia competencia,
        Parametros parametros)
    {
        var cotas = empregado.CotasDeSalarioFamilia(competencia.UltimoDia);
        if (cotas == 0) return new ResultadoDoSalarioFamilia(0, 0m, false);

        var temDireito = remuneracaoDoMes <= parametros.TetoSalarioFamilia;
        return new ResultadoDoSalarioFamilia(
            cotas,
            temDireito ? Dinheiro.Centavos(cotas * parametros.CotaSalarioFamilia) : 0m,
            temDireito);
    }
}
