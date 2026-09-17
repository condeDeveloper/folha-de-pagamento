using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <summary>
/// Depósito do FGTS. Não é desconto: sai do bolso do empregador e vai para a conta
/// vinculada do empregado, por isso entra no demonstrativo como informativa.
/// </summary>
public static class Fgts
{
    public static decimal Aliquota(Empregado empregado, Parametros parametros) =>
        empregado.Categoria == Categoria.Aprendiz ? parametros.AliquotaFgtsAprendiz : parametros.AliquotaFgts;

    public static decimal Deposito(decimal baseDeCalculo, Empregado empregado, Parametros parametros) =>
        Dinheiro.Aplicar(Dinheiro.NaoNegativo(baseDeCalculo), Aliquota(empregado, parametros));

    /// <summary>
    /// Multa rescisória sobre todo o saldo depositado no contrato, mais os depósitos
    /// da própria rescisão: 40% na dispensa sem justa causa e 20% no acordo.
    /// </summary>
    public static decimal Multa(decimal saldoDaConta, decimal percentual) =>
        Dinheiro.Aplicar(Dinheiro.NaoNegativo(saldoDaConta), percentual);
}
