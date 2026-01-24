FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/HR/360Retail.Services.HR.API/360Retail.Services.HR.API.csproj", "src/Services/HR/360Retail.Services.HR.API/"]
COPY ["src/Services/HR/360Retail.Services.HR.Application/360Retail.Services.HR.Application.csproj", "src/Services/HR/360Retail.Services.HR.Application/"]
COPY ["src/Services/HR/360Retail.Services.HR.Domain/360Retail.Services.HR.Domain.csproj", "src/Services/HR/360Retail.Services.HR.Domain/"]
COPY ["src/Services/HR/360Retail.Services.HR.Infrastructure/360Retail.Services.HR.Infrastructure.csproj", "src/Services/HR/360Retail.Services.HR.Infrastructure/"]

RUN dotnet restore "src/Services/HR/360Retail.Services.HR.API/360Retail.Services.HR.API.csproj"
COPY . .
WORKDIR "/src/src/Services/HR/360Retail.Services.HR.API"
RUN dotnet build "360Retail.Services.HR.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "360Retail.Services.HR.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "360Retail.Services.HR.API.dll"]
