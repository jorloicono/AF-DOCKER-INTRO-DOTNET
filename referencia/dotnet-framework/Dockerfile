# escape=`
# ==========================================================================
# PLANTILLA DE REFERENCIA — Aplicación ASP.NET (.NET Framework 4.8.x) en IIS
# Requiere CONTENEDORES WINDOWS: no funciona en contenedores Linux.
# Ajusta nombres de solución/proyecto y la versión de Windows (ltsc2019/ltsc2022/ltsc2025)
# a la del servidor donde vaya a ejecutarse.
# ==========================================================================

# ---------- Etapa 1: compilación con el SDK de .NET Framework ----------
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8.1-windowsservercore-ltsc2022 AS build
WORKDIR C:\src

# Restauramos primero los paquetes para aprovechar la caché de capas
COPY *.sln .
COPY MiAplicacionWeb\*.csproj MiAplicacionWeb\
COPY MiAplicacionWeb\packages.config MiAplicacionWeb\
RUN nuget restore

# Copiamos el código y publicamos a una carpeta
COPY MiAplicacionWeb\. MiAplicacionWeb\
WORKDIR C:\src\MiAplicacionWeb
RUN msbuild MiAplicacionWeb.csproj /p:Configuration=Release `
    /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem `
    /p:PublishUrl=C:\publish /p:DeleteExistingFiles=true

# ---------- Etapa 2: ejecución sobre IIS ----------
# La imagen aspnet ya trae IIS, ASP.NET 4.8 y ServiceMonitor (que mantiene vivo el contenedor)
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8.1-windowsservercore-ltsc2022 AS final
WORKDIR C:\inetpub\wwwroot
COPY --from=build C:\publish\. .

# La imagen base ya expone el puerto 80 y define el ENTRYPOINT con ServiceMonitor.exe w3svc
