FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Identity/API/_360Retail.Services.Identity.API.csproj", "src/Services/Identity/API/"]
COPY ["src/Services/Identity/Application/_360Retail.Services.Identity.Application.csproj", "src/Services/Identity/Application/"]
COPY ["src/Services/Identity/Domain/_360Retail.Services.Identity.Domain.csproj", "src/Services/Identity/Domain/"]
COPY ["src/Services/Identity/Infrastructure/_360Retail.Services.Identity.Infrastructure.csproj", "src/Services/Identity/Infrastructure/"]
COPY ["src/Services/Saas/Infrastructure/_360Retail.Services.Saas.Infrastructure.csproj", "src/Services/Saas/Infrastructure/"]
COPY ["src/Services/Saas/Application/_360Retail.Services.Saas.Application.csproj", "src/Services/Saas/Application/"]
COPY ["src/Services/Saas/Domain/_360Retail.Services.Saas.Domain.csproj", "src/Services/Saas/Domain/"]

RUN dotnet restore "src/Services/Identity/API/_360Retail.Services.Identity.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Identity/API"
RUN dotnet build "_360Retail.Services.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_360Retail.Services.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_360Retail.Services.Identity.API.dll"]
