namespace Folha.Core.Rubricas;

/// <summary>
/// Catálogo de verbas. A tabela de incidências é o coração da folha: errar aqui
/// erra o INSS, o imposto e o depósito do FGTS de uma vez só.
/// </summary>
public static class Catalogo
{
    // Proventos do mês
    public static readonly Rubrica Salario = Rubrica.Salarial("001", "Salário base");
    public static readonly Rubrica HoraExtra50 = Rubrica.Salarial("002", "Horas extras 50%");
    public static readonly Rubrica HoraExtra100 = Rubrica.Salarial("003", "Horas extras 100%");
    public static readonly Rubrica DsrSobreExtras = Rubrica.Salarial("004", "DSR sobre horas extras");
    public static readonly Rubrica AdicionalNoturno = Rubrica.Salarial("005", "Adicional noturno");
    public static readonly Rubrica DsrSobreNoturno = Rubrica.Salarial("006", "DSR sobre adicional noturno");
    public static readonly Rubrica Insalubridade = Rubrica.Salarial("007", "Adicional de insalubridade");
    public static readonly Rubrica Periculosidade = Rubrica.Salarial("008", "Adicional de periculosidade");
    public static readonly Rubrica OutrosProventos = Rubrica.Salarial("009", "Outros proventos");

    /// <summary>Benefício previdenciário reembolsado pelo INSS: não é salário, não incide em nada.</summary>
    public static readonly Rubrica SalarioFamilia = Rubrica.Indenizatoria("010", "Salário-família");

    // Décimo terceiro
    public static readonly Rubrica DecimoTerceiro = Rubrica.Salarial("011", "Décimo terceiro salário");
    /// <summary>
    /// O adiantamento sai bruto: INSS e imposto só incidem em dezembro, sobre a
    /// gratificação inteira. O FGTS, esse sim, é depositado já no mês do adiantamento.
    /// </summary>
    public static readonly Rubrica AdiantamentoDecimoTerceiro =
        new("012", "Adiantamento do décimo terceiro", Natureza.Provento, IncideFgts: true);

    // Férias
    public static readonly Rubrica Ferias = Rubrica.Salarial("020", "Férias");
    public static readonly Rubrica TercoDeFerias = Rubrica.Salarial("021", "Um terço constitucional");
    public static readonly Rubrica AbonoPecuniario = Rubrica.Indenizatoria("022", "Abono pecuniário");
    public static readonly Rubrica TercoDoAbono = Rubrica.Indenizatoria("023", "Um terço sobre o abono");

    // Rescisão
    public static readonly Rubrica SaldoDeSalario = Rubrica.Salarial("030", "Saldo de salário");

    /// <summary>Indenizado não tem INSS nem imposto, mas tem FGTS (artigo 15, §1º da Lei 8.036).</summary>
    public static readonly Rubrica AvisoPrevioIndenizado =
        new("031", "Aviso prévio indenizado", Natureza.Provento, IncideFgts: true);

    public static readonly Rubrica DecimoTerceiroSobreAviso =
        new("032", "Décimo terceiro sobre o aviso prévio", Natureza.Provento, IncideFgts: true);

    public static readonly Rubrica FeriasVencidas = Rubrica.Indenizatoria("033", "Férias vencidas indenizadas");
    public static readonly Rubrica TercoDeFeriasVencidas = Rubrica.Indenizatoria("034", "Um terço sobre as férias vencidas");
    public static readonly Rubrica FeriasProporcionais = Rubrica.Indenizatoria("035", "Férias proporcionais indenizadas");
    public static readonly Rubrica TercoDeFeriasProporcionais = Rubrica.Indenizatoria("036", "Um terço sobre as férias proporcionais");
    public static readonly Rubrica MultaFgts = Rubrica.Indenizatoria("037", "Multa rescisória do FGTS");

    /// <summary>Tem tabela própria de INSS e de imposto, por isso não é a rubrica 011.</summary>
    public static readonly Rubrica DecimoTerceiroProporcional =
        new("038", "Décimo terceiro proporcional", Natureza.Provento, IncideFgts: true);

    // Descontos
    public static readonly Rubrica Inss = Rubrica.Desconto("501", "INSS");
    public static readonly Rubrica Irrf = Rubrica.Desconto("502", "IRRF");
    public static readonly Rubrica ValeTransporte = Rubrica.Desconto("503", "Vale-transporte");
    public static readonly Rubrica PensaoAlimenticia = Rubrica.Desconto("504", "Pensão alimentícia");
    public static readonly Rubrica AdiantamentoSalarial = Rubrica.Desconto("505", "Adiantamento salarial");
    public static readonly Rubrica OutrosDescontos = Rubrica.Desconto("506", "Outros descontos");
    public static readonly Rubrica AvisoPrevioDescontado = Rubrica.Desconto("507", "Aviso prévio não cumprido");
    public static readonly Rubrica DecimoTerceiroAdiantado = Rubrica.Desconto("508", "Adiantamento do décimo terceiro pago");
    public static readonly Rubrica InssSobreDecimoTerceiro = Rubrica.Desconto("520", "INSS sobre o décimo terceiro");
    public static readonly Rubrica IrrfSobreDecimoTerceiro = Rubrica.Desconto("521", "IRRF sobre o décimo terceiro");

    public static readonly Rubrica Faltas = Rubrica.DescontoQueReduzBases("510", "Faltas");
    public static readonly Rubrica DsrSobreFaltas = Rubrica.DescontoQueReduzBases("511", "DSR sobre faltas");
    public static readonly Rubrica Atrasos = Rubrica.DescontoQueReduzBases("512", "Atrasos");

    // Informativas
    public static readonly Rubrica BaseFgts = new("901", "Base do FGTS", Natureza.Informativa);
    public static readonly Rubrica FgtsDoMes = new("902", "FGTS do mês", Natureza.Informativa);
    public static readonly Rubrica BaseInss = new("903", "Base do INSS", Natureza.Informativa);
    public static readonly Rubrica BaseIrrf = new("904", "Base do IRRF", Natureza.Informativa);
    public static readonly Rubrica FaixaIrrf = new("905", "Faixa do IRRF", Natureza.Informativa);
    public static readonly Rubrica FgtsDepositado = new("906", "FGTS depositado na conta", Natureza.Informativa);

    /// <summary>Todas as rubricas conhecidas, para expor a tabela de incidências.</summary>
    public static IReadOnlyList<Rubrica> Todas { get; } =
    [
        Salario, HoraExtra50, HoraExtra100, DsrSobreExtras, AdicionalNoturno, DsrSobreNoturno,
        Insalubridade, Periculosidade, OutrosProventos, SalarioFamilia,
        DecimoTerceiro, AdiantamentoDecimoTerceiro,
        Ferias, TercoDeFerias, AbonoPecuniario, TercoDoAbono,
        SaldoDeSalario, AvisoPrevioIndenizado, DecimoTerceiroSobreAviso,
        FeriasVencidas, TercoDeFeriasVencidas, FeriasProporcionais, TercoDeFeriasProporcionais, MultaFgts,
        DecimoTerceiroProporcional,
        Inss, Irrf, ValeTransporte, PensaoAlimenticia, AdiantamentoSalarial, OutrosDescontos, AvisoPrevioDescontado, DecimoTerceiroAdiantado,
        InssSobreDecimoTerceiro, IrrfSobreDecimoTerceiro,
        Faltas, DsrSobreFaltas, Atrasos,
        BaseFgts, FgtsDoMes, BaseInss, BaseIrrf, FaixaIrrf, FgtsDepositado,
    ];
}
