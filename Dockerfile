# ----- STAGE 1: BUILD -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY Fitvalle_25.csproj ./
RUN dotnet restore

# Copiar todo el código
COPY . .

# Publicar en modo Release
RUN dotnet publish -c Release -o /out

# ----- STAGE 2: RUNTIME -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar lo publicado desde la etapa anterior
COPY --from=build /out .

# Exponer puerto 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Ejecutar la app
ENTRYPOINT ["dotnet", "Fitvalle_25.dll"]