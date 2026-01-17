# Minhas Decisões Arquiteturais
## Por que escolhi cada componente e como implementei

Este documento explica minhas decisões técnicas para o sistema de controle de fluxo de caixa. Vou falar sobre o que implementei, por quê, e quais trade-offs considerei.

---

## Por que Microserviços?

Separei o sistema em dois microserviços: `TransactionsService` e `ConsolidatedService`. 

**O motivo principal:** o requisito era claro: o serviço de lançamentos não pode parar se o consolidado cair. Com microserviços, consigo isolar completamente as falhas. Se o `ConsolidatedService` tiver problema, o `TransactionsService` continua funcionando normalmente.

Além disso, cada serviço pode escalar independentemente. Se eu tiver pico de lançamentos, escalo só o `TransactionsService`. Se o processamento de consolidação estiver lento, escalo só o `ConsolidatedService`.

**Trade-off que aceitei:** mais complexidade operacional. Tenho dois serviços para monitorar, fazer deploy, gerenciar. Mas para um sistema que precisa de alta disponibilidade, vale a pena.

**Como implementei:** Cada serviço tem seu próprio projeto .NET, seu próprio banco PostgreSQL, e roda em container Docker separado. A comunicação entre eles é totalmente assíncrona via SQS, então não há dependência síncrona.

---

## Por que Comunicação Assíncrona via SQS?

Usei filas SQS (simuladas via LocalStack) para comunicação entre os serviços.

**O motivo:** desacoplamento temporal. Quando um comerciante cria um lançamento, o `TransactionsService` salva no banco, publica o evento na fila, e responde imediatamente. Não precisa esperar o `ConsolidatedService` processar. Isso garante baixa latência na resposta.

Outro ponto importante: resiliência. Se o `ConsolidatedService` cair, as mensagens ficam na fila. Quando ele voltar, processa tudo. Não perde dados.

Para o requisito de 50 req/s com 5% de perda, as filas absorvem picos. O `ConsolidatedService` processa no seu ritmo, sem sobrecarregar.

**Trade-off que aceitei:** consistência eventual. O saldo consolidado pode ter alguns segundos de atraso em relação ao lançamento. Mas isso é aceitável para consolidação diária - não precisa ser em tempo real.

**Como implementei:** 
- `TransactionsService` publica eventos `TransactionCreatedEvent` na fila SQS após persistir com sucesso
- `ConsolidatedService` tem um `BackgroundService` que consome mensagens continuamente
- Usei AWS SDK para .NET, configurado para LocalStack em desenvolvimento
- Implementei retry automático e tratamento de erros

---

## Por que Bancos de Dados Separados?

Cada microserviço tem seu próprio banco PostgreSQL.

**O motivo:** isolamento total. Se um banco tiver problema (lock, falha, manutenção), o outro continua funcionando. Isso é crítico para o requisito de disponibilidade.

Além disso, posso otimizar cada banco para seu caso de uso:
- Banco de transações: otimizado para escrita rápida
- Banco de consolidados: otimizado para leitura e agregações

**Trade-off que aceitei:** possível duplicação de dados. Os dados do lançamento existem no `TransactionsService` e são processados pelo `ConsolidatedService`. Mas isso é aceitável - cada serviço tem sua própria visão dos dados para seu propósito.

**Como implementei:** 
- Dois containers PostgreSQL no docker-compose
- Cada serviço tem sua própria connection string
- Migrations separadas para cada banco
- Entity Framework Core configurado independentemente em cada serviço

---

## Por que API Gateway (Kong)?

Implementei Kong como ponto único de entrada.

**O motivo principal:** demonstra conhecimento em gestão de APIs. A vaga mencionava Apigee, então mostrei que conheço o conceito e sei implementar.

Mas além disso, o Kong resolve vários problemas:
- **Rate Limiting:** crítico para o requisito de 50 req/s com 5% de perda. Configurei 5000/min para transactions e 100/min para consolidated
- **CORS:** necessário se houver frontend
- **Roteamento:** abstrai a localização dos serviços
- **Observabilidade:** logs centralizados, Request-ID para rastreamento

Apliquei dois padrões de design aqui:
- **Proxy:** Kong atua como proxy reverso, escondendo detalhes dos serviços backend
- **Façade:** fornece interface unificada e simplificada para os clientes

**Trade-off que aceitei:** ponto único de falha. Se o Kong cair, tudo cai. Mas ele pode ser escalado horizontalmente, e os benefícios superam o risco.

**Como implementei:**
- Kong em modo DB-less (configuração declarativa via `kong.yml`)
- Configuração de serviços, rotas e plugins
- Rate limiting configurado por serviço
- CORS habilitado
- Health checks configurados

