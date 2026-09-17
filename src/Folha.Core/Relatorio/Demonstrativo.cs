using System.Globalization;
using System.Text;
using Folha.Core.Cadastro;
using Folha.Core.Comum;
using Folha.Core.Rubricas;

namespace Folha.Core.Relatorio;

/// <summary>
/// Demonstrativo de pagamento. Além de guardar as linhas, é ele que sabe somar as
/// bases: cada base é a soma dos proventos que incidem menos os descontos que
/// reduzem aquela mesma base. Por isso o motor lança na ordem e pergunta a base
/// no meio do caminho, exatamente como o holerite é montado na prática.
/// </summary>
public sealed class Demonstrativo(Empregado empregado, Competencia competencia, string titulo)
{
    /// <summary>
    /// Formato brasileiro montado na mão em vez de pedir a cultura pt-BR: a API roda
    /// com globalização invariante e lá a cultura simplesmente não existe.
    /// </summary>
    private static readonly NumberFormatInfo Brasil = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
        NegativeSign = "-",
    };

    private readonly List<Lancamento> _itens = [];

    public Empregado Empregado { get; } = empregado;

    public Competencia Competencia { get; } = competencia;

    public string Titulo { get; } = titulo;

    public IReadOnlyList<Lancamento> Itens => _itens;

    /// <summary>Lança uma verba. Valor zerado não vira linha no demonstrativo.</summary>
    public Demonstrativo Lancar(Rubrica rubrica, decimal valor, decimal referencia = 0m, string unidade = "")
    {
        var arredondado = Dinheiro.Centavos(valor);
        if (arredondado != 0m) _itens.Add(new Lancamento(rubrica, arredondado, referencia, unidade));
        return this;
    }

    /// <summary>Lança uma linha informativa, que aparece no rodapé sem mexer no líquido.</summary>
    public Demonstrativo Informar(Rubrica rubrica, decimal valor, decimal referencia = 0m, string unidade = "")
    {
        _itens.Add(new Lancamento(rubrica, Dinheiro.Centavos(valor), referencia, unidade));
        return this;
    }

    private decimal Base(Func<Rubrica, bool> incide) => Dinheiro.NaoNegativo(Dinheiro.Centavos(
        _itens.Where(i => !i.EhInformativa && incide(i.Rubrica)).Sum(i => i.ValorComSinal)));

    public decimal BaseInss => Base(r => r.IncideInss);

    public decimal BaseIrrf => Base(r => r.IncideIrrf);

    public decimal BaseFgts => Base(r => r.IncideFgts);

    public decimal TotalProventos => Dinheiro.Centavos(_itens.Where(i => i.EhProvento).Sum(i => i.Valor));

    public decimal TotalDescontos => Dinheiro.Centavos(_itens.Where(i => i.EhDesconto).Sum(i => i.Valor));

    public decimal Liquido => Dinheiro.Centavos(TotalProventos - TotalDescontos);

    /// <summary>Valor lançado numa rubrica, ou zero se ela não apareceu.</summary>
    public decimal Valor(Rubrica rubrica) =>
        Dinheiro.Centavos(_itens.Where(i => i.Rubrica.Codigo == rubrica.Codigo).Sum(i => i.Valor));

    public bool Tem(Rubrica rubrica) => _itens.Any(i => i.Rubrica.Codigo == rubrica.Codigo);

    /// <summary>Demonstrativo em texto, no formato de holerite.</summary>
    public string Imprimir()
    {
        var texto = new StringBuilder();
        var linha = new string('-', 78);

        texto.AppendLine(linha);
        texto.AppendLine($"{Titulo,-56}{Competencia,22}");
        texto.AppendLine($"{Empregado.Matricula} {Empregado.Nome,-40}{Empregado.Cargo,30}");
        texto.AppendLine(linha);
        texto.AppendLine($"{"Cód",-5}{"Descrição",-41}{"Ref.",10}{"Proventos",11}{"Descontos",11}");
        texto.AppendLine(linha);

        foreach (var item in _itens.Where(i => !i.EhInformativa))
        {
            var provento = item.EhProvento ? Moeda(item.Valor) : string.Empty;
            var desconto = item.EhDesconto ? Moeda(item.Valor) : string.Empty;
            texto.AppendLine(
                $"{item.Rubrica.Codigo,-5}{item.Rubrica.Nome,-41}{item.ReferenciaFormatada,10}{provento,11}{desconto,11}");
        }

        texto.AppendLine(linha);
        texto.AppendLine($"{"Totais",-56}{Moeda(TotalProventos),11}{Moeda(TotalDescontos),11}");
        texto.AppendLine($"{"Líquido a receber",-67}{Moeda(Liquido),11}");
        texto.AppendLine(linha);

        foreach (var item in _itens.Where(i => i.EhInformativa))
            texto.AppendLine($"{item.Rubrica.Nome,-56}{Moeda(item.Valor),22}");

        return texto.ToString();
    }

    private static string Moeda(decimal valor) => valor.ToString("N2", Brasil);
}
