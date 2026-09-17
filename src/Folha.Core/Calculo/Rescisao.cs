using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Relatorio;
using Folha.Core.Rubricas;
using Folha.Core.Tabelas;

namespace Folha.Core.Calculo;

public enum MotivoDoDesligamento
{
    /// <summary>Dispensa sem justa causa: o pacote completo.</summary>
    SemJustaCausa,

    /// <summary>Pedido de demissão: sem multa, sem saque, sem seguro.</summary>
    PedidoDeDemissao,

    /// <summary>Justa causa: perde aviso, décimo terceiro e férias proporcionais.</summary>
    JustaCausa,

    /// <summary>Acordo do artigo 484-A da CLT: metade do aviso, metade da multa, 80% do saque.</summary>
    AcordoEntreAsPartes,

    /// <summary>Fim do contrato por prazo determinado.</summary>
    TerminoDeContrato,
}

/// <summary>Dados do desligamento.</summary>
public sealed record PedidoDeRescisao
{
    public required Empregado Empregado { get; init; }

    public required DateOnly Desligamento { get; init; }

    public required MotivoDoDesligamento Motivo { get; init; }

    /// <summary>Aviso cumprido trabalhando. Quando falso, o aviso é indenizado e projeta o contrato.</summary>
    public bool AvisoTrabalhado { get; init; }

    /// <summary>Dias de férias vencidas que o empregado não chegou a gozar.</summary>
    public int DiasDeFeriasVencidas { get; init; }

    public int FaltasNoPeriodoAquisitivo { get; init; }

    /// <summary>Saldo já depositado na conta vinculada, base da multa rescisória.</summary>
    public decimal SaldoDaContaDoFgts { get; init; }

    public decimal MediaDeVariaveis { get; init; }

    /// <summary>Primeira parcela do décimo terceiro já adiantada no ano.</summary>
    public decimal DecimoTerceiroJaAdiantado { get; init; }
}

/// <param name="DiasDeAvisoPrevio">Aviso proporcional pelo tempo de casa.</param>
/// <param name="DataDeProjecao">Data que vale para os avos quando o aviso é indenizado.</param>
/// <param name="AvosDeDecimoTerceiro">Avos do décimo terceiro do ano do desligamento.</param>
/// <param name="AvosDeFeriasProporcionais">Avos de férias do período aquisitivo em curso.</param>
/// <param name="MultaDoFgts">Multa sobre o saldo da conta vinculada.</param>
/// <param name="PercentualDeSaque">Quanto do FGTS o empregado consegue sacar.</param>
/// <param name="TemSeguroDesemprego">Se o motivo dá direito ao benefício.</param>
/// <param name="Demonstrativo">Termo de rescisão.</param>
/// <param name="Observacoes">O porquê de cada verba devida ou negada.</param>
public sealed record ResultadoDaRescisao(
    int DiasDeAvisoPrevio,
    DateOnly DataDeProjecao,
    int AvosDeDecimoTerceiro,
    int AvosDeFeriasProporcionais,
    decimal MultaDoFgts,
    decimal PercentualDeSaque,
    bool TemSeguroDesemprego,
    Demonstrativo Demonstrativo,
    IReadOnlyList<string> Observacoes);

/// <summary>
/// Termo de rescisão. O motivo do desligamento decide quase tudo, e o aviso prévio
/// indenizado projeta a data de saída para frente — o que muda os avos do décimo
/// terceiro e das férias proporcionais, e é onde a conta costuma sair errada.
/// </summary>
public sealed class Rescisao(TabelaInss inss, TabelaIrrf irrf, Parametros parametros)
{
    public static Rescisao Vigente2025 { get; } =
        new(TabelaInss.Vigente2025, TabelaIrrf.Vigente2025, Parametros.Vigente2025);

    /// <summary>
    /// Trinta dias mais três por ano completo de casa, limitado a noventa
    /// (Lei 12.506/2011).
    /// </summary>
    public static int DiasDeAvisoPrevio(Empregado empregado, DateOnly desligamento) =>
        Math.Min(90, 30 + 3 * empregado.AnosDeCasaEm(desligamento));

