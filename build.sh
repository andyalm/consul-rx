#!/usr/bin/env bash

dotnet build
dotnet test ConsulRx.UnitTests/ConsulRx.UnitTests.csproj
dotnet test Configuration.UnitTests/ConsulRx.Configuration.UnitTests.csproj
dotnet test Templating.UnitTests/ConsulRx.Templating.UnitTests.csproj