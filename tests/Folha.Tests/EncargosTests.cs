using Folha.Core.Encargos;

namespace Folha.Tests;

public class EncargosTests
{
    private static readonly Parametros Legais = Parametros.Vigente2025;

    [Fact]
    public void Encargos_basicos_saem_direto_das_aliquotas()
    {
        var resultado = CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais);

        resultado.InssPatronal.Should().Be(600.00m);   // 20%
        resultado.Rat.Should().Be(30.00m);             // 1%
        resultado.Terceiros.Should().Be(174.00m);      // 5,8%
        resultado.Fgts.Should().Be(240.00m);           // 8%
    }

    [Fact]
    public void Provisoes_guardam_um_doze_avos_de_ferias_com_terco_e_de_decimo_terceiro()
    {
        var resultado = CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais);

        resultado.ProvisaoDeFerias.Should().Be(333.33m);          // 1/12 x 4/3
        resultado.ProvisaoDeDecimoTerceiro.Should().Be(250.00m);  // 1/12
    }

    [Fact]
    public void Provisao_tambem_gera_encargo_quando_virar_pagamento()
    {
        var resultado = CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais);

        // 20% + 1% + 5,8% + 8% sobre as duas provisões.
        resultado.EncargosSobreProvisoes.Should().Be(203.00m);
    }

    [Fact]
    public void Empregado_de_tres_mil_custa_cerca_de_sessenta_por_cento_a_mais()
    {
        var resultado = CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais);

        resultado.Total.Should().Be(4_830.33m);
        resultado.PercentualSobreSalario.Should().BeApproximately(0.6101m, 0.0001m);
    }

    [Fact]
    public void Fap_maximo_dobra_o_rat()
    {
        var comum = CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais, new PerfilDeEncargos(0.03m));
        var agravado = CustoDoEmpregador.Calcular(
            3_000m, Exemplos.HoraRedonda, Legais, new PerfilDeEncargos(0.03m, Fap: 2m));

        comum.Rat.Should().Be(90.00m);
        agravado.Rat.Should().Be(180.00m);
    }

    [Fact]
    public void Sem_provisionar_o_custo_cai_para_pouco_mais_de_um_terco()
    {
        var resultado = CustoDoEmpregador.Calcular(
            3_000m, Exemplos.HoraRedonda, Legais, new PerfilDeEncargos(ProvisionarFeriasEDecimo: false));

        resultado.ProvisaoDeFerias.Should().Be(0m);
        resultado.EncargosSobreProvisoes.Should().Be(0m);
        resultado.PercentualSobreSalario.Should().BeApproximately(0.348m, 0.0001m);
    }

    [Fact]
    public void Aprendiz_custa_menos_porque_o_fgts_e_de_dois_por_cento()
    {
        var aprendiz = Exemplos.HoraRedonda with { Categoria = Categoria.Aprendiz };
        var resultado = CustoDoEmpregador.Calcular(3_000m, aprendiz, Legais);

        resultado.Fgts.Should().Be(60.00m);
        resultado.Total.Should().BeLessThan(CustoDoEmpregador.Calcular(3_000m, Exemplos.HoraRedonda, Legais).Total);
    }

    [Fact]
    public void Remuneracao_zerada_nao_quebra_a_conta()
    {
        var resultado = CustoDoEmpregador.Calcular(0m, Exemplos.HoraRedonda, Legais);

        resultado.Total.Should().Be(0m);
        resultado.PercentualSobreSalario.Should().Be(0m);
    }
}
