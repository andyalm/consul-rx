#!/bin/bash

sleep 10

/register-service.sh service1-http node1 10.0.0.1
/register-service.sh service2-http node2 10.0.0.2

/set-kv.sh apps/harness/features/feature1 on
/set-kv.sh apps/harness/features/feature2 off
/set-kv.sh apps/harness/features/feature3 on