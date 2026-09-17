namespace Folha.Tests;

public class FeriasTests
{
    private static readonly Ferias Motor = Ferias.Vigente2025;
    private static readonly DateOnly Inicio = new(2025, 9, 1);

    [Theory]
    [InlineData(0, 30)]
    [InlineData(5, 30)]
    [InlineData(6, 24)]
    [InlineData(14, 24)]
    [InlineData(15, 18)]
    [InlineData(23, 18)]
    [InlineData(24, 12)]
    [InlineData(32, 12)]
    [InlineData(33, 0)]
    public void Tabela_de_faltas_do_artigo_130(int faltas, int diasDeDireito)
    {
        Ferias.DiasDeDireito(faltas).Should().Be(diasDeDireito);
    }

    [Fact]
    public void Trinta_dias_de_ferias_pagam_o_salario_mais_um_terco()
    {
        var resultado = Calcular(3_000m);

        resultado.DiasDeGozo.Should().Be(30);
        resultado.Demonstrativo.Valor(Catalogo.Ferias).Should().Be(3_000.00m);
        resultado.Demonstrativo.Valor(Catalogo.TercoDeFerias).Should().Be(1_000.00m);
        resultado.Demonstrativo.TotalProventos.Should().Be(4_000.00m);
    }

    [Fact]
    public void Inss_e_imposto_incidem_sobre_as_ferias_mais_o_terco()
    {
        var demonstrativo = Calcular(3_000m).Demonstrativo;

        demonstrativo.BaseInss.Should().Be(4_000.00m);
        demonstrativo.Valor(Catalogo.Inss).Should().Be(373.41m);
        demonstrativo.Valor(Catalogo.Irrf).Should().Be(114.76m);
        demonstrativo.Liquido.Should().Be(3_511.83m);
    }

    [Fact]
    public void Abono_pecuniario_vende_um_terco_do_direito()
    {
        var resultado = Calcular(3_000m, abono: true);

        resultado.DiasVendidos.Should().Be(10);
        resultado.DiasDeGozo.Should().Be(20);
        resultado.Demonstrativo.Valor(Catalogo.AbonoPecuniario).Should().Be(1_000.00m);
        resultado.Demonstrativo.Valor(Catalogo.TercoDoAbono).Should().Be(333.33m);
    }

    [Fact]
    public void Abono_e_o_terco_dele_saem_sem_inss_e_sem_imposto()
    {
        var demonstrativo = Calcular(3_000m, abono: true).Demonstrativo;

        // Só os 20 dias gozados e o terço deles entram na base.
        demonstrativo.BaseInss.Should().Be(2_666.67m);
        demonstrativo.BaseFgts.Should().Be(2_666.67m);
    }

    [Fact]
    public void Quem_perdeu_dias_por_falta_vende_um_terco_do_que_sobrou()
    {
        var resultado = Calcular(3_000m, abono: true, faltas: 10);

        resultado.DiasDeDireito.Should().Be(24);
        resultado.DiasVendidos.Should().Be(8);
        resultado.DiasDeGozo.Should().Be(16);
    }

    [Fact]
    public void Ferias_fracionadas_pagam_so_os_dias_pedidos()
    {
        var resultado = Motor.Calcular(new PedidoDeFerias
        {
            Empregado = Exemplos.ComSalario(3_000m),
            Inicio = Inicio,
            DiasDeGozo = 15,
        });

        resultado.DiasDeGozo.Should().Be(15);
        resultado.Fim.Should().Be(new DateOnly(2025, 9, 15));
        resultado.Demonstrativo.Valor(Catalogo.Ferias).Should().Be(1_500.00m);
    }

    [Fact]
    public void Quem_faltou_demais_perde_as_ferias_inteiras()
    {
        var resultado = Calcular(3_000m, faltas: 40);

        resultado.DiasDeDireito.Should().Be(0);
        resultado.Demonstrativo.TotalProventos.Should().Be(0m);
    }

    [Fact]
    public void Adiantamento_do_decimo_terceiro_pode_sair_junto_das_ferias()
    {
        var resultado = Motor.Calcular(new PedidoDeFerias
        {
            Empregado = Exemplos.ComSalario(3_000m),
            Inicio = Inicio,
            AdiantarDecimoTerceiro = true,
        });

        resultado.Demonstrativo.Valor(Catalogo.AdiantamentoDecimoTerceiro).Should().Be(1_500.00m);
    }

    private static ResultadoDeFerias Calcular(decimal salario, bool abono = false, int faltas = 0) =>
        Motor.Calcular(new PedidoDeFerias
        {
            Empregado = Exemplos.ComSalario(salario),
            Inicio = Inicio,
            AbonoPecuniario = abono,
            FaltasNoPeriodoAquisitivo = faltas,
        });
}
