FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SafeRoute.Shared/SafeRoute.Shared.csproj SafeRoute.Shared/
COPY SafeRoute.OrderService/SafeRoute.OrderService.csproj SafeRoute.OrderService/
RUN dotnet restore SafeRoute.OrderService/SafeRoute.OrderService.csproj

COPY . .
RUN dotnet publish SafeRoute.OrderService/SafeRoute.OrderService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5057
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SafeRoute.OrderService.dll"]