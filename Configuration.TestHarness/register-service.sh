#!/bin/bash

CONSUL_HTTP_ADDR=${CONSUL_HTTP_ADDR:-"localhost:8500"}
SERVICE_NAME=$1
NODE_NAME=$2
SERVICE_ADDRESS=${3:-10.0.0.1}
SERVICE_PORT=${4:-80}

echo "Registering service $SERVICE_NAME at address $SERVICE_ADDRESS"
curl -X PUT --data "{\"Node\": \"$NODE_NAME\", \"Address\": \"$SERVICE_ADDRESS\", \"Service\": { \"Service\": \"$SERVICE_NAME\", \"Port\": $SERVICE_PORT } }" "http://$CONSUL_HTTP_ADDR/v1/catalog/register"