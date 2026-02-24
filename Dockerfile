# Estágio de Construção (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar o arquivo de projeto e restaurar dependências
COPY ["RachaInteligente/RachaInteligente.csproj", "RachaInteligente/"]
RUN dotnet restore "RachaInteligente/RachaInteligente.csproj"

# Copiar o restante dos arquivos e compilar
COPY . .
WORKDIR "/src/RachaInteligente"
RUN dotnet build "RachaInteligente.csproj" -c Release -o /app/build

# Estágio de Publicação
FROM build AS publish
RUN dotnet publish "RachaInteligente.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Configurar a cultura para pt-BR para garantir formatação de moeda correta
ENV LANG=pt_BR.UTF-8
ENV LC_ALL=pt_BR.UTF-8
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RachaInteligente.dll"]