    public ResultadoDaRescisao Calcular(PedidoDeRescisao pedido)
    {
        var empregado = pedido.Empregado;
        var desligamento = pedido.Desligamento;
        var competencia = Competencia.De(desligamento);
        var motivo = pedido.Motivo;
        var observacoes = new List<string>();

        var remuneracao = Dinheiro.Centavos(empregado.SalarioBase + pedido.MediaDeVariaveis);
        var demonstrativo = new Demonstrativo(empregado, competencia, $"Termo de rescisão — {Descrever(motivo)}");

        // 1. Saldo de salário: os dias do mês até o desligamento, em trinta avos.
        var diasTrabalhados = empregado.DiasDeSalarioNa(competencia, desligamento);
        demonstrativo.Lancar(Catalogo.SaldoDeSalario, Dinheiro.PorDia(remuneracao, diasTrabalhados), diasTrabalhados, "d");

        // 2. Aviso prévio. Indenizado projeta o contrato; não cumprido é descontado.
        var diasDeAviso = DiasDeAvisoPrevio(empregado, desligamento);
        var projecao = desligamento;

        if (EmpregadorPagaAviso(motivo) && !pedido.AvisoTrabalhado)
        {
            var proporcao = motivo == MotivoDoDesligamento.AcordoEntreAsPartes ? 0.5m : 1m;
            var valor = Dinheiro.Centavos(Dinheiro.PorDia(remuneracao, diasDeAviso) * proporcao);
            demonstrativo.Lancar(Catalogo.AvisoPrevioIndenizado, valor, diasDeAviso, "d");
            projecao = desligamento.AddDays(diasDeAviso);
            observacoes.Add(
                $"Aviso prévio de {diasDeAviso} dias indenizado{(proporcao < 1m ? " pela metade, por ser acordo" : string.Empty)}; " +
                $"o contrato projeta até {projecao:dd/MM/yyyy} para efeito de avos.");
        }
        else if (motivo == MotivoDoDesligamento.PedidoDeDemissao && !pedido.AvisoTrabalhado)
        {
            demonstrativo.Lancar(Catalogo.AvisoPrevioDescontado, Dinheiro.PorDia(remuneracao, 30), 30, "d");
            observacoes.Add("Pedido de demissão sem cumprir o aviso: trinta dias descontados do acerto.");
        }
        else if (pedido.AvisoTrabalhado)
        {
            observacoes.Add($"Aviso prévio de {diasDeAviso} dias cumprido trabalhando: não há projeção nem indenização.");
        }

        // 3. Décimo terceiro proporcional, com os avos já projetados.
        var avosDecimo = 0;
        if (TemDireitoAProporcionais(motivo))
        {
            avosDecimo = DecimoTerceiro.Avos(empregado, desligamento.Year, desligamento);
            var valorDecimo = Dinheiro.Centavos(remuneracao / 12m * avosDecimo);
            demonstrativo.Lancar(Catalogo.DecimoTerceiroProporcional, valorDecimo, avosDecimo, "avos");

            // Os avos ganhos pela projeção são a diferença entre contar até a data
            // projetada e contar até o desligamento, sempre dentro do mesmo ano.
            var avosProjetados = projecao > desligamento
                ? DecimoTerceiro.Avos(empregado, desligamento.Year, projecao) - avosDecimo
                : 0;
            if (avosProjetados > 0)
                demonstrativo.Lancar(
                    Catalogo.DecimoTerceiroSobreAviso, Dinheiro.Centavos(remuneracao / 12m * avosProjetados), avosProjetados, "avos");

            demonstrativo.Lancar(Catalogo.DecimoTerceiroAdiantado, pedido.DecimoTerceiroJaAdiantado);
        }
        else
        {
            observacoes.Add("Justa causa: o décimo terceiro proporcional não é devido (artigo 3º da Lei 4.090).");
        }

        // 4. Férias vencidas: devidas em qualquer motivo, inclusive na justa causa
        //    (Súmula 171 do TST só alcança as proporcionais).
        if (pedido.DiasDeFeriasVencidas > 0)
        {
            var valor = Dinheiro.PorDia(remuneracao, pedido.DiasDeFeriasVencidas);
            demonstrativo.Lancar(Catalogo.FeriasVencidas, valor, pedido.DiasDeFeriasVencidas, "d");
            demonstrativo.Lancar(Catalogo.TercoDeFeriasVencidas, Dinheiro.Centavos(valor / 3m));
            observacoes.Add("Férias vencidas são devidas mesmo na justa causa.");
        }

        // 5. Férias proporcionais do período aquisitivo em curso.
        var avosDeFerias = 0;
        if (TemDireitoAProporcionais(motivo))
        {
            avosDeFerias = AvosDeFerias(empregado, projecao);
            var diasDeDireito = Ferias.DiasDeDireito(pedido.FaltasNoPeriodoAquisitivo);
            var valor = Dinheiro.Centavos(remuneracao / 30m * diasDeDireito / 12m * avosDeFerias);
            demonstrativo.Lancar(Catalogo.FeriasProporcionais, valor, avosDeFerias, "avos");
            demonstrativo.Lancar(Catalogo.TercoDeFeriasProporcionais, Dinheiro.Centavos(valor / 3m));
        }
        else
        {
            observacoes.Add("Justa causa: as férias proporcionais são perdidas (Súmula 171 do TST).");
        }

        // 6. INSS e imposto. Só o saldo de salário e o décimo terceiro proporcional
        //    são tributáveis: aviso indenizado e férias indenizadas são verbas
        //    indenizatórias e saem líquidas.
        var baseInss = demonstrativo.BaseInss;
        var contribuicao = inss.Contribuicao(baseInss);
        demonstrativo.Lancar(Catalogo.Inss, contribuicao, inss.AliquotaEfetiva(baseInss) * 100m, "%");

        var dependentes = empregado.DependentesParaIrrf(desligamento);
        var resultadoIrrf = Irrf.Calcular(demonstrativo.BaseIrrf, contribuicao, dependentes, irrf, parametros);
        demonstrativo.Lancar(Catalogo.Irrf, resultadoIrrf.Imposto, resultadoIrrf.Aliquota * 100m, "%");

        var baseDecimo = demonstrativo.Valor(Catalogo.DecimoTerceiroProporcional);
        if (baseDecimo > 0m)
        {
            var inssDecimo = inss.Contribuicao(baseDecimo);
            demonstrativo.Lancar(Catalogo.InssSobreDecimoTerceiro, inssDecimo, inss.AliquotaEfetiva(baseDecimo) * 100m, "%");
            var irrfDecimo = Irrf.Calcular(baseDecimo, inssDecimo, dependentes, irrf, parametros);
            demonstrativo.Lancar(Catalogo.IrrfSobreDecimoTerceiro, irrfDecimo.Imposto, irrfDecimo.Aliquota * 100m, "%");
        }

        // 7. FGTS: depósito do mês, multa rescisória e o que dá para sacar.
        var baseFgts = demonstrativo.BaseFgts;
        var depositoDoMes = Fgts.Deposito(baseFgts, empregado, parametros);
        var saldoTotal = Dinheiro.Centavos(pedido.SaldoDaContaDoFgts + depositoDoMes);
        var percentualDaMulta = PercentualDaMulta(motivo);
        var multa = Fgts.Multa(saldoTotal, percentualDaMulta);
        if (multa > 0m) demonstrativo.Lancar(Catalogo.MultaFgts, multa, percentualDaMulta * 100m, "%");

        var percentualDeSaque = PercentualDeSaque(motivo);
        demonstrativo.Informar(Catalogo.BaseInss, baseInss);
        demonstrativo.Informar(Catalogo.BaseIrrf, resultadoIrrf.BaseTributavel);
        demonstrativo.Informar(Catalogo.BaseFgts, baseFgts);
        demonstrativo.Informar(Catalogo.FgtsDoMes, depositoDoMes);
        demonstrativo.Informar(Catalogo.FgtsDepositado, Dinheiro.Aplicar(saldoTotal, percentualDeSaque));

        var seguro = motivo == MotivoDoDesligamento.SemJustaCausa;
        observacoes.Add(seguro
            ? "Dá direito ao seguro-desemprego e ao saque integral do FGTS."
            : $"Sem seguro-desemprego; saque do FGTS liberado em {percentualDeSaque * 100m:0}%.");

        return new ResultadoDaRescisao(
            diasDeAviso, projecao, avosDecimo, avosDeFerias, multa, percentualDeSaque, seguro,
            demonstrativo, observacoes);
    }

