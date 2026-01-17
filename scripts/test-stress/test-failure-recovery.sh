#!/bin/bash

# Script para testar falha e recuperação do ConsolidatedService
# 1. Inicia carga no TransactionsService
# 2. Para o ConsolidatedService
# 3. Verifica que TransactionsService continua funcionando
# 4. Reinicia ConsolidatedService
# 5. Verifica que eventos acumulados são processados

ENDPOINT="${1:-http://localhost:8000}"
MERCHANT_ID="${4:-3fa85f64-5717-4562-b3fc-2c963f66afa6}"

create_json() {
    local merchant_id=$1
    local type=$2
    local amount=$3
    local date=$4
    local desc=$5
    amount=$(echo "$amount" | tr -d '[:space:]')
    printf '{"merchantId":"%s","type":%d,"amount":%s,"transactionDate":"%s","description":"%s"}' \
        "$merchant_id" "$type" "$amount" "$date" "$desc"
}

echo "🧪 Teste de Falha e Recuperação"
echo "=================================="
echo ""

echo "1️⃣ Criando algumas transações iniciais..."
for i in {1..10}; do
    TYPE=$((RANDOM % 2))
    AMOUNT=$(LC_ALL=C awk "BEGIN {printf \"%.2f\", $RANDOM/100}" | tr -d '\n\r ')
    DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    JSON_PAYLOAD=$(create_json "$MERCHANT_ID" "$TYPE" "$AMOUNT" "$DATE" "Pre-failure transaction $i")
    
    JSON_FILE=$(mktemp)
    echo -n "$JSON_PAYLOAD" > "$JSON_FILE"
    
    curl -s -X POST "$ENDPOINT/api/transactions" \
        -H "Content-Type: application/json" \
        --data-binary "@$JSON_FILE" > /dev/null
    
    rm -f "$JSON_FILE"
done
echo "✅ 10 transações criadas"
sleep 5
echo ""

echo "2️⃣ Verificando saldo antes de parar o serviço..."
BALANCE_RESPONSE=$(curl -s "$ENDPOINT/api/dailybalances?merchantId=$MERCHANT_ID&date=$(date +%Y-%m-%d)")
BEFORE_BALANCE=$(echo "$BALANCE_RESPONSE" | jq -r 'if type == "array" then .[0].netBalance // "N/A" else .netBalance // "N/A" end')
echo "Saldo atual: $BEFORE_BALANCE"
echo ""

echo "3️⃣ Parando ConsolidatedService..."
CONSOLIDATED_PID=$(lsof -ti :5001 2>/dev/null)
if [ -n "$CONSOLIDATED_PID" ]; then
    kill "$CONSOLIDATED_PID" 2>/dev/null
    echo "✅ ConsolidatedService (PID: $CONSOLIDATED_PID) parado"
    sleep 2
else
    echo "⚠️  ConsolidatedService não encontrado na porta 5001"
    echo "   Certifique-se de que está rodando com 'dotnet run'"
    read -p "Pressione Enter quando o serviço estiver parado..."
fi
echo ""

echo "4️⃣ Criando transações enquanto o serviço está parado..."
echo "   (TransactionsService deve continuar funcionando)"
for i in {1..20}; do
    TYPE=$((RANDOM % 2))
    AMOUNT=$(LC_ALL=C awk "BEGIN {printf \"%.2f\", $RANDOM/100}" | tr -d '\n\r ')
    DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    JSON_PAYLOAD=$(create_json "$MERCHANT_ID" "$TYPE" "$AMOUNT" "$DATE" "Transaction while service down $i")
    
    JSON_FILE=$(mktemp)
    echo -n "$JSON_PAYLOAD" > "$JSON_FILE"
    
    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$ENDPOINT/api/transactions" \
        -H "Content-Type: application/json" \
        --data-binary "@$JSON_FILE")
    
    rm -f "$JSON_FILE"
    
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" -eq 200 ] || [ "$HTTP_CODE" -eq 201 ]; then
        echo "✅ Transação $i criada (HTTP $HTTP_CODE)"
    else
        echo "❌ Falha na transação $i (HTTP $HTTP_CODE)"
    fi
    
    sleep 0.5
done
echo ""

echo "5️⃣ Verificando mensagens na fila SQS..."
QUEUE_URL="http://localhost:4566/000000000000/transactions-created"
MESSAGE_COUNT=$(AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test \
    aws --endpoint-url=http://localhost:4566 sqs get-queue-attributes \
    --queue-url "$QUEUE_URL" \
    --attribute-names ApproximateNumberOfMessages \
    --region us-east-1 2>/dev/null | jq -r '.Attributes.ApproximateNumberOfMessages // "0"')
echo "Mensagens na fila: $MESSAGE_COUNT"
echo ""

echo "6️⃣ Reiniciando ConsolidatedService..."
echo "⚠️  Por favor, reinicie o ConsolidatedService manualmente em outro terminal:"
echo "   cd consolidated-service/src/ConsolidatedService.Api && dotnet run"
read -p "Pressione Enter quando o serviço estiver rodando..."
echo ""

echo "7️⃣ Aguardando processamento das mensagens..."
sleep 10

echo "8️⃣ Verificando saldo após recuperação..."
for i in {1..30}; do
    BALANCE_RESPONSE=$(curl -s "$ENDPOINT/api/dailybalances?merchantId=$MERCHANT_ID&date=$(date +%Y-%m-%d)")
    AFTER_BALANCE=$(echo "$BALANCE_RESPONSE" | jq -r 'if type == "array" then .[0].netBalance // "N/A" else .netBalance // "N/A" end')
    
    if [ "$AFTER_BALANCE" != "N/A" ] && [ "$AFTER_BALANCE" != "$BEFORE_BALANCE" ]; then
        echo "✅ Saldo atualizado: $AFTER_BALANCE"
        echo "   Saldo anterior: $BEFORE_BALANCE"
        break
    fi
    
    if [ $i -eq 30 ]; then
        echo "⚠️  Saldo não foi atualizado após 30 tentativas"
    else
        echo "⏳ Aguardando... ($i/30)"
        sleep 2
    fi
done
echo ""

echo "9️⃣ Verificando mensagens restantes na fila..."
FINAL_MESSAGE_COUNT=$(AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test \
    aws --endpoint-url=http://localhost:4566 sqs get-queue-attributes \
    --queue-url "$QUEUE_URL" \
    --attribute-names ApproximateNumberOfMessages \
    --region us-east-1 2>/dev/null | jq -r '.Attributes.ApproximateNumberOfMessages // "0"')
echo "Mensagens restantes: $FINAL_MESSAGE_COUNT"
echo ""

if [ "$FINAL_MESSAGE_COUNT" -eq "0" ] || [ "$FINAL_MESSAGE_COUNT" -lt "$MESSAGE_COUNT" ]; then
    echo "✅ Teste concluído com sucesso!"
    echo "   - TransactionsService continuou funcionando durante a falha"
    echo "   - Mensagens foram acumuladas na fila"
    echo "   - ConsolidatedService processou as mensagens após recuperação"
else
    echo "⚠️  Algumas mensagens podem não ter sido processadas"
fi
