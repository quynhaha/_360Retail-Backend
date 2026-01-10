FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Saas/API/_360Retail.Services.Saas.API.csproj", "src/Services/Saas/API/"]
COPY ["src/Services/Saas/Application/_360Retail.Services.Saas.Application.csproj", "src/Services/Saas/Application/"]
COPY ["src/Services/Saas/Domain/_360Retail.Services.Saas.Domain.csproj", "src/Services/Saas/Domain/"]
COPY ["src/Services/Saas/Infrastructure/_360Retail.Services.Saas.Infrastructure.csproj", "src/Services/Saas/Infrastructure/"]

RUN dotnet restore "src/Services/Saas/API/_360Retail.Services.Saas.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Saas/API"
RUN dotnet build "_360Retail.Services.Saas.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_360Retail.Services.Saas.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_360Retail.Services.Saas.API.dll"]
