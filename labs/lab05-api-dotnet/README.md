# Lab 05 — Contenerizar una API .NET

**Día 2 · Duración estimada: 60 minutos**

## Objetivo

Empaquetar una API ASP.NET Core en .NET 10 en una imagen de Docker, primero de forma ingenua y después con una **construcción multi-etapa**, que es como se hace en la práctica. Compararemos tamaños, veremos el efecto de la caché en la restauración de paquetes y comprobaremos que la API se ejecuta con un usuario sin privilegios.

La aplicación está en `labs/lab05-api-dotnet/ConvocatoriasApi`. Es una *minimal API* con un catálogo de convocatorias de financiación en memoria y estos endpoints:

| Método y ruta | Devuelve |
|---|---|
| `GET /` | Información del contenedor: mensaje configurado, entorno, máquina, usuario y sistema operativo |
| `GET /convocatorias` | Lista de convocatorias |
| `GET /convocatorias/{id}` | Una convocatoria o 404 |
| `GET /health` | Estado de salud (`Healthy`) |

Echa un vistazo a `Program.cs` antes de empezar: no tiene nada específico de Docker. **El código no cambia para contenerizarlo.**

```powershell
cd labs\lab05-api-dotnet\ConvocatoriasApi
```

---

## Paso 1 (opcional). Ejecutarla sin Docker

Si tienes el SDK de .NET 10 instalado, comprueba que la aplicación funciona de la forma habitual:

```powershell
dotnet run --urls http://localhost:5000
```

Abre http://localhost:5000 y fíjate en los campos `sistemaOperativo` y `usuario`: son los de tu Windows. Para la aplicación con `Ctrl+C` y **borra las carpetas `bin` y `obj`** que se han generado:

```powershell
Remove-Item -Recurse -Force bin, obj
```

Si no tienes el SDK, no pasa nada: ese es precisamente uno de los argumentos a favor de Docker. Para construir la imagen no necesitas .NET instalado en tu equipo, porque la compilación ocurre dentro de un contenedor con el SDK.

## Paso 2. Versión ingenua: una sola etapa

Crea un fichero `Dockerfile` dentro de la carpeta `ConvocatoriasApi`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app
WORKDIR /app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConvocatoriasApi.dll"]
```

`WORKDIR` fija (y crea si no existe) la carpeta de trabajo dentro de la imagen. `RUN` ejecuta un comando durante la construcción: aquí, la compilación y publicación. `ENTRYPOINT` define el proceso que se ejecutará al arrancar el contenedor.

```powershell
docker build -t convocatorias-api:una-etapa .
docker run -d -p 8080:8080 --name api convocatorias-api:una-etapa
```

Abre http://localhost:8080 y http://localhost:8080/convocatorias. Funciona, y fíjate en que ahora el sistema operativo es Linux y la máquina es el identificador del contenedor.

> La compilación necesita descargar paquetes de NuGet. Si en tu red no hay acceso a `nuget.org`, coméntalo con el formador: la solución en entornos aislados es un *feed* NuGet interno, y lo veremos en la parte de infraestructura.

Ahora mira el tamaño:

```powershell
docker images convocatorias-api
```

Ocupa bastante, porque la imagen lleva el SDK completo: compilador, herramientas, caché de NuGet y el código fuente. Nada de eso hace falta para **ejecutar** la API, y todo ello es peso extra y superficie de ataque en producción.

```powershell
docker rm -f api
```

## Paso 3. Versión buena: multi-etapa

Sustituye el contenido del `Dockerfile` por este:

```dockerfile
# syntax=docker/dockerfile:1

