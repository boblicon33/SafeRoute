FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SafeRoute.Shared/SafeRoute.Shared.csproj SafeRoute.Shared/
COPY SafeRoute.MonitorService/SafeRoute.MonitorService.csproj SafeRoute.MonitorService/
RUN dotnet restore SafeRoute.MonitorService/SafeRoute.MonitorService.csproj

COPY . .
RUN dotnet publish SafeRoute.MonitorService/SafeRoute.MonitorService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5002
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SafeRoute.MonitorService.dll"]