    /// <summary>
    /// Avos de férias do período aquisitivo aberto: meses com quinze dias ou mais
    /// desde o último aniversário de admissão.
    /// </summary>
    public static int AvosDeFerias(Empregado empregado, DateOnly ate)
    {
        var inicioDoPeriodo = empregado.Admissao.AddYears(empregado.AnosDeCasaEm(ate));
        var avos = 0;
        var mes = new Competencia(inicioDoPeriodo.Year, inicioDoPeriodo.Month);
        var ultima = Competencia.De(ate);

        while (mes.CompareTo(ultima) <= 0)
        {
            var inicio = inicioDoPeriodo > mes.PrimeiroDia ? inicioDoPeriodo : mes.PrimeiroDia;
            var fim = ate < mes.UltimoDia ? ate : mes.UltimoDia;
            if (fim >= inicio && fim.DayNumber - inicio.DayNumber + 1 >= 15) avos++;
            mes = mes.Proxima;
        }

        return Math.Min(12, avos);
    }

    private static bool EmpregadorPagaAviso(MotivoDoDesligamento motivo) =>
        motivo is MotivoDoDesligamento.SemJustaCausa or MotivoDoDesligamento.AcordoEntreAsPartes;

    private static bool TemDireitoAProporcionais(MotivoDoDesligamento motivo) =>
        motivo != MotivoDoDesligamento.JustaCausa;

    private decimal PercentualDaMulta(MotivoDoDesligamento motivo) => motivo switch
    {
        MotivoDoDesligamento.SemJustaCausa => parametros.MultaFgtsDispensa,
        MotivoDoDesligamento.AcordoEntreAsPartes => parametros.MultaFgtsAcordo,
        _ => 0m,
    };

    private static decimal PercentualDeSaque(MotivoDoDesligamento motivo) => motivo switch
    {
        MotivoDoDesligamento.SemJustaCausa => 1m,
        MotivoDoDesligamento.TerminoDeContrato => 1m,
        MotivoDoDesligamento.AcordoEntreAsPartes => 0.8m,
        _ => 0m,
    };

    private static string Descrever(MotivoDoDesligamento motivo) => motivo switch
    {
        MotivoDoDesligamento.SemJustaCausa => "dispensa sem justa causa",
        MotivoDoDesligamento.PedidoDeDemissao => "pedido de demissão",
        MotivoDoDesligamento.JustaCausa => "dispensa por justa causa",
        MotivoDoDesligamento.AcordoEntreAsPartes => "acordo entre as partes",
        _ => "término de contrato",
    };
}