# Etapa 1 — build: compilamos con la imagen del SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ConvocatoriasApi.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2 — runtime: solo lo necesario para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ConvocatoriasApi.dll"]
```

Hay dos `FROM`, es decir, dos etapas. La primera, llamada `build`, compila con el SDK. La segunda parte de la imagen `aspnet`, que solo contiene el runtime, y copia **únicamente el resultado publicado** desde la primera con `COPY --from=build`. Todo lo de la etapa de compilación se descarta y no llega a la imagen final.

Dos detalles más:

- **Copiamos primero el `.csproj` y restauramos**, y solo después copiamos el resto del código. Así la capa de `dotnet restore` queda en caché mientras no cambien las dependencias, y cada cambio en el código no obliga a descargar de nuevo los paquetes NuGet.
- **`USER $APP_UID`**: las imágenes oficiales de .NET incluyen un usuario sin privilegios llamado `app`, y con esta línea la aplicación se ejecuta con él en lugar de como `root`.

Crea también un fichero `.dockerignore` en la misma carpeta para no enviar `bin` y `obj` al contexto:

```
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/*.user
**/Dockerfile*
**/.dockerignore
**/.env
```

Construye y compara:

```powershell
docker build -t convocatorias-api:1.0 .
docker images convocatorias-api
```

La diferencia de tamaño entre `una-etapa` y `1.0` es la diferencia entre llevar el taller entero a producción o solo el coche.

## Paso 4. Probar la imagen

```powershell
docker run -d -p 8080:8080 --name api convocatorias-api:1.0
```

Prueba los endpoints en el navegador o desde PowerShell:

```powershell
Invoke-RestMethod http://localhost:8080/
Invoke-RestMethod http://localhost:8080/convocatorias
Invoke-RestMethod http://localhost:8080/convocatorias/2
Invoke-RestMethod http://localhost:8080/health
```

En la respuesta de `/` el campo `usuario` ahora vale `app`: la API no se ejecuta como administrador.

¿Por qué el puerto 8080 y no el 80? Desde .NET 8, las imágenes de ASP.NET Core escuchan por defecto en el 8080, precisamente porque un usuario sin privilegios no puede abrir puertos por debajo del 1024 en Linux. Con `-p` puedes publicarlo en el puerto que quieras de tu equipo: `-p 80:8080` también es válido.

## Paso 5. La caché en acción

Abre `Program.cs` y cambia, por ejemplo, el texto `"API de Convocatorias"` por otro. Reconstruye:

```powershell
docker build -t convocatorias-api:1.1 .
```

Observa que el paso `RUN dotnet restore` aparece como `CACHED` y solo se repite la publicación. Ahora haz la prueba contraria: añade una línea en blanco al `.csproj` y reconstruye. Esta vez se repite también la restauración. En un proyecto con muchas dependencias, esta diferencia son minutos en cada construcción.

## Paso 6. Varias instancias de la misma imagen

```powershell
docker run -d -p 8081:8080 --name api2 convocatorias-api:1.0
docker run -d -p 8082:8080 --name api3 convocatorias-api:1.0
```

Consulta `/` en los puertos 8080, 8081 y 8082. Las tres responden con un `maquina` distinto: son tres procesos aislados creados a partir de la misma imagen, en un segundo y sin instalar nada. Esta es la base de cómo se escala horizontalmente una aplicación con contenedores.

## Paso 7. Limpieza

```powershell
docker rm -f api api2 api3
docker rmi convocatorias-api:una-etapa convocatorias-api:1.1
```

Conserva `convocatorias-api:1.0`, la usaremos en el siguiente lab.

---

## Solución

En `soluciones/lab05/` tienes `Dockerfile.una-etapa`, `Dockerfile` (multi-etapa) y `.dockerignore`.

## Para comprobar lo aprendido

1. ¿Qué etapa del Dockerfile multi-etapa acaba en la imagen final?
2. ¿Por qué se copia el `.csproj` antes que el resto del código?
3. ¿En qué puerto escucha por defecto una aplicación ASP.NET Core en las imágenes de .NET 8 o posteriores, y por qué?

<details>
<summary>Respuestas</summary>

1. Solo la última (`final`, basada en `aspnet`). De la etapa `build` solo llega lo que se copia explícitamente con `COPY --from=build`.
2. Para que la capa de `dotnet restore` se reutilice de la caché mientras no cambien las dependencias.
3. En el 8080, porque la aplicación se ejecuta con un usuario sin privilegios, que en Linux no puede abrir puertos inferiores al 1024.

</details>
