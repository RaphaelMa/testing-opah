#!/bin/bash

# Script de carga testando diretamente no serviço (bypass Kong)
# Útil para validar que o problema não é o rate limiting

ENDPOINT="${1:-http://localhost:5002}"
RATE="${2:-50}"
DURATION="${3:-60}"
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

echo "🚀 Teste de carga direto no serviço (bypass Kong)"
echo "Endpoint: $ENDPOINT"
echo "Taxa: $RATE req/s"
echo "Duração: $DURATION segundos"
echo "Merchant ID: $MERCHANT_ID"
echo ""

TEMP_DIR=$(mktemp -d)
SUCCESS_FILE="$TEMP_DIR/success"
FAILED_FILE="$TEMP_DIR/failed"
TOTAL_FILE="$TEMP_DIR/total"

echo 0 > "$SUCCESS_FILE"
echo 0 > "$FAILED_FILE"
echo 0 > "$TOTAL_FILE"

START_TIME=$(date +%s)
END_TIME=$((START_TIME + DURATION))

while [ $(date +%s) -lt $END_TIME ]; do
    BATCH_START=$(date +%s)
    
    PIDS=()
    for i in $(seq 1 $RATE); do
        (
            TYPE=$((RANDOM % 2))
            AMOUNT=$((RANDOM % 10000 + 1))
            AMOUNT_DECIMAL=$(LC_ALL=C awk "BEGIN {printf \"%.2f\", $AMOUNT/100}" | tr -d '\n\r ')
            DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
            
            JSON_PAYLOAD=$(create_json "$MERCHANT_ID" "$TYPE" "$AMOUNT_DECIMAL" "$DATE" "Load test")
            
            JSON_FILE=$(mktemp)
            echo -n "$JSON_PAYLOAD" > "$JSON_FILE"
            
            RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$ENDPOINT/api/transactions" \
                -H "Content-Type: application/json" \
                --data-binary "@$JSON_FILE" 2>/dev/null)
            
            rm -f "$JSON_FILE"
            
            HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
            
            if [ "$HTTP_CODE" -eq 200 ] || [ "$HTTP_CODE" -eq 201 ]; then
                echo 1 >> "$SUCCESS_FILE"
            else
                echo 1 >> "$FAILED_FILE"
                if [ "$HTTP_CODE" != "429" ] && [ "$HTTP_CODE" != "400" ]; then
                    BODY=$(echo "$RESPONSE" | sed '$d' | tr '\n' ' ')
                    echo "❌ HTTP $HTTP_CODE: $BODY" >&2
                elif [ "$HTTP_CODE" -eq "400" ]; then
                    BODY=$(echo "$RESPONSE" | sed '$d' | tr '\n' ' ')
                    if [ -n "$BODY" ]; then
                        echo "❌ HTTP 400: $BODY" >&2
                    fi
                fi
            fi
            echo 1 >> "$TOTAL_FILE"
        ) &
        PIDS+=($!)
    done
    
    for pid in "${PIDS[@]}"; do
        wait $pid 2>/dev/null
    done
    
    BATCH_END=$(date +%s)
    ELAPSED=$((BATCH_END - BATCH_START))
    
    SUCCESS=$(wc -l < "$SUCCESS_FILE" 2>/dev/null || echo 0)
    FAILED=$(wc -l < "$FAILED_FILE" 2>/dev/null || echo 0)
    TOTAL=$(wc -l < "$TOTAL_FILE" 2>/dev/null || echo 0)
    
    if [ $ELAPSED -lt 1 ]; then
        sleep 1
    fi
    
    echo "📊 Progresso: $TOTAL requisições | Sucesso: $SUCCESS | Falhas: $FAILED"
done

SUCCESS=$(wc -l < "$SUCCESS_FILE" 2>/dev/null || echo 0)
FAILED=$(wc -l < "$FAILED_FILE" 2>/dev/null || echo 0)
TOTAL=$(wc -l < "$TOTAL_FILE" 2>/dev/null || echo 0)

rm -rf "$TEMP_DIR"

echo ""
echo "✅ Teste concluído"
echo "Total: $TOTAL requisições"
if [ $TOTAL -gt 0 ]; then
    SUCCESS_PCT=$(awk "BEGIN {printf \"%.2f\", $SUCCESS*100/$TOTAL}")
    FAILED_PCT=$(awk "BEGIN {printf \"%.2f\", $FAILED*100/$TOTAL}")
    echo "Sucesso: $SUCCESS ($SUCCESS_PCT%)"
    echo "Falhas: $FAILED ($FAILED_PCT%)"
    echo "Taxa de perda: $FAILED_PCT%"
else
    echo "Nenhuma requisição foi processada"
fi