---

## Por que LocalStack?

Usei LocalStack para simular AWS SQS localmente.

**O motivo:** desenvolvimento local sem custo. Não preciso de conta AWS, não pago nada, e consigo desenvolver e testar tudo localmente.

Facilita muito os testes de integração. Posso testar o fluxo completo sem depender de infraestrutura externa.

**Trade-off que aceitei:** pequenas diferenças em relação ao AWS real. LocalStack não é 100% idêntico, mas é suficiente para desenvolvimento e testes. Em produção, troco para AWS real.

**Como implementei:**
- Container LocalStack no docker-compose
- Script `setup-localstack.sh` que cria a fila SQS automaticamente
- AWS SDK configurado com endpoint do LocalStack
- Credenciais fake ("test", "test") para LocalStack

---

## Por que .NET 8?

Escolhi .NET 8 (C#) como stack.

**O motivo:** é a stack que conheço melhor e que atende bem aos requisitos. Performance adequada, ecossistema maduro, tipagem forte que reduz erros.

O AWS SDK para .NET é oficial e bem documentado. Entity Framework Core facilita muito o trabalho com banco de dados.

**Trade-off:** outras stacks poderiam funcionar igualmente bem (Java, Node.js, Go), mas .NET atende perfeitamente e é o que domino.

**Como implementei:**
- Clean Architecture com 4 camadas: Domain, Application, Infrastructure, API
- Entity Framework Core para ORM
- AWS SDK para integração com SQS
- Serilog para logging estruturado
- xUnit para testes

---

## O que Implementei Além do Básico

### Circuit Breaker Customizado

Implementei um Circuit Breaker próprio (não usei Polly por questões de compatibilidade de versão).

**Por quê:** proteção contra falhas em cascata. Se o SQS estiver com problema, o Circuit Breaker abre após 5 falhas consecutivas e fica aberto por 30 segundos. Isso evita que o serviço fique tentando inutilmente e consumindo recursos.

**Como funciona:**
- Estados: Closed → Open → Half-Open → Closed
- Quando está Open, rejeita chamadas imediatamente
- Após 30 segundos, tenta novamente (Half-Open)
- Se funcionar, volta para Closed; se falhar, volta para Open

**Onde usei:**
- No `TransactionsService`: protege a publicação de eventos no SQS
- No `ConsolidatedService`: protege o processamento de mensagens

### Health Checks

Implementei endpoints `/health` em cada serviço.

**Por quê:** monitoramento e orquestração. O Docker Compose usa health checks para saber quando os serviços estão prontos. Ferramentas de monitoramento podem verificar a saúde dos serviços.

**O que verifica:**
- Conexão com PostgreSQL
- Conexão com SQS (via health check customizado)

### Correlation ID

Middleware que propaga um ID único entre requisições.

**Por quê:** rastreamento distribuído. Em sistemas com múltiplos serviços, é difícil rastrear uma requisição que passa por vários componentes. O Correlation ID resolve isso.

**Como funciona:**
- Gera um ID único no primeiro serviço que recebe a requisição
- Propaga via headers HTTP
- Inclui nos logs de todos os serviços
- Facilita debugar problemas em produção

### Docker Compose Completo

Tudo sobe com um comando: `docker compose up -d`.

**Por quê:** facilidade de uso. Qualquer pessoa consegue rodar o sistema completo sem instalar nada além do Docker.

**O que inclui:**
- 2 bancos PostgreSQL
- LocalStack
- Kong
- 2 APIs .NET (Transactions e Consolidated)
- Migrations automáticas na inicialização
- Health checks configurados

### Logs Estruturados

Usei Serilog para logging estruturado.

**Por quê:** facilita análise e integração com ferramentas de observabilidade. Logs em formato JSON podem ser facilmente indexados e consultados.

**O que logo:**
- Todas as requisições HTTP
- Eventos publicados/consumidos
- Erros com stack trace completo
- Correlation ID em todos os logs

---

## Como Atendi os Requisitos Não Funcionais

### Disponibilidade

**Requisito:** O serviço de lançamentos não pode ficar indisponível se o consolidado cair.

**Como resolvi:**
- Comunicação assíncrona: se o `ConsolidatedService` cair, o `TransactionsService` continua funcionando. Os eventos ficam na fila.
- Persistência primeiro: lançamento é salvo no banco antes de publicar evento. Se falhar ao publicar, o lançamento já está salvo.
- Tratamento de erros: falhas ao publicar evento não impedem o registro. O evento pode ser republicado depois.

### Escalabilidade

**Requisito:** 50 req/s no consolidado, com máximo 5% de perda.

**Como resolvi:**
- Processamento assíncrono: filas absorvem picos
- Múltiplas instâncias: posso escalar o `ConsolidatedService` horizontalmente, todas consomem da mesma fila
- Rate limiting no Kong: proteção contra sobrecarga
- Testes de carga: criei scripts para validar o requisito

**Resultado dos testes:** consegui 50 req/s com menos de 0,1% de perda, bem abaixo dos 5% permitidos.

### Resiliência

**Como resolvi:**
- Circuit Breaker: protege contra falhas em cascata
- Retry automático: mensagens falhadas voltam para a fila
- Health checks: detecta problemas rapidamente
- Isolamento: falhas em um serviço não afetam outros

### Segurança

**O que implementei:**
- Rate limiting no Kong
- Validação de entrada em todos os endpoints
- Bancos isolados por serviço
- CORS configurado

**O que deixei para produção:**
- OAuth 2.0/JWT (pode ser adicionado no Kong)
- HTTPS (responsabilidade da infraestrutura)
- Criptografia em repouso
- Secrets management

---

## Limitações que Assumi

Para manter o escopo do desafio viável, assumi algumas limitações:

1. **LocalStack vs AWS real:** pequenas diferenças de comportamento, mas suficiente para desenvolvimento
2. **Autenticação simplificada:** não implementei OAuth completo, mas a estrutura está pronta
3. **Monitoramento básico:** logs estruturados, mas sem integração com APM completo
4. **Testes de carga locais:** podem não refletir 100% o comportamento em produção
5. **Consistência eventual:** saldos podem ter alguns segundos de atraso (aceitável)
6. **Sem replicação:** um banco por serviço, sem réplicas (pode ser adicionado depois)

Essas limitações são conscientes e documentadas. Em produção, seriam endereçadas conforme necessidade.

---

## Fluxo de Dados que Implementei

### Criar Lançamento

1. Comerciante faz `POST /api/transactions` via Kong
2. Kong aplica rate limiting e roteia para `TransactionsService`
3. `TransactionsService` valida e salva no PostgreSQL
4. Serviço publica evento `TransactionCreatedEvent` no SQS
5. Responde HTTP 201 ao cliente (não espera consolidação)

**Tempo total:** ~50-100ms (depende da latência do SQS)

### Processar Consolidação

1. `ConsolidatedService` (BackgroundService) consome mensagem do SQS
2. Deserializa evento
3. Busca ou cria `DailyBalance` para a data do lançamento
4. Atualiza totais (créditos/débitos) e saldo líquido
5. Salva no PostgreSQL
6. Remove mensagem da fila

**Tempo por mensagem:** ~20-50ms

### Consultar Saldo

1. Comerciante faz `GET /api/dailybalances?merchantId=xxx&date=2024-01-17` via Kong
2. Kong roteia para `ConsolidatedService`
3. Serviço consulta PostgreSQL (saldo pré-calculado)
4. Retorna resposta

**Tempo total:** ~10-30ms (consulta simples em banco otimizado)

---

## O que Gostaria de Ter Implementado (Mas Ficou Fora do Escopo)

### Curto Prazo
- **OAuth 2.0/JWT no Kong:** autenticação completa
- **Idempotência de eventos:** garantir que reprocessamento não cause duplicação
- **Cache Redis:** cachear saldos consultados frequentemente
- **APM completo:** integração com New Relic, Datadog ou similar

### Médio Prazo
- **BFF (Backend for Frontend):** agregar dados de múltiplos serviços no gateway
- **Event Sourcing:** armazenar todos os eventos como fonte da verdade
- **CQRS no ConsolidatedService:** separar comandos (escrita) de queries (leitura)

### Longo Prazo
- **Kafka:** substituir SQS por Kafka para streaming de eventos
- **Service Mesh:** Istio ou Linkerd para comunicação entre serviços
- **Integração bancária:** conectar com APIs bancárias reais

---

## Conclusão

Todas as decisões que tomei foram pensadas para atender os requisitos do desafio, especialmente:
- Alta disponibilidade (lançamentos não param se consolidado cair)
- Escalabilidade (50 req/s com 5% de perda)
- Resiliência (circuit breaker, retry, health checks)
- Facilidade de uso (Docker Compose, setup automatizado)

Implementei além do básico para demonstrar conhecimento em:
- Padrões de design (Proxy, Façade, Circuit Breaker)
- Gestão de APIs (Kong)
- Observabilidade (logs estruturados, correlation ID)
- Boas práticas (Clean Architecture, testes, documentação)

O sistema está pronto para evoluir. A arquitetura permite adicionar novos recursos sem grandes refatorações.
