namespace Folha.Tests;

public class JornadaTests
{
    private static readonly Competencia Setembro = new(2025, 9);

    [Fact]
    public void Setembro_de_2025_tem_vinte_e_seis_dias_uteis_e_quatro_repousos()
    {
        // O feriado de 7 de setembro caiu num domingo: não gera repouso a mais
        // nem tira dia útil.
        var feriados = new[] { new DateOnly(2025, 9, 7) };

        Setembro.DiasUteis(feriados).Should().Be(26);
        Setembro.Repousos(feriados).Should().Be(4);
        (Setembro.DiasUteis(feriados) + Setembro.Repousos(feriados)).Should().Be(Setembro.DiasNoMes);
    }

    [Fact]
    public void Feriado_no_meio_da_semana_vira_repouso_e_deixa_de_ser_dia_util()
    {
        var comFeriado = new[] { new DateOnly(2025, 9, 9) };

        Setembro.DiasUteis(comFeriado).Should().Be(25);
        Setembro.Repousos(comFeriado).Should().Be(5);
    }

    [Fact]
    public void Hora_extra_de_cinquenta_por_cento_vale_uma_vez_e_meia_a_hora_normal()
    {
        var resultado = Calcular(new Jornada { HorasExtras50 = 10m });

        // 2.200 / 220 = 10,00 a hora; 10h x 10,00 x 1,5
        resultado.ValorHoraExtra50.Should().Be(150.00m);
    }

    [Fact]
    public void Hora_extra_de_cem_por_cento_vale_o_dobro()
    {
        Calcular(new Jornada { HorasExtras100 = 4m }).ValorHoraExtra100.Should().Be(80.00m);
    }

    [Fact]
    public void Sete_horas_de_relogio_a_noite_valem_oito_horas_reduzidas()
    {
        // Artigo 73, §1º da CLT: a hora noturna tem 52 minutos e 30 segundos.
        var resultado = Calcular(new Jornada { HorasNoturnas = 7m });

        resultado.HorasNoturnasReduzidas.Should().BeApproximately(8m, 0.0001m);
        resultado.AdicionalNoturno.Should().Be(16.00m);
    }

    [Fact]
    public void Dsr_reparte_as_extras_do_mes_pelos_repousos()
    {
        var feriados = new[] { new DateOnly(2025, 9, 7) };
        var resultado = Calcular(new Jornada { HorasExtras50 = 10m, Feriados = feriados });

        // 150,00 / 26 dias úteis x 4 repousos
        resultado.DsrSobreExtras.Should().Be(23.08m);
    }

    [Fact]
    public void Sem_verba_variavel_nao_ha_dsr()
    {
        Calcular(Jornada.Vazia).DsrSobreExtras.Should().Be(0m);
        CalculoDeJornada.Dsr(500m, 0, 4).Should().Be(0m);
    }

    [Fact]
    public void Falta_injustificada_desconta_o_dia_e_o_repouso_da_semana()
    {
        var resultado = Calcular(new Jornada { Faltas = [new DateOnly(2025, 9, 10)] });

        resultado.DescontoDeFaltas.Should().Be(73.33m);   // 2.200 / 30
        resultado.RepousosPerdidos.Should().Be(1);
        resultado.DescontoDeDsr.Should().Be(73.33m);
    }

    [Fact]
    public void Duas_faltas_na_mesma_semana_derrubam_um_repouso_so()
    {
        // 10 e 11 de setembro de 2025 são quarta e quinta da mesma semana.
        var resultado = Calcular(new Jornada
        {
            Faltas = [new DateOnly(2025, 9, 10), new DateOnly(2025, 9, 11)],
        });

        resultado.RepousosPerdidos.Should().Be(1);
        resultado.DescontoDeFaltas.Should().Be(146.67m);
    }

    [Fact]
    public void Faltas_em_semanas_diferentes_derrubam_um_repouso_cada()
    {
        var resultado = Calcular(new Jornada
        {
            Faltas = [new DateOnly(2025, 9, 10), new DateOnly(2025, 9, 18)],
        });

        resultado.RepousosPerdidos.Should().Be(2);
    }

    [Fact]
    public void Domingo_e_a_segunda_seguinte_caem_em_semanas_diferentes()
    {
        // A semana do DSR fecha no domingo: 14/09 pertence à semana anterior a 15/09.
        CalculoDeJornada.RepousosPerdidos([new DateOnly(2025, 9, 14), new DateOnly(2025, 9, 15)])
            .Should().Be(2);
    }

    [Fact]
    public void Atraso_e_cobrado_em_horas_e_nao_em_dias()
    {
        Calcular(new Jornada { MinutosDeAtraso = 90m }).DescontoDeAtrasos.Should().Be(15.00m);
    }

    [Fact]
    public void Falta_fora_da_competencia_e_ignorada()
    {
        var resultado = Calcular(new Jornada { Faltas = [new DateOnly(2025, 8, 20)] });

        resultado.DescontoDeFaltas.Should().Be(0m);
        resultado.RepousosPerdidos.Should().Be(0);
    }

    private static ResultadoDaJornada Calcular(Jornada jornada) =>
        CalculoDeJornada.Calcular(Exemplos.HoraRedonda, jornada, Setembro);
}
