namespace Folha.Tests;

public class RescisaoTests
{
    private static readonly Rescisao Motor = Rescisao.Vigente2025;
    private static readonly DateOnly Saida = new(2025, 6, 10);

    [Fact]
    public void Aviso_previo_ganha_tres_dias_por_ano_completo_de_casa()
    {
        // Admitido em março de 2020: cinco anos completos em junho de 2025.
        Rescisao.DiasDeAvisoPrevio(Exemplos.HoraRedonda, Saida).Should().Be(45);
    }

    [Fact]
    public void Aviso_previo_nao_passa_de_noventa_dias()
    {
        var veterano = Exemplos.HoraRedonda with { Admissao = new DateOnly(1995, 1, 1) };

        Rescisao.DiasDeAvisoPrevio(veterano, Saida).Should().Be(90);
    }

    [Fact]
    public void Dispensa_sem_justa_causa_paga_o_pacote_inteiro()
    {
        var resultado = Calcular(MotivoDoDesligamento.SemJustaCausa);
        var termo = resultado.Demonstrativo;

        termo.Valor(Catalogo.SaldoDeSalario).Should().Be(1_000.00m);       // 10 dias de 3.000
        termo.Valor(Catalogo.AvisoPrevioIndenizado).Should().Be(4_500.00m); // 45 dias
        termo.Tem(Catalogo.DecimoTerceiroProporcional).Should().BeTrue();
        termo.Tem(Catalogo.FeriasProporcionais).Should().BeTrue();
        resultado.TemSeguroDesemprego.Should().BeTrue();
        resultado.PercentualDeSaque.Should().Be(1m);
    }

    [Fact]
    public void Aviso_indenizado_projeta_o_contrato_e_ganha_avos()
    {
        var resultado = Calcular(MotivoDoDesligamento.SemJustaCausa);

        // 10 de junho mais 45 dias cai em 25 de julho: julho passa a valer um avo.
        resultado.DataDeProjecao.Should().Be(new DateOnly(2025, 7, 25));
        resultado.AvosDeDecimoTerceiro.Should().Be(5);
        resultado.Demonstrativo.Tem(Catalogo.DecimoTerceiroSobreAviso).Should().BeTrue();
    }

    [Fact]
    public void Aviso_trabalhado_nao_projeta_nem_indeniza()
    {
        var resultado = Calcular(MotivoDoDesligamento.SemJustaCausa, avisoTrabalhado: true);

        resultado.DataDeProjecao.Should().Be(Saida);
        resultado.Demonstrativo.Tem(Catalogo.AvisoPrevioIndenizado).Should().BeFalse();
    }

    [Fact]
    public void Multa_do_fgts_e_de_quarenta_por_cento_do_saldo_da_conta()
    {
        var resultado = Calcular(MotivoDoDesligamento.SemJustaCausa, saldoFgts: 10_000m);

        // 10.000 de saldo mais o depósito do mês da rescisão.
        var deposito = resultado.Demonstrativo.Valor(Catalogo.FgtsDoMes);
        resultado.MultaDoFgts.Should().Be(Math.Round((10_000m + deposito) * 0.40m, 2));
    }

    [Fact]
    public void Pedido_de_demissao_sem_cumprir_aviso_desconta_trinta_dias()
    {
        var resultado = Calcular(MotivoDoDesligamento.PedidoDeDemissao);
        var termo = resultado.Demonstrativo;

        termo.Valor(Catalogo.AvisoPrevioDescontado).Should().Be(3_000.00m);
        termo.Tem(Catalogo.AvisoPrevioIndenizado).Should().BeFalse();
        resultado.MultaDoFgts.Should().Be(0m);
        resultado.PercentualDeSaque.Should().Be(0m);
        resultado.TemSeguroDesemprego.Should().BeFalse();
    }

    [Fact]
    public void Pedido_de_demissao_ainda_paga_decimo_terceiro_e_ferias_proporcionais()
    {
        var termo = Calcular(MotivoDoDesligamento.PedidoDeDemissao).Demonstrativo;

        termo.Tem(Catalogo.DecimoTerceiroProporcional).Should().BeTrue();
        termo.Tem(Catalogo.FeriasProporcionais).Should().BeTrue();
    }

