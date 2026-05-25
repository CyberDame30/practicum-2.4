#!/usr/bin/env bash
set -e
dotnet new sln --name Nimble.Modulith --output .
dotnet sln add Nimble.Modulith.AppHost/Nimble.Modulith.AppHost.csproj --solution-folder "_Host"
dotnet sln add Nimble.Modulith.ServiceDefaults/Nimble.Modulith.ServiceDefaults.csproj --solution-folder "_Host"
dotnet sln add Nimble.Modulith.Web/Nimble.Modulith.Web.csproj --solution-folder "_Host"
for m in Users Products Customers Email Reporting; do
  dotnet sln add Nimble.Modulith.$m.Contracts/Nimble.Modulith.$m.Contracts.csproj --solution-folder "$m Module" || true
  dotnet sln add Nimble.Modulith.$m/Nimble.Modulith.$m.csproj --solution-folder "$m Module"
done
