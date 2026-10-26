# Preparación del entorno

Esta guía deja tu equipo listo para el curso. Lleva entre 30 y 40 minutos. Lo más importante es el último apartado, la verificación y descarga previa de imágenes, porque así llegamos el primer día con todo funcionando.

## Requisitos del equipo

- Windows 10 (22H2) o Windows 11, de 64 bits.
- Al menos 8 GB de RAM (16 GB recomendable: SQL Server en contenedor necesita 2 GB por sí solo).
- Unos 20 GB libres en disco.
- Virtualización activada en la BIOS/UEFI. Para comprobarlo: Administrador de tareas → pestaña Rendimiento → CPU. Debe aparecer *Virtualización: Habilitado*.
- Permisos de administrador para la instalación.

## 1. Activar WSL 2

Docker en Windows ejecuta los contenedores Linux sobre WSL 2 (el subsistema de Windows para Linux). Abre PowerShell **como administrador** y ejecuta:

```powershell
wsl --install
```

Reinicia el equipo cuando lo pida. Si WSL ya estaba instalado, asegúrate de que está actualizado con `wsl --update`.

## 2. Instalar Docker Desktop

Descárgalo desde https://www.docker.com/products/docker-desktop/ e instálalo con las opciones por defecto (backend WSL 2). Al terminar, abre Docker Desktop y espera a que indique que el motor está en marcha (*Engine running*).

**Sobre la licencia.** Docker Desktop es gratuito para uso personal, educativo y para organizaciones pequeñas, pero requiere suscripción de pago en organizaciones que superan cierto número de empleados o de facturación, y eso puede incluir a administraciones públicas. Conviene confirmarlo con sistemas. Si la política de la organización no lo permite, la alternativa gratuita es **Rancher Desktop** (https://rancherdesktop.io): durante la instalación, elige *dockerd (moby)* como motor de contenedores. Todos los comandos del curso funcionan igual.

## 3. Editor y SDK

- **Visual Studio Code** con la extensión **Container Tools** de Microsoft (sucesora de la antigua extensión *Docker*). Es lo que usaremos en clase. Si prefieres Visual Studio 2022 o posterior, también sirve.
- **SDK de .NET 10** (https://dotnet.microsoft.com/download). Es recomendable pero no imprescindible: las imágenes se compilan dentro de contenedores, así que Docker funciona aunque no tengas .NET instalado.

## 4. Verificación y descarga previa de imágenes

En una terminal (PowerShell o la terminal de VS Code), comprueba que Docker responde:

```powershell
docker version
docker run hello-world
```

El primer comando debe mostrar un bloque *Client* y otro *Server*. El segundo debe terminar con el mensaje *Hello from Docker!*.

Después, descarga las imágenes que usaremos, para no depender de la red durante el curso:

```powershell
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker pull mcr.microsoft.com/dotnet/aspnet:10.0
docker pull mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull nginx:alpine
docker pull alpine
docker pull registry:3
```

Las imágenes del SDK de .NET y de SQL Server son las más pesadas (entre las dos, en torno a 2-3 GB), así que la descarga puede tardar unos minutos.

Por último, comprueba que tienen salida a NuGet (la compilación de las aplicaciones .NET dentro de los contenedores descarga paquetes de `nuget.org`):

```powershell
docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet nuget search Microsoft.Data.SqlClient --take 1
```

Debe devolver al menos un resultado.

## Si algo falla

Si algún paso da error, en especial los `docker pull` o la búsqueda en NuGet (normalmente por proxy o por restricciones de salida a internet), envía al formador una captura del mensaje de error antes del curso. Para equipos sin acceso a los registros se pueden preparar las imágenes en fichero y cargarlas sin conexión con `docker load`.

Problemas frecuentes:

| Síntoma | Causa habitual | Solución |
|---|---|---|
| Docker Desktop no arranca: *WSL 2 installation is incomplete* | WSL no instalado o desactualizado | `wsl --install` o `wsl --update` como administrador y reiniciar |
| *Virtualization support not detected* | Virtualización desactivada en BIOS/UEFI | Activar Intel VT-x / AMD-V (suele requerir a soporte técnico) |
| `docker version` muestra error en el bloque *Server* | Docker Desktop no está arrancado | Abrir Docker Desktop y esperar a *Engine running* |
| `docker pull` con *timeout* o *x509 certificate* | Proxy corporativo | Configurar el proxy en Docker Desktop → *Settings → Resources → Proxies* |
| SQL Server se detiene nada más arrancar | Poca memoria para WSL | Cerrar aplicaciones o aumentar la memoria asignada |
