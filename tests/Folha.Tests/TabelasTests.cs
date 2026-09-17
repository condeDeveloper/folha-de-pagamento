namespace Folha.Tests;

public class TabelasTests
{
    private static readonly TabelaInss Previdencia = TabelaInss.Vigente2025;
    private static readonly TabelaIrrf Renda = TabelaIrrf.Vigente2025;
    private static readonly Parametros Legais = Parametros.Vigente2025;

    [Fact]
    public void Inss_cobra_apenas_a_primeira_aliquota_de_quem_ganha_o_minimo()
    {
        // 1.518,00 x 7,5%
        Previdencia.Contribuicao(1_518.00m).Should().Be(113.85m);
    }

    [Fact]
    public void Inss_e_progressivo_faixa_a_faixa()
    {
        // 1.518,00 a 7,5% + 1.275,88 a 9% + 206,12 a 12%
        Previdencia.Contribuicao(3_000.00m).Should().Be(253.41m);
    }

    [Fact]
    public void Inss_para_no_teto_por_mais_que_o_salario_suba()
    {
        var noTeto = Previdencia.Contribuicao(Previdencia.Teto);

        noTeto.Should().Be(951.63m);
        Previdencia.Contribuicao(50_000.00m).Should().Be(noTeto);
    }

    [Fact]
    public void Aliquota_efetiva_do_inss_fica_bem_abaixo_da_nominal()
    {
        // Quem ganha o teto contribui com 11,66%, não com os 14% da última faixa.
        Previdencia.AliquotaEfetiva(Previdencia.Teto).Should().BeApproximately(0.1166m, 0.0001m);
        Previdencia.AliquotaEfetiva(Previdencia.Teto).Should().BeLessThan(Previdencia.Faixas[^1].Aliquota);
    }

    [Fact]
    public void Inss_de_base_zerada_ou_negativa_nao_vira_credito()
    {
        Previdencia.Contribuicao(0m).Should().Be(0m);
        Previdencia.Contribuicao(-500m).Should().Be(0m);
    }

    [Fact]
    public void Contribuicao_do_inss_cresce_de_forma_monotona()
    {
        var anterior = 0m;
        for (var salario = 500m; salario <= 12_000m; salario += 250m)
        {
            var atual = Previdencia.Contribuicao(salario);
            atual.Should().BeGreaterThanOrEqualTo(anterior);
            anterior = atual;
        }
    }

    [Fact]
    public void Irrf_isenta_quem_esta_na_primeira_faixa()
    {
        Renda.Imposto(2_428.80m).Should().Be(0m);
        Renda.LimiteDeIsencao.Should().Be(2_428.80m);
    }

    [Fact]
    public void Irrf_aplica_a_aliquota_da_faixa_menos_a_parcela_a_deduzir()
    {
        // 5.000 x 27,5% - 908,73
        Renda.Imposto(5_000.00m).Should().Be(466.27m);
    }

    [Fact]
    public void Irrf_nunca_devolve_imposto_negativo()
    {
        Renda.Imposto(100m).Should().Be(0m);
        Renda.Imposto(-100m).Should().Be(0m);
    }

    [Fact]
    public void Salario_de_tres_mil_paga_menos_imposto_pelo_simplificado()
    {
        var resultado = Irrf.Calcular(3_000.00m, Previdencia.Contribuicao(3_000.00m), 0, Renda, Legais);

        resultado.UsouSimplificado.Should().BeTrue();
        resultado.Imposto.Should().Be(0m);
        resultado.BaseTributavel.Should().Be(2_392.80m);
    }

    [Fact]
    public void Salario_alto_volta_a_compensar_pelas_deducoes_legais()
    {
        var resultado = Irrf.Calcular(10_000.00m, Previdencia.Contribuicao(10_000.00m), 0, Renda, Legais);

        resultado.UsouSimplificado.Should().BeFalse();
        resultado.DeducaoLegal.Should().Be(951.63m);
        resultado.Imposto.Should().Be(1_579.57m);
    }

    [Fact]
    public void O_motor_sempre_retem_o_menor_dos_dois_caminhos()
    {
        for (var bruto = 1_000m; bruto <= 20_000m; bruto += 137m)
        {
            var inss = Previdencia.Contribuicao(bruto);
            var resultado = Irrf.Calcular(bruto, inss, 1, Renda, Legais);

            var pelaLegal = Renda.Imposto(bruto - inss - Legais.DeducaoPorDependente);
            var pelaSimplificada = Renda.Imposto(bruto - Math.Min(Legais.DescontoSimplificado, bruto));

            resultado.Imposto.Should().Be(Math.Min(pelaLegal, pelaSimplificada));
        }
    }

    [Fact]
    public void Cada_dependente_derruba_o_imposto_pela_deducao_da_tabela()
    {
        var semDependentes = Irrf.Calcular(10_000m, Previdencia.Contribuicao(10_000m), 0, Renda, Legais);
        var comDois = Irrf.Calcular(10_000m, Previdencia.Contribuicao(10_000m), 2, Renda, Legais);

        (semDependentes.Imposto - comDois.Imposto)
            .Should().BeApproximately(2 * Legais.DeducaoPorDependente * 0.275m, 0.01m);
    }

    [Fact]
    public void Aposentado_com_sessenta_e_cinco_anos_ganha_uma_parcela_isenta_a_mais()
    {
        var comum = Irrf.Calcular(10_000m, Previdencia.Contribuicao(10_000m), 0, Renda, Legais);
        var idoso = Irrf.Calcular(
            10_000m, Previdencia.Contribuicao(10_000m), 0, Renda, Legais, aposentadoComSessentaECinco: true);

        idoso.Imposto.Should().BeLessThan(comum.Imposto);
        idoso.DeducaoLegal.Should().Be(comum.DeducaoLegal + Legais.IsencaoMaiorDeSessentaECinco);
    }

    [Fact]
    public void Pensao_alimenticia_reduz_a_base_do_imposto()
    {
        var sem = Irrf.Calcular(10_000m, Previdencia.Contribuicao(10_000m), 0, Renda, Legais);
        var com = Irrf.Calcular(10_000m, Previdencia.Contribuicao(10_000m), 0, Renda, Legais, pensaoAlimenticia: 1_500m);

        (sem.Imposto - com.Imposto).Should().BeApproximately(1_500m * 0.275m, 0.01m);
    }

    [Fact]
    public void Desconto_simplificado_nunca_passa_do_proprio_rendimento()
    {
        var resultado = Irrf.Calcular(400m, 30m, 0, Renda, Legais);

        resultado.DeducaoSimplificada.Should().Be(400m);
        resultado.Imposto.Should().Be(0m);
    }
}
