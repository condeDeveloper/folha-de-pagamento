namespace Folha.Tests;

public class DecimoTerceiroTests
{
    private static readonly DecimoTerceiro Motor = DecimoTerceiro.Vigente2025;

    [Fact]
    public void Ano_inteiro_de_casa_da_doze_avos()
    {
        DecimoTerceiro.Avos(Exemplos.HoraRedonda, 2025).Should().Be(12);
    }

    [Fact]
    public void Mes_de_admissao_com_menos_de_quinze_dias_nao_conta()
    {
        // Admitido em 20 de julho: sobram doze dias no mês.
        var empregado = Exemplos.HoraRedonda with { Admissao = new DateOnly(2025, 7, 20) };

        DecimoTerceiro.Avos(empregado, 2025).Should().Be(5);
    }

    [Fact]
    public void Mes_de_admissao_com_exatos_quinze_dias_conta()
    {
        var empregado = Exemplos.HoraRedonda with { Admissao = new DateOnly(2025, 7, 17) };

        DecimoTerceiro.Avos(empregado, 2025).Should().Be(6);
    }

    [Fact]
    public void Desligamento_no_meio_do_ano_corta_os_avos()
    {
        DecimoTerceiro.Avos(Exemplos.HoraRedonda, 2025, new DateOnly(2025, 6, 20)).Should().Be(6);
    }

    [Fact]
    public void Primeira_parcela_e_metade_da_gratificacao_sem_nenhum_desconto()
    {
        var resultado = Calcular(3_000m);

        resultado.Integral.Should().Be(3_000.00m);
        resultado.PrimeiraParcela.Valor(Catalogo.AdiantamentoDecimoTerceiro).Should().Be(1_500.00m);
        resultado.PrimeiraParcela.TotalDescontos.Should().Be(0m);
        resultado.PrimeiraParcela.Liquido.Should().Be(1_500.00m);
    }

    [Fact]
    public void O_fgts_da_primeira_parcela_sai_no_mes_do_adiantamento()
    {
        Calcular(3_000m).PrimeiraParcela.Valor(Catalogo.FgtsDoMes).Should().Be(120.00m);
    }

    [Fact]
    public void Segunda_parcela_desconta_os_impostos_calculados_sobre_o_total()
    {
        var resultado = Calcular(3_000m);
        var segunda = resultado.SegundaParcela;

        // INSS de 3.000, não de 1.500: por isso o desconto parece grande na segunda.
        segunda.Valor(Catalogo.Inss).Should().Be(253.41m);
        segunda.Valor(Catalogo.DecimoTerceiroAdiantado).Should().Be(1_500.00m);
        segunda.Liquido.Should().Be(1_246.59m);
    }

    [Fact]
    public void As_duas_parcelas_somadas_dao_a_gratificacao_menos_os_impostos()
    {
        var resultado = Calcular(6_000m);
        var inss = resultado.SegundaParcela.Valor(Catalogo.Inss);
        var irrf = resultado.SegundaParcela.Valor(Catalogo.Irrf);

        (resultado.PrimeiraParcela.Liquido + resultado.SegundaParcela.Liquido)
            .Should().Be(resultado.Integral - inss - irrf);
    }

    [Fact]
    public void Media_de_variaveis_integra_a_gratificacao()
    {
        var resultado = Motor.Calcular(new PedidoDeDecimoTerceiro
        {
            Empregado = Exemplos.ComSalario(3_000m),
            Ano = 2025,
            MediaDeVariaveis = 600m,
        });

        resultado.Remuneracao.Should().Be(3_600.00m);
        resultado.Integral.Should().Be(3_600.00m);
    }

    [Fact]
    public void Meio_ano_de_casa_paga_metade_da_gratificacao()
    {
        var empregado = Exemplos.ComSalario(3_000m) with { Admissao = new DateOnly(2025, 7, 1) };
        var resultado = Motor.Calcular(new PedidoDeDecimoTerceiro { Empregado = empregado, Ano = 2025 });

        resultado.Avos.Should().Be(6);
        resultado.Integral.Should().Be(1_500.00m);
    }

    [Fact]
    public void Adiantamento_informado_substitui_a_metade_teorica()
    {
        var resultado = Motor.Calcular(new PedidoDeDecimoTerceiro
        {
            Empregado = Exemplos.ComSalario(3_000m),
            Ano = 2025,
            AdiantamentoJaPago = 1_000m,
        });

        resultado.SegundaParcela.Valor(Catalogo.DecimoTerceiroAdiantado).Should().Be(1_000.00m);
        resultado.SegundaParcela.Liquido.Should().Be(1_746.59m);
    }

    private static ResultadoDoDecimoTerceiro Calcular(decimal salario) =>
        Motor.Calcular(new PedidoDeDecimoTerceiro { Empregado = Exemplos.ComSalario(salario), Ano = 2025 });
}
