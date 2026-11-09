# Contratos de Frequência e Relatórios

Os DTOs são projeções de resposta da Application, sem entidades, consultas implementadas ou dependência de EF Core.

- O resumo individual reutiliza `MemberFrequencyCounts`; não existe uma segunda estrutura de contagens individuais.
- Totais globais usam `long`, pois podem ultrapassar `int.MaxValue` ao agregar membros.
- Percentuais usam `decimal?`, sem arredondamento interno. O percentual global deve usar `FrequencyAggregator`: primeiro somar presenças e faltas injustificadas, depois dividir. Nunca calcular a média dos percentuais individuais.
- Faltas justificadas aparecem nas respostas, mas não entram no numerador nem no denominador. Sem denominador, o percentual é `null`.
- As interfaces não calculam nem validam os valores dos DTOs. A futura implementação deve preservar essas regras e validar contagens negativas.
- O relatório por membros reutiliza os resumos; o detalhamento combina um resumo com o histórico.
- As coleções de resposta devem ser não nulas, usando listas vazias quando não houver resultados. Membro existente sem participação tem contagens zero, percentual nulo e histórico vazio. Resumo ou detalhe nulo indica membro inexistente.
- Para consultar a própria frequência e histórico, a futura camada de aplicação deve obter o `memberId` do contexto autenticado confiável. As interfaces não recebem tokens nem acessam HTTP. A autorização para relatórios e consultas de terceiros deve ocorrer antes da consulta.

## Dependências pendentes de Eventos

- Definir a origem e o significado de `EventDate` (início previsto, realizado ou outra referência) e a convenção de fuso. `DateTimeOffset` é apenas o tipo de transporte escolhido; não pressupõe uma propriedade na entidade futura.
- Definir elegibilidade histórica, quais eventos entram nas consultas e como cancelamentos e reagendamentos afetam contagens e histórico.
- Definir quando a participação pode ser classificada como presença, falta justificada ou injustificada. O enum é de resposta; não define estados de Eventos. Participações ainda não classificadas precisam de uma decisão antes da integração.
- Definir a regra que produz `IsLate`, inclusive seu tratamento para ausências. Nenhuma tolerância ou janela foi presumida.
- Definir fechamento de faltas e aprovação de justificativas, garantindo consistência entre resumo, histórico e relatório global.
- Definir ordenação, filtros de período e paginação quando as regras de consulta estiverem disponíveis. Os contratos atuais não prometem ordenação nem impõem filtros.
- Implementar consultas, persistência e registro de dependências em etapa posterior. Nenhum desses mecanismos é introduzido aqui.
