FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Sales/API/_360Retail.Services.Sales.API.csproj", "src/Services/Sales/API/"]
COPY ["src/Services/Sales/Application/_360Retail.Services.Sales.Application.csproj", "src/Services/Sales/Application/"]
COPY ["src/Services/Sales/Domain/_360Retail.Services.Sales.Domain.csproj", "src/Services/Sales/Domain/"]
COPY ["src/Services/Sales/Infrastructure/_360Retail.Services.Sales.Infrastructure.csproj", "src/Services/Sales/Infrastructure/"]
RUN dotnet restore "src/Services/Sales/API/_360Retail.Services.Sales.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Sales/API"
RUN dotnet build "_360Retail.Services.Sales.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_360Retail.Services.Sales.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_360Retail.Services.Sales.API.dll"]
