namespace Folha.Core.Cadastro;

public enum TipoDependente
{
    Filho,
    Enteado,
    Conjuge,
    Pai,
    Mae,
    Outro,
}

/// <summary>
/// Dependente do empregado. A mesma pessoa pode valer para o imposto de renda,
/// para o salário-família ou para os dois: as regras de idade são diferentes.
/// </summary>
/// <param name="Nome">Nome do dependente.</param>
/// <param name="Nascimento">Data de nascimento.</param>
/// <param name="Tipo">Grau de parentesco.</param>
/// <param name="Invalido">Dependente inválido não perde a cota por idade.</param>
public sealed record Dependente(string Nome, DateOnly Nascimento, TipoDependente Tipo, bool Invalido = false)
{
    public int IdadeEm(DateOnly data)
    {
        var idade = data.Year - Nascimento.Year;
        if (Nascimento.AddYears(idade) > data) idade--;
        return idade;
    }

    /// <summary>
    /// Vale dedução no imposto de renda: filho ou enteado até 21 anos, até 24 se
    /// universitário — aqui simplificado para 21 —, cônjuge e ascendentes sem renda.
    /// </summary>
    public bool DeduzIrrf(DateOnly data) => Tipo switch
    {
        TipoDependente.Filho or TipoDependente.Enteado => Invalido || IdadeEm(data) <= 21,
        _ => true,
    };

    /// <summary>Vale cota de salário-família: filho ou equiparado até 14 anos, ou inválido.</summary>
    public bool DaSalarioFamilia(DateOnly data) =>
        Tipo is TipoDependente.Filho or TipoDependente.Enteado && (Invalido || IdadeEm(data) < 14);
}
