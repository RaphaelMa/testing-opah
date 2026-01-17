# Sistema de Controle de Fluxo de Caixa

Sistema distribuído para controle de fluxo de caixa com dois microserviços:

- **TransactionsService**: Registra lançamentos financeiros (débitos e créditos)
- **ConsolidatedService**: Gera saldos consolidados diários automaticamente

A comunicação é assíncrona via SQS. Se o serviço de consolidação cair, o de lançamentos continua funcionando normalmente - os eventos ficam na fila e são processados quando o serviço voltar.

## 🚀 Como Rodar (Forma Mais Fácil)

**Pré-requisito:** Docker e Docker Compose instalados

```bash
# 1. Subir tudo de uma vez
./scripts/setup.sh

# 2. Aguardar uns 30 segundos para tudo inicializar
# (migrations rodam automaticamente)

# 3. Pronto! Tudo está rodando:
#    - Kong Gateway: http://localhost:8000
#    - Transactions API: http://localhost:5002
#    - Consolidated API: http://localhost:5001
```

**Para parar tudo:**
```bash
docker compose down
```

**Para ver os logs:**
```bash
docker compose logs -f
```

**Para verificar se está tudo funcionando:**
```bash
curl http://localhost:5002/health
curl http://localhost:5001/health
```

## 📋 O Que Acontece Quando Você Roda

1. **Docker Compose sobe:**
   - 2 bancos PostgreSQL (um para cada serviço)
   - LocalStack (simula AWS SQS localmente)
   - Kong (API Gateway)
   - 2 APIs .NET (Transactions e Consolidated)

2. **Migrations rodam automaticamente** quando as APIs iniciam

3. **Fila SQS é criada** automaticamente pelo script

4. **Tudo fica pronto** em ~30 segundos

## 🔄 Como Funciona

**Fluxo de criação de lançamento:**
```
1. Você faz POST /api/transactions
2. TransactionsService salva no banco
3. TransactionsService publica evento no SQS
4. ConsolidatedService (em background) pega o evento
5. ConsolidatedService atualiza o saldo diário
```

**Fluxo de consulta:**
```
1. Você faz GET /api/dailybalances?merchantId=xxx&date=2024-01-17
2. ConsolidatedService retorna o saldo pré-calculado
```

## 📡 Endpoints

**Via Kong (recomendado):**
- `POST http://localhost:8000/api/transactions` - Criar lançamento
- `GET http://localhost:8000/api/dailybalances?merchantId={id}&date={date}` - Saldo do dia
- `GET http://localhost:8000/api/dailybalances?merchantId={id}&startDate={date}&endDate={date}` - Saldo por período

**Direto nos serviços:**
- Transactions: `http://localhost:5002/api/transactions`, `/swagger`, `/health`
- Consolidated: `http://localhost:5001/api/dailybalances`, `/swagger`, `/health`

**Exemplo de criação de transação:**
```bash
curl -X POST http://localhost:8000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "merchantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "type": 1,
    "amount": 100.50,
    "transactionDate": "2024-01-17T10:00:00Z",
    "description": "Venda teste"
  }'
```

## 🧪 Testes

**Unitários:**
```bash
cd transactions-service && dotnet test
cd consolidated-service && dotnet test
```

**Teste de carga:**
```bash
# Direto no serviço (bypass Kong)
./scripts/test-stress/load-test-direct.sh http://localhost:5002 50 60

# Ou via Kong
./scripts/test-stress/load-test-direct.sh http://localhost:8000 50 60
```

**Teste de falha e recuperação:**
```bash
./scripts/test-stress/test-failure-recovery.sh
```
Este teste verifica que:
- TransactionsService continua funcionando quando ConsolidatedService cai
- Eventos acumulam na fila
- Quando ConsolidatedService volta, processa tudo

## 🏗️ Arquitetura

**Padrões usados:**
- Clean Architecture (Domain, Application, Infrastructure, API)
- Repository Pattern
- Event-Driven Architecture
- API Gateway (Kong)

**Tecnologias:**
- .NET 8.0
- PostgreSQL (um banco por serviço)
- AWS SQS (via LocalStack)
- Kong (API Gateway)
- Docker Compose

**Detalhes técnicos:**
- [Arquitetura completa](./docs/arquitetura.md)
- [C4 Model](./docs/c4-model.md)

## 🎯 Diferenciais Implementados

**Circuit Breaker customizado:**
- Protege contra falhas em cascata
- Abre após 5 falhas consecutivas
- Fica aberto por 30 segundos
- Estados: Closed → Open → Half-Open → Closed

**Health Checks:**
- Endpoint `/health` em cada serviço
- Verifica conexão com banco e SQS
- Útil para monitoramento

**Correlation ID:**
- Middleware que propaga ID único entre requisições
- Facilita rastreamento em sistemas distribuídos

**Docker Compose completo:**
- Tudo sobe com um comando
- Migrations automáticas
- Health checks configurados
- Pronto para desenvolvimento e testes

**Logs estruturados:**
- Serilog configurado
- Logs em formato estruturado
- Fácil integração com ferramentas de observabilidade

## 🔧 Desenvolvimento Local (Sem Docker para APIs)

Se você quiser rodar as APIs localmente (com hot reload, debug, etc):

