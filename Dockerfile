FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UnitConverter.slnx global.json ./
COPY UnitConverter/UnitConverter.csproj UnitConverter/
COPY UnitConverter.Tests/UnitConverter.Tests.csproj UnitConverter.Tests/
RUN dotnet restore UnitConverter.slnx

COPY . ./
RUN dotnet build UnitConverter.slnx --configuration Release --no-restore

FROM build AS test
RUN dotnet test --solution UnitConverter.slnx --configuration Release --no-build \
    --results-directory /testresults -- \
    --report-trx --report-trx-filename tests.trx \
    --coverlet --coverlet-output-format cobertura --coverlet-include "[UnitConverter]*"

FROM build AS publish
RUN dotnet publish UnitConverter/UnitConverter.csproj --configuration Release --no-build --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=publish /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "UnitConverter.dll"]
