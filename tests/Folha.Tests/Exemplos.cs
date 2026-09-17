namespace Folha.Tests;

/// <summary>Empregados de exemplo, para os testes falarem de regra e não de cadastro.</summary>
public static class Exemplos
{
    /// <summary>Carga de 220 horas e salário de 2.200: a hora sai redonda em R$ 10,00.</summary>
    public static Empregado HoraRedonda => new()
    {
        Matricula = "0001",
        Nome = "Ana Ribeiro",
        Cargo = "Analista",
        Admissao = new DateOnly(2020, 3, 1),
        SalarioBase = 2_200.00m,
    };

    public static Empregado ComSalario(decimal salario) => HoraRedonda with { SalarioBase = salario };

    public static Dependente Filho(int anoDeNascimento) =>
        new("Filho", new DateOnly(anoDeNascimento, 6, 15), TipoDependente.Filho);
}
