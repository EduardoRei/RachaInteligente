# Estágio de Construção (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY ["RachaInteligente/RachaInteligente.csproj", "RachaInteligente/"]
COPY ["RachaInteligente.Web/RachaInteligente.Web.csproj", "RachaInteligente.Web/"]
COPY ["RachaInteligente.Shared/RachaInteligente.Shared.csproj", "RachaInteligente.Shared/"]
RUN dotnet restore "RachaInteligente/RachaInteligente.csproj"

# Copiar o restante dos arquivos
COPY . .

# Estágio de Publicação
FROM build AS publish
WORKDIR "/src/RachaInteligente"
RUN dotnet publish "RachaInteligente.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Configurar a cultura para pt-BR
ENV LANG=pt_BR.UTF-8
ENV LC_ALL=pt_BR.UTF-8
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RachaInteligente.dll"]
