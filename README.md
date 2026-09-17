# Folha de Pagamento

[![CI](https://github.com/condeDeveloper/folha-de-pagamento/actions/workflows/ci.yml/badge.svg)](https://github.com/condeDeveloper/folha-de-pagamento/actions/workflows/ci.yml)

Motor de folha de pagamento brasileira em C# e .NET 8. Calcula o demonstrativo mensal, o décimo terceiro nas duas parcelas, o recibo de férias, o termo de rescisão e o custo do empregado para a empresa — com as tabelas de INSS e IRRF de 2025 e a lógica de incidência de cada verba explicada rubrica por rubrica.

## O que faz

- **INSS progressivo** faixa a faixa, como ficou depois da reforma de 2019: quem ganha o teto contribui com 11,66% efetivos, não com os 14% da última faixa.
- **IRRF pelos dois caminhos**: deduções legais (INSS, dependentes, pensão, parcela isenta dos 65 anos) contra o desconto simplificado, retendo sempre o menor — que é o que a Receita chama de opção mais benéfica.
- **Tabela de incidências por rubrica**: o mesmo real pago como hora extra entra em INSS, IRRF e FGTS; pago como férias indenizadas não entra em nenhum dos três. As bases saem da soma dos proventos que incidem menos os descontos que reduzem aquela base, e não de um número solto.
- **Cartão de ponto**: horas extras de 50% e 100%, adicional noturno com a hora reduzida de 52 minutos e 30 segundos, reflexo de tudo isso no DSR e desconto de faltas — a falta injustificada derruba o repouso da própria semana.
- **Adicionais** de insalubridade (sobre o mínimo) e periculosidade (sobre o contratual), que não acumulam: a folha oferece o mais vantajoso e diz por quê.
- **Salário-família, vale-transporte** com teto de 6%, pensão alimentícia, adiantamentos.
- **Décimo terceiro**: avos por mês com quinze dias ou mais, primeira parcela bruta e impostos incidindo sobre a gratificação inteira na segunda.
- **Férias**: tabela de faltas do artigo 130, abono pecuniário de um terço do direito, terço constitucional e adiantamento do décimo terceiro junto.
- **Rescisão** nos cinco motivos, com aviso prévio proporcional de até 90 dias, projeção do aviso indenizado nos avos, multa do FGTS, percentual de saque, seguro-desemprego e uma lista de observações dizendo o que foi devido e o que foi perdido.
- **Custo do empregador**: cota patronal, RAT com FAP, terceiros, FGTS e as provisões de férias e décimo terceiro, com os encargos que elas ainda vão gerar.

## Rodar

```bash
dotnet run --project src/Folha.Api
```

Documentação em http://localhost:5000/docs.

```bash
# demonstrativo mensal com horas extras
curl -s localhost:5000/api/folha/mensal -H 'Content-Type: application/json' -d '{
  "empregado": { "matricula": "0001", "nome": "Ana Ribeiro", "admissao": "2020-03-01", "salarioBase": 2200 },
  "ano": 2025, "mes": 9, "horasExtras50": 10, "feriados": ["2025-09-07"]
}'
# => salário 2.200,00 + extras 150,00 + DSR 23,08, base do INSS 2.373,08

# rescisão sem justa causa
curl -s localhost:5000/api/folha/rescisao -H 'Content-Type: application/json' -d '{
  "empregado": { "matricula": "0001", "nome": "Ana Ribeiro", "admissao": "2020-03-01", "salarioBase": 3000 },
  "desligamento": "2025-06-10", "motivo": "SemJustaCausa", "saldoDaContaDoFgts": 10000
}'
# => aviso de 45 dias indenizado, projeção até 25/07, multa de 40% e saque integral

curl -s localhost:5000/api/rubricas   # catálogo de verbas com as três incidências
curl -s localhost:5000/api/tabelas    # INSS, IRRF e parâmetros em vigor
```

## Testes

```bash
dotnet test
```

108 testes. Entre eles: o teto do INSS batendo com a soma das faixas, a retenção sempre pelo caminho mais barato varrendo faixa por faixa, a hora noturna reduzida (sete horas de relógio valem oito), a falta que derruba um repouso por semana e não um por falta, o abono pecuniário saindo sem INSS enquanto as férias gozadas pagam, os avos que só contam mês com quinze dias, a projeção do aviso indenizado ganhando avos de décimo terceiro, e a justa causa perdendo as proporcionais mas mantendo as férias vencidas.

## Tabelas

Tudo que muda de ano em ano está em `src/Folha.Core/Tabelas`, em três arquivos: nenhuma regra de cálculo carrega número cravado.

| | Valor | Vigência |
|---|---|---|
| Salário mínimo | 1.518,00 | jan/2025 |
| Teto do INSS | 8.157,41 (contribuição máxima 951,63) | jan/2025 |
| Faixa isenta do IRRF | 2.428,80 | mai/2025 |
| Desconto simplificado | 607,20 | mai/2025 |
| Dedução por dependente | 189,59 | mai/2025 |
| Cota do salário-família | 65,00 até 1.906,04 | jan/2025 |

## Arquitetura

```
src/Folha.Core
  Comum/      Dinheiro (centavos), Competencia (dias úteis e repousos)
  Cadastro/   Empregado, Dependente
  Tabelas/    TabelaInss, TabelaIrrf, Parametros
  Rubricas/   Rubrica (incidências), Catalogo, Lancamento
  Calculo/    Jornada, Adicionais, Inss, Irrf, Fgts, SalarioFamilia,
              FolhaMensal, DecimoTerceiro, Ferias, Rescisao
  Encargos/   CustoDoEmpregador
  Relatorio/  Demonstrativo (holerite), ResumoDaFolha
src/Folha.Api   minimal API com Swagger
tests/Folha.Tests
```

O `Demonstrativo` é mais do que um relatório: é ele que sabe somar as bases, e o motor lança as verbas na ordem e pergunta a base no meio do caminho — exatamente como o holerite é montado na prática.

## Licença

MIT