    [Fact]
    public void Justa_causa_perde_proporcionais_mas_nao_as_ferias_vencidas()
    {
        var termo = Calcular(MotivoDoDesligamento.JustaCausa, feriasVencidas: 30).Demonstrativo;

        termo.Tem(Catalogo.DecimoTerceiroProporcional).Should().BeFalse();
        termo.Tem(Catalogo.FeriasProporcionais).Should().BeFalse();
        termo.Valor(Catalogo.FeriasVencidas).Should().Be(3_000.00m);
        termo.Valor(Catalogo.TercoDeFeriasVencidas).Should().Be(1_000.00m);
        termo.Valor(Catalogo.SaldoDeSalario).Should().Be(1_000.00m);
    }

    [Fact]
    public void Acordo_paga_metade_do_aviso_e_metade_da_multa()
    {
        var semJustaCausa = Calcular(MotivoDoDesligamento.SemJustaCausa, saldoFgts: 10_000m);
        var acordo = Calcular(MotivoDoDesligamento.AcordoEntreAsPartes, saldoFgts: 10_000m);

        acordo.Demonstrativo.Valor(Catalogo.AvisoPrevioIndenizado)
            .Should().Be(semJustaCausa.Demonstrativo.Valor(Catalogo.AvisoPrevioIndenizado) / 2m);
        acordo.MultaDoFgts.Should().Be(
            Math.Round((10_000m + acordo.Demonstrativo.Valor(Catalogo.FgtsDoMes)) * 0.20m, 2));
        acordo.PercentualDeSaque.Should().Be(0.8m);
        acordo.TemSeguroDesemprego.Should().BeFalse();
    }

    [Fact]
    public void Verbas_indenizatorias_nao_entram_na_base_do_inss()
    {
        var termo = Calcular(MotivoDoDesligamento.SemJustaCausa, feriasVencidas: 30).Demonstrativo;

        // Só o saldo de salário é tributável: aviso e férias indenizadas ficam de fora.
        termo.BaseInss.Should().Be(1_000.00m);
        termo.TotalProventos.Should().BeGreaterThan(termo.BaseInss);
    }

    [Fact]
    public void Aviso_indenizado_tem_fgts_mesmo_sem_inss()
    {
        var termo = Calcular(MotivoDoDesligamento.SemJustaCausa).Demonstrativo;

        termo.BaseFgts.Should().BeGreaterThan(termo.BaseInss);
        termo.Valor(Catalogo.FgtsDoMes).Should().Be(Math.Round(termo.BaseFgts * 0.08m, 2));
    }

    [Fact]
    public void Decimo_terceiro_da_rescisao_tem_inss_com_tabela_propria()
    {
        var termo = Calcular(MotivoDoDesligamento.SemJustaCausa).Demonstrativo;

        termo.Tem(Catalogo.InssSobreDecimoTerceiro).Should().BeTrue();
        termo.Valor(Catalogo.InssSobreDecimoTerceiro)
            .Should().Be(TabelaInss.Vigente2025.Contribuicao(termo.Valor(Catalogo.DecimoTerceiroProporcional)));
    }

    [Fact]
    public void Avos_de_ferias_contam_do_ultimo_aniversario_de_admissao()
    {
        // Admissão em 1º de março de 2020: o período aquisitivo abriu em março de 2025.
        // Março, abril e maio fecham quinze dias; junho para no dia 10 e não conta.
        Rescisao.AvosDeFerias(Exemplos.HoraRedonda, Saida).Should().Be(3);
    }

    [Fact]
    public void Termo_explica_por_escrito_o_que_foi_devido_e_o_que_foi_perdido()
    {
        var resultado = Calcular(MotivoDoDesligamento.JustaCausa);

        resultado.Observacoes.Should().Contain(o => o.Contains("Súmula 171"));
        resultado.Observacoes.Should().Contain(o => o.Contains("Lei 4.090"));
    }

    private static ResultadoDaRescisao Calcular(
        MotivoDoDesligamento motivo,
        bool avisoTrabalhado = false,
        decimal saldoFgts = 0m,
        int feriasVencidas = 0) =>
        Motor.Calcular(new PedidoDeRescisao
        {
            Empregado = Exemplos.ComSalario(3_000m),
            Desligamento = Saida,
            Motivo = motivo,
            AvisoTrabalhado = avisoTrabalhado,
            SaldoDaContaDoFgts = saldoFgts,
            DiasDeFeriasVencidas = feriasVencidas,
        });
}
