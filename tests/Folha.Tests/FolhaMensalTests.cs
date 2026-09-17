namespace Folha.Tests;

public class FolhaMensalTests
{
    private static readonly FolhaMensal Motor = FolhaMensal.Vigente2025;
    private static readonly Competencia Setembro = new(2025, 9);

    [Fact]
    public void Mes_sem_nenhum_evento_e_salario_menos_inss()
    {
        var demonstrativo = Calcular(Exemplos.ComSalario(3_000m));

        demonstrativo.Valor(Catalogo.Salario).Should().Be(3_000.00m);
        demonstrativo.Valor(Catalogo.Inss).Should().Be(253.41m);
        demonstrativo.Valor(Catalogo.Irrf).Should().Be(0m);
        demonstrativo.Liquido.Should().Be(2_746.59m);
    }

    [Fact]
    public void Fgts_e_informativo_e_nao_entra_no_liquido()
    {
        var demonstrativo = Calcular(Exemplos.ComSalario(3_000m));

        demonstrativo.Valor(Catalogo.FgtsDoMes).Should().Be(240.00m);
        demonstrativo.TotalDescontos.Should().Be(253.41m);
    }

    [Fact]
    public void Aprendiz_recolhe_dois_por_cento_de_fgts()
    {
        var aprendiz = Exemplos.ComSalario(1_518m) with { Categoria = Categoria.Aprendiz };

        Calcular(aprendiz).Valor(Catalogo.FgtsDoMes).Should().Be(30.36m);
    }

    [Fact]
    public void Horas_extras_arrastam_o_dsr_e_engordam_a_base_do_inss()
    {
        var demonstrativo = Calcular(Exemplos.HoraRedonda, new Jornada
        {
            HorasExtras50 = 10m,
            Feriados = [new DateOnly(2025, 9, 7)],
        });

        demonstrativo.Valor(Catalogo.HoraExtra50).Should().Be(150.00m);
        demonstrativo.Valor(Catalogo.DsrSobreExtras).Should().Be(23.08m);
        demonstrativo.BaseInss.Should().Be(2_373.08m);
    }

    [Fact]
    public void Falta_reduz_a_base_do_inss_e_nao_so_o_liquido()
    {
        var comFalta = Calcular(Exemplos.ComSalario(3_000m), new Jornada { Faltas = [new DateOnly(2025, 9, 10)] });

        comFalta.Valor(Catalogo.Faltas).Should().Be(100.00m);
        comFalta.Valor(Catalogo.DsrSobreFaltas).Should().Be(100.00m);
        comFalta.BaseInss.Should().Be(2_800.00m);
        comFalta.Valor(Catalogo.Inss).Should().Be(229.41m);
    }

    [Fact]
    public void Salario_familia_sai_para_quem_esta_dentro_do_teto()
    {
        var empregado = Exemplos.ComSalario(1_600m) with
        {
            Dependentes = [Exemplos.Filho(2015), Exemplos.Filho(2018)],
        };

        Calcular(empregado).Valor(Catalogo.SalarioFamilia).Should().Be(130.00m);
    }

    [Fact]
    public void Salario_familia_nao_sai_para_quem_ganha_acima_do_teto()
    {
        var empregado = Exemplos.ComSalario(3_000m) with { Dependentes = [Exemplos.Filho(2015)] };

        Calcular(empregado).Tem(Catalogo.SalarioFamilia).Should().BeFalse();
    }

    [Fact]
    public void Salario_familia_nao_entra_em_base_nenhuma()
    {
        var empregado = Exemplos.ComSalario(1_600m) with { Dependentes = [Exemplos.Filho(2015)] };
        var demonstrativo = Calcular(empregado);

        demonstrativo.BaseInss.Should().Be(1_600.00m);
        demonstrativo.BaseFgts.Should().Be(1_600.00m);
    }

    [Fact]
    public void Quem_fez_quinze_anos_perde_a_cota_do_salario_familia()
    {
        var empregado = Exemplos.ComSalario(1_600m) with { Dependentes = [Exemplos.Filho(2010)] };

        Calcular(empregado).Tem(Catalogo.SalarioFamilia).Should().BeFalse();
    }

