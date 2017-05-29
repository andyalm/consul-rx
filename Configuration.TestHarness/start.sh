#!/usr/bin/env bash

dotnet build
dotnet publish
docker-compose up --build -d

trap ctrl_c INT

function ctrl_c() {
   docker-compose down
}

docker-compose logs -f

