FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/CRM/API/_360Retail.Services.CRM.API.csproj", "src/Services/CRM/API/"]
COPY ["src/Services/CRM/Application/_360Retail.Services.CRM.Application.csproj", "src/Services/CRM/Application/"]
COPY ["src/Services/CRM/Domain/_360Retail.Services.CRM.Domain.csproj", "src/Services/CRM/Domain/"]
COPY ["src/Services/CRM/Infrastructure/_360Retail.Services.CRM.Infrastructure.csproj", "src/Services/CRM/Infrastructure/"]

RUN dotnet restore "src/Services/CRM/API/_360Retail.Services.CRM.API.csproj"
COPY . .
WORKDIR "/src/src/Services/CRM/API"
RUN dotnet build "_360Retail.Services.CRM.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "_360Retail.Services.CRM.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "_360Retail.Services.CRM.API.dll"]