    [Fact]
    public void Admitido_no_meio_do_mes_recebe_em_trinta_avos()
    {
        var novato = Exemplos.ComSalario(3_000m) with { Admissao = new DateOnly(2025, 9, 16) };
        var demonstrativo = Calcular(novato);

        demonstrativo.Valor(Catalogo.Salario).Should().Be(1_500.00m);
        demonstrativo.Itens.First().Referencia.Should().Be(15m);
    }

    [Fact]
    public void Periculosidade_vale_trinta_por_cento_do_salario_contratual()
    {
        var empregado = Exemplos.ComSalario(3_000m) with { Periculosidade = true };
        var demonstrativo = Calcular(empregado);

        demonstrativo.Valor(Catalogo.Periculosidade).Should().Be(900.00m);
        demonstrativo.BaseInss.Should().Be(3_900.00m);
    }

    [Fact]
    public void Insalubridade_incide_sobre_o_salario_minimo_e_nao_sobre_o_contratual()
    {
        var empregado = Exemplos.ComSalario(3_000m) with { Insalubridade = GrauDeInsalubridade.Medio };

        Calcular(empregado).Valor(Catalogo.Insalubridade).Should().Be(303.60m);
    }

    [Fact]
    public void Insalubridade_e_periculosidade_nao_acumulam()
    {
        var empregado = Exemplos.ComSalario(3_000m) with
        {
            Insalubridade = GrauDeInsalubridade.Maximo,
            Periculosidade = true,
        };
        var demonstrativo = Calcular(empregado);

        // 30% de 3.000 é mais do que 40% do mínimo: prevalece a periculosidade.
        demonstrativo.Valor(Catalogo.Periculosidade).Should().Be(900.00m);
        demonstrativo.Tem(Catalogo.Insalubridade).Should().BeFalse();
    }

    [Fact]
    public void Vale_transporte_para_nos_seis_por_cento_do_salario()
    {
        var empregado = Exemplos.HoraRedonda with { CustoDiarioDeTransporte = 12.00m };

        Calcular(empregado).Valor(Catalogo.ValeTransporte).Should().Be(132.00m);
    }

    [Fact]
    public void Vale_transporte_barato_desconta_so_o_custo_real()
    {
        var empregado = Exemplos.HoraRedonda with { CustoDiarioDeTransporte = 3.00m };

        // 26 dias úteis de setembro x 3,00
        Calcular(empregado).Valor(Catalogo.ValeTransporte).Should().Be(78.00m);
    }

    [Fact]
    public void Pensao_alimenticia_desconta_mas_derruba_o_imposto()
    {
        var empregado = Exemplos.ComSalario(10_000m);
        var comPensao = Motor.Calcular(new Movimento
        {
            Empregado = empregado,
            Competencia = Setembro,
            PensaoAlimenticia = 1_500m,
        });

        comPensao.Valor(Catalogo.PensaoAlimenticia).Should().Be(1_500m);
        comPensao.Valor(Catalogo.Irrf).Should().Be(1_167.07m);
    }

    [Fact]
    public void Demonstrativo_impresso_mostra_as_rubricas_e_o_liquido()
    {
        var texto = Calcular(Exemplos.ComSalario(3_000m)).Imprimir();

        texto.Should().Contain("Salário base");
        texto.Should().Contain("INSS");
        texto.Should().Contain("Líquido a receber");
        texto.Should().Contain("2.746,59");
    }

    [Fact]
    public void Resumo_fecha_a_folha_somando_os_demonstrativos()
    {
        var demonstrativos = new[]
        {
            Calcular(Exemplos.ComSalario(3_000m)),
            Calcular(Exemplos.ComSalario(3_000m)),
        };

        var resumo = ResumoDaFolha.De(demonstrativos);

        resumo.Empregados.Should().Be(2);
        resumo.LiquidoAPagar.Should().Be(5_493.18m);
        resumo.InssRetido.Should().Be(506.82m);
        resumo.FgtsADepositar.Should().Be(480.00m);
    }

    private static Demonstrativo Calcular(Empregado empregado, Jornada? jornada = null) =>
        Motor.Calcular(new Movimento
        {
            Empregado = empregado,
            Competencia = Setembro,
            Jornada = jornada ?? Jornada.Vazia,
        });
}
