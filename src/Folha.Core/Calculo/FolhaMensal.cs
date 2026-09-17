using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Relatorio;
using Folha.Core.Rubricas;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

/// <summary>Tudo que aconteceu com um empregado no mês e que a folha precisa saber.</summary>
public sealed record Movimento
{
    public required Empregado Empregado { get; init; }

    public required Competencia Competencia { get; init; }

    public Jornada Jornada { get; init; } = Jornada.Vazia;

    /// <summary>Data do desligamento, quando o mês é o último do contrato.</summary>
    public DateOnly? Desligamento { get; init; }

    public decimal PensaoAlimenticia { get; init; }

    public decimal AdiantamentoSalarial { get; init; }

    /// <summary>Prêmios, comissões e afins: entram como verba salarial.</summary>
    public decimal OutrosProventos { get; init; }

    /// <summary>Plano de saúde, farmácia, empréstimo consignado: não mexem em base nenhuma.</summary>
    public decimal OutrosDescontos { get; init; }
}

/// <summary>
/// Motor da folha mensal. Monta o demonstrativo na mesma ordem em que o holerite é
/// construído: primeiro tudo que é remuneração, depois os descontos que reduzem as
/// bases, e só então os impostos — que dependem das bases já fechadas.
/// </summary>
public sealed class FolhaMensal(TabelaInss inss, TabelaIrrf irrf, Parametros parametros)
{
    public TabelaInss TabelaInss { get; } = inss;

    public TabelaIrrf TabelaIrrf { get; } = irrf;

    public Parametros Parametros { get; } = parametros;

    public static FolhaMensal Vigente2025 { get; } =
        new(TabelaInss.Vigente2025, TabelaIrrf.Vigente2025, Parametros.Vigente2025);

