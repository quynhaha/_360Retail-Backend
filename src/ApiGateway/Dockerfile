FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ApiGateway/_360Retail.ApiGateway.csproj", "src/ApiGateway/"]
RUN dotnet restore "src/ApiGateway/_360Retail.ApiGateway.csproj"
COPY . .
WORKDIR "/src/src/ApiGateway"
RUN dotnet build "_360Retail.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_360Retail.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_360Retail.ApiGateway.dll"]
