#!/bin/bash

echo "🚀 Setting up the development environment..."

echo "🛑 Verificando portas e containers Docker..."

# Parar containers existentes do projeto primeiro
echo "   Parando containers existentes do projeto..."
docker compose down 2>/dev/null || true

# Parar processos .NET relacionados que podem estar usando as portas
echo "   Parando processos .NET relacionados..."
pkill -9 -f "TransactionsService" 2>/dev/null || true
pkill -9 -f "ConsolidatedService" 2>/dev/null || true
pkill -9 -f "dotnet.*TransactionsService" 2>/dev/null || true
pkill -9 -f "dotnet.*ConsolidatedService" 2>/dev/null || true
sleep 2

PORTS=(5002 5001)
for PORT in "${PORTS[@]}"; do
    echo "   Verificando porta $PORT..."
    
    # Tentar múltiplas vezes (até 5 tentativas)
    MAX_ATTEMPTS=5
    ATTEMPT=0
    
    while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
        ATTEMPT=$((ATTEMPT + 1))
        
        # Verificar se há container Docker rodando usando a porta
        CONTAINER_RUNNING=$(docker ps --filter "publish=$PORT" --format "{{.ID}}" | head -1)
        if [ -n "$CONTAINER_RUNNING" ]; then
            echo "   ⚠️  [Tentativa $ATTEMPT/$MAX_ATTEMPTS] Container Docker na porta $PORT. Parando..."
            docker stop "$CONTAINER_RUNNING" 2>/dev/null || true
            docker rm "$CONTAINER_RUNNING" 2>/dev/null || true
            sleep 2
        fi
        
        # Verificar se há processo local usando a porta
        PIDS=$(lsof -ti :$PORT 2>/dev/null)
        if [ -n "$PIDS" ]; then
            echo "   ⚠️  [Tentativa $ATTEMPT/$MAX_ATTEMPTS] Processo(es) na porta $PORT (PIDs: $PIDS). Parando..."
            # Matar todos os processos encontrados
            for PID in $PIDS; do
                kill -9 "$PID" 2>/dev/null || true
                # Tentar matar processos filhos também
                pkill -9 -P "$PID" 2>/dev/null || true
            done
            sleep 3
        fi
        
        # Verificar se está livre agora
        FINAL_CHECK=$(lsof -ti :$PORT 2>/dev/null)
        if [ -z "$FINAL_CHECK" ]; then
            echo "   ✅ Porta $PORT está livre"
            break
        fi
        
        if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
            echo "   ❌ Porta $PORT ainda em uso após $MAX_ATTEMPTS tentativas."
            echo "   Processos restantes:"
            lsof -i :$PORT 2>/dev/null || echo "      (nenhum processo encontrado)"
            echo ""
            echo "   Tente parar manualmente:"
            echo "      lsof -ti :$PORT | xargs kill -9"
            echo "      docker ps -a --filter 'publish=$PORT' -q | xargs docker rm -f"
            echo "      pkill -9 -f 'TransactionsService\|ConsolidatedService'"
            exit 1
        fi
    done
done

echo ""
echo "📦 Starting Docker containers..."
docker compose up -d

echo "⏳ Waiting for services to be ready..."
sleep 15

echo "📋 Setting up LocalStack SQS queue..."
./scripts/setup-localstack.sh

echo "✅ Setup complete!"
echo ""
echo "All services are running in Docker containers:"
echo "  - Kong Gateway: http://localhost:8000"
echo "  - Transactions API: http://localhost:5002"
echo "  - Consolidated API: http://localhost:5001"
echo ""
echo "Migrations run automatically on startup."
echo ""
echo "To view logs:"
echo "  docker compose logs -f"
echo ""
echo "To stop all services:"
echo "  docker compose down"
echo ""
echo "For local development (without Docker for APIs), see README.md"