    public Demonstrativo Calcular(Movimento movimento)
    {
        var empregado = movimento.Empregado;
        var competencia = movimento.Competencia;
        var demonstrativo = new Demonstrativo(empregado, competencia, "Demonstrativo de pagamento");

        // 1. Salário do mês, em trinta avos. Admissão ou desligamento no meio do mês
        //    encurtam o período; o mês cheio vale sempre trinta dias, não os do calendário.
        var dias = empregado.DiasDeSalarioNa(competencia, movimento.Desligamento);
        var salario = Dinheiro.PorDia(empregado.SalarioBase, dias);
        demonstrativo.Lancar(Catalogo.Salario, salario, dias, "d");

        // 2. Adicionais de condição de trabalho, proporcionais aos mesmos dias.
        var adicionais = Adicionais.Calcular(empregado, Parametros);
        if (adicionais.Periculosidade > 0m && adicionais.Acumulados == adicionais.Periculosidade)
            demonstrativo.Lancar(Catalogo.Periculosidade, Proporcional(adicionais.Periculosidade, dias), 30m, "%");
        else if (adicionais.Acumulados > 0m)
            demonstrativo.Lancar(
                Catalogo.Insalubridade, Proporcional(adicionais.Insalubridade, dias), (int)empregado.Insalubridade, "%");

        // 3. Cartão de ponto.
        var jornada = CalculoDeJornada.Calcular(empregado, movimento.Jornada, competencia);
        demonstrativo.Lancar(Catalogo.HoraExtra50, jornada.ValorHoraExtra50, movimento.Jornada.HorasExtras50, "h");
        demonstrativo.Lancar(Catalogo.HoraExtra100, jornada.ValorHoraExtra100, movimento.Jornada.HorasExtras100, "h");
        demonstrativo.Lancar(Catalogo.AdicionalNoturno, jornada.AdicionalNoturno, jornada.HorasNoturnasReduzidas, "h");
        demonstrativo.Lancar(Catalogo.DsrSobreExtras, jornada.DsrSobreExtras);
        demonstrativo.Lancar(Catalogo.DsrSobreNoturno, jornada.DsrSobreNoturno);
        demonstrativo.Lancar(Catalogo.OutrosProventos, movimento.OutrosProventos);

        // 4. Descontos que reduzem as bases antes dos impostos.
        var faltas = movimento.Jornada.Faltas.Count(competencia.Contem);
        demonstrativo.Lancar(Catalogo.Faltas, jornada.DescontoDeFaltas, faltas, "d");
        demonstrativo.Lancar(Catalogo.DsrSobreFaltas, jornada.DescontoDeDsr, jornada.RepousosPerdidos, "d");
        demonstrativo.Lancar(Catalogo.Atrasos, jornada.DescontoDeAtrasos, movimento.Jornada.MinutosDeAtraso, "min");

        // 5. Salário-família olha a remuneração já líquida de faltas.
        var remuneracao = demonstrativo.BaseInss;
        var salarioFamilia = SalarioFamilia.Calcular(empregado, remuneracao, competencia, Parametros);
        demonstrativo.Lancar(Catalogo.SalarioFamilia, salarioFamilia.Valor, salarioFamilia.Cotas, "cotas");

        // 6. INSS sobre o salário de contribuição fechado.
        var baseInss = demonstrativo.BaseInss;
        var contribuicao = TabelaInss.Contribuicao(baseInss);
        demonstrativo.Lancar(Catalogo.Inss, contribuicao, TabelaInss.AliquotaEfetiva(baseInss) * 100m, "%");

        // 7. Imposto de renda, já com o INSS como dedução.
        var resultadoIrrf = Irrf.Calcular(
            demonstrativo.BaseIrrf,
            contribuicao,
            empregado.DependentesParaIrrf(competencia.UltimoDia),
            TabelaIrrf,
            Parametros,
            movimento.PensaoAlimenticia,
            empregado.Aposentado && empregado.TemSessentaECincoAnos(competencia.UltimoDia));
        demonstrativo.Lancar(Catalogo.Irrf, resultadoIrrf.Imposto, resultadoIrrf.Aliquota * 100m, "%");

        // 8. Descontos que não tocam em base nenhuma.
        demonstrativo.Lancar(Catalogo.ValeTransporte, ValeTransporte(empregado, competencia, movimento, faltas));
        demonstrativo.Lancar(Catalogo.PensaoAlimenticia, movimento.PensaoAlimenticia);
        demonstrativo.Lancar(Catalogo.AdiantamentoSalarial, movimento.AdiantamentoSalarial);
        demonstrativo.Lancar(Catalogo.OutrosDescontos, movimento.OutrosDescontos);

        // 9. Rodapé: as bases e o depósito do FGTS, que não é desconto do empregado.
        var baseFgts = demonstrativo.BaseFgts;
        demonstrativo.Informar(Catalogo.BaseInss, baseInss);
        demonstrativo.Informar(Catalogo.BaseIrrf, resultadoIrrf.BaseTributavel);
        demonstrativo.Informar(Catalogo.BaseFgts, baseFgts);
        demonstrativo.Informar(Catalogo.FgtsDoMes, Fgts.Deposito(baseFgts, empregado, Parametros));

        return demonstrativo;
    }

    private static decimal Proporcional(decimal valorCheio, int dias) =>
        dias >= 30 ? valorCheio : Dinheiro.Centavos(valorCheio / 30m * dias);

    /// <summary>
    /// O desconto do vale-transporte é o menor entre 6% do salário básico e o custo
    /// real do transporte no mês (artigo 4º da Lei 7.418).
    /// </summary>
    private decimal ValeTransporte(Empregado empregado, Competencia competencia, Movimento movimento, int faltas)
    {
        if (empregado.CustoDiarioDeTransporte <= 0m) return 0m;

        var diasTrabalhados = Math.Max(0, competencia.DiasUteis(movimento.Jornada.Feriados) - faltas);
        var custo = Dinheiro.Centavos(empregado.CustoDiarioDeTransporte * diasTrabalhados);
        var teto = Dinheiro.Aplicar(empregado.SalarioBase, Parametros.LimiteDescontoValeTransporte);
        return Math.Min(custo, teto);
    }
}