**1. Subir apenas infraestrutura:**
```bash
docker compose up -d postgres-transactions postgres-consolidated localstack kong
sleep 10
./scripts/setup-localstack.sh
```

**2. Rodar migrations:**
```bash
cd transactions-service/src/TransactionsService.Api
dotnet ef database update --project ../TransactionsService.Infrastructure

cd ../../consolidated-service/src/ConsolidatedService.Api
dotnet ef database update --project ../ConsolidatedService.Infrastructure
```

**3. Rodar os serviços:**
```bash
# Terminal 1
cd transactions-service/src/TransactionsService.Api
dotnet run

# Terminal 2
cd consolidated-service/src/ConsolidatedService.Api
dotnet run
```

**Importante:** Ajuste as connection strings nos `appsettings.json` para apontar para `localhost` ao invés dos nomes dos serviços Docker.

## 🛡️ Segurança

**Implementado:**
- Rate limiting no Kong (5000/min transactions, 100/min consolidated)
- Validação de entrada em todos os endpoints
- Bancos isolados por serviço

**Para produção:**
- OAuth 2.0/JWT no Kong
- HTTPS obrigatório
- Criptografia em repouso
- Secrets management

## 💭 Decisões Arquiteturais

**Microserviços:** Escolhi microserviços para isolar falhas. Se um serviço cair, o outro continua funcionando. Trade-off: mais complexidade operacional, mas maior resiliência.

**Comunicação assíncrona:** Uso SQS para desacoplar os serviços. O TransactionsService não precisa esperar o ConsolidatedService processar. Trade-off: consistência eventual, mas maior disponibilidade.

**Bancos separados:** Cada serviço tem seu próprio banco. Isso isola falhas e permite escalar independentemente. Trade-off: possível duplicação de dados, mas isolamento total.

**Kong como API Gateway:** Centraliza rate limiting, CORS, roteamento. Alinha com requisito de gestão de APIs (Apigee). Trade-off: ponto único de falha, mas facilita evolução e manutenção.

**LocalStack:** Simula AWS localmente. Perfeito para desenvolvimento e testes. Trade-off: pequenas diferenças do AWS real, mas suficiente para o escopo.

## 🔄 CI/CD Pipeline

O projeto inclui pipeline GitLab CI/CD configurado (`.gitlab-ci.yml`) com:

- **Build**: Compilação dos serviços .NET
- **Test**: Execução de testes unitários com cobertura
- **Docker Build**: Construção de imagens Docker e push para registry
- **Deploy**: Deploy manual para staging e produção

**Detalhes:** Veja [documentação do pipeline](./docs/gitlab-ci-explicacao.md)

## 🚧 Evoluções Futuras

**Curto prazo:**
- OAuth 2.0/JWT no Kong
- Idempotência de eventos
- Cache Redis para saldos
- APM e dashboards

**Médio prazo:**
- BFF no gateway
- Event sourcing
- CQRS no ConsolidatedService

**Longo prazo:**
- Kafka para streaming
- Service mesh (Istio/Linkerd)
- Integração bancária

## 📁 Estrutura do Projeto

```
test-tecninco-opah/
├── transactions-service/          # Microserviço de transações
│   ├── src/
│   │   ├── TransactionsService.Domain/
│   │   ├── TransactionsService.Application/
│   │   ├── TransactionsService.Infrastructure/
│   │   ├── TransactionsService.Api/
│   │   └── TransactionsService.Tests.Unit/
│   └── Dockerfile
├── consolidated-service/           # Microserviço de consolidação
│   ├── src/
│   │   ├── ConsolidatedService.Domain/
│   │   ├── ConsolidatedService.Application/
│   │   ├── ConsolidatedService.Infrastructure/
│   │   ├── ConsolidatedService.Api/
│   │   └── ConsolidatedService.Tests.Unit/
│   └── Dockerfile
├── api-gateway/                   # Configuração do Kong
│   └── kong.yml
├── docs/                          # Documentação de arquitetura
│   ├── arquitetura.md
│   └── c4-model.md
├── scripts/                       # Scripts de setup e testes
│   ├── setup.sh                   # Setup completo
│   ├── setup-localstack.sh        # Configura SQS
│   └── test-stress/               # Testes de carga e falha
├── docker-compose.yml             # Orquestração de tudo
└── README.md
```

## ❓ Problemas Comuns

**Porta já em uso:**
```bash
# O script setup.sh já tenta limpar portas automaticamente
# Se ainda der problema:
docker compose down
lsof -ti :5002 :5001 | xargs kill -9 2>/dev/null
./scripts/setup.sh
```

**Migrations não rodaram:**
- As migrations rodam automaticamente quando as APIs iniciam
- Se der problema, verifique os logs: `docker compose logs transactions-api`

**LocalStack não está criando a fila:**
```bash
./scripts/setup-localstack.sh
```

**Kong não está roteando:**
- Verifique se os serviços estão rodando: `docker compose ps`
- Verifique os logs do Kong: `docker compose logs kong`

---

**Desenvolvido com foco em:**
- Resiliência (circuit breaker, health checks)
- Observabilidade (logs estruturados, correlation ID)
- Facilidade de uso (Docker Compose, setup automatizado)
- Boas práticas (Clean Architecture, testes)
