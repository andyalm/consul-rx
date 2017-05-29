#!/bin/bash

CONSUL_HTTP_ADDR=${CONSUL_HTTP_ADDR:-"localhost:8500"}
KEY=$1
VALUE=$2


echo "Setting key $KEY to value $VALUE"
curl -X PUT --data $VALUE http://$CONSUL_HTTP_ADDR/v1/kv/$KEY