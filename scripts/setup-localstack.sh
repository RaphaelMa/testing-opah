#!/bin/bash

ENDPOINT=http://localhost:4566
QUEUE_NAME=transactions-created
REGION=us-east-1

echo "Waiting for LocalStack to be ready..."
sleep 5

echo "Creating SQS queue: $QUEUE_NAME"

AWS_ACCESS_KEY_ID=test \
AWS_SECRET_ACCESS_KEY=test \
aws --endpoint-url=$ENDPOINT sqs create-queue \
    --queue-name $QUEUE_NAME \
    --region $REGION 2>/dev/null || echo "Queue may already exist"

echo "Queue created successfully"
echo "Queue URL: $ENDPOINT/000000000000/$QUEUE_NAME"
