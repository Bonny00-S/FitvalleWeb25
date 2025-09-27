# Etapa base: para ejecutar la app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Etapa build: para compilar la app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos de la solución
COPY . .

# Restaurar dependencias
RUN dotnet restore "Fitvalle_25.sln"

# Compilar en modo Release
RUN dotnet publish "Fitvalle_25.sln" -c Release -o /app/publish

# Etapa final: imagen optimizada
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ejecutar la app
ENTRYPOINT ["dotnet", "Fitvalle_25.dll"]
