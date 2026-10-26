# Lab 06 — Configuración: una imagen, muchos entornos

**Día 2 · Duración estimada: 45 minutos**

## Objetivo

Configurar la API del lab 05 desde fuera del contenedor, sin reconstruir la imagen. Es el principio de **construir una vez y desplegar en cualquier entorno**: la misma imagen que se prueba en desarrollo es la que llega a preproducción y producción, y solo cambia la configuración que recibe.

Para quien venga de .NET Framework, esto sustituye a las transformaciones de `web.config` por entorno: en lugar de generar un paquete distinto para cada entorno, hay un único artefacto y la configuración se inyecta al arrancar.

Necesitas la imagen `convocatorias-api:1.0` del lab anterior. Comprueba que existe:

```powershell
docker images convocatorias-api
```

Trabaja desde la carpeta de este lab:

```powershell
cd labs\lab06-configuracion
```

---

## Paso 1. Cómo lee .NET la configuración

ASP.NET Core combina varias fuentes de configuración, y las posteriores sobrescriben a las anteriores:

1. `appsettings.json`
2. `appsettings.{Entorno}.json` (por ejemplo, `appsettings.Production.json`)
3. **Variables de entorno**
4. Argumentos de línea de comandos

Como las variables de entorno van después de los ficheros, ganan. Las secciones anidadas del JSON se expresan con **doble guion bajo**: la clave `ConnectionStrings:Default` se convierte en la variable `ConnectionStrings__Default`.

La API tiene en `appsettings.json` la clave `"Mensaje": "Hola desde appsettings.json"`, y el endpoint `/` la muestra.

## Paso 2. Sobrescribir con variables de entorno

```powershell
docker run -d -p 8080:8080 --name api-env -e Mensaje="Hola desde una variable de entorno" convocatorias-api:1.0
Invoke-RestMethod http://localhost:8080/
```

El campo `mensaje` ha cambiado sin tocar ni el código ni la imagen. Ahora cambia también el entorno de ASP.NET Core:

```powershell
docker rm -f api-env
docker run -d -p 8080:8080 --name api-env -e ASPNETCORE_ENVIRONMENT=Development -e Mensaje="Entorno de desarrollo" convocatorias-api:1.0
Invoke-RestMethod http://localhost:8080/
```

El campo `entorno` vale ahora `Development`. Por defecto, dentro del contenedor es `Production`.

## Paso 3. Ficheros de variables

Cuando hay muchas variables, escribirlas en el comando es incómodo. En la carpeta del lab hay un fichero `config.env`:

```
ASPNETCORE_ENVIRONMENT=Staging
Mensaje=Hola desde config.env (entorno de preproducción)
```

```powershell
docker rm -f api-env
docker run -d -p 8080:8080 --name api-env --env-file config.env convocatorias-api:1.0
Invoke-RestMethod http://localhost:8080/
docker exec api-env printenv
```

`printenv` lista las variables que ha recibido el contenedor. Un aviso importante: **las variables de entorno se pueden leer con `docker inspect`**, así que no son un sitio seguro para contraseñas en producción. Lo trataremos en buenas prácticas.

## Paso 4. Un fichero de configuración montado desde fuera

A veces se prefiere un fichero de configuración completo por entorno. Crea en la carpeta del lab un fichero `appsettings.Production.json`:

```json
{
  "Mensaje": "Hola desde appsettings.Production.json montado como volumen"
}
```

Y móntalo dentro del contenedor, en la carpeta de la aplicación:

```powershell
docker rm -f api-env
docker run -d -p 8080:8080 --name api-env -v "${PWD}\appsettings.Production.json:/app/appsettings.Production.json:ro" convocatorias-api:1.0
Invoke-RestMethod http://localhost:8080/
```

La opción `-v origen:destino:ro` monta un fichero de tu equipo dentro del contenedor, en modo solo lectura. Como el entorno por defecto es `Production`, .NET carga ese fichero y su valor sobrescribe al de `appsettings.json`. Profundizaremos en los montajes mañana, con los volúmenes.

> En CMD, sustituye `${PWD}` por `%cd%`. En PowerShell, las comillas son necesarias si la ruta tiene espacios.

## Paso 5. El mismo artefacto en tres entornos

Levanta la misma imagen tres veces, como si fueran tres entornos:

```powershell
docker rm -f api-env
docker run -d -p 8081:8080 --name api-des -e ASPNETCORE_ENVIRONMENT=Development -e Mensaje="DESARROLLO" convocatorias-api:1.0
docker run -d -p 8082:8080 --name api-pre -e ASPNETCORE_ENVIRONMENT=Staging -e Mensaje="PREPRODUCCIÓN" convocatorias-api:1.0
docker run -d -p 8083:8080 --name api-pro -e Mensaje="PRODUCCIÓN" convocatorias-api:1.0
```

Consulta `/` en los puertos 8081, 8082 y 8083. Es exactamente el mismo binario en los tres casos: lo que se probó en preproducción es, bit a bit, lo que se ejecuta en producción.

## Paso 6. Cambiar el puerto de escucha

El puerto interno también es configuración. La variable `ASPNETCORE_HTTP_PORTS` indica en qué puerto escucha Kestrel:

```powershell
docker run -d -p 9000:5000 --name api-puerto -e ASPNETCORE_HTTP_PORTS=5000 convocatorias-api:1.0
Invoke-RestMethod http://localhost:9000/
```

La API escucha ahora en el 5000 dentro del contenedor, y lo publicamos en el 9000 de tu equipo.

## Paso 7. Logs y depuración

Una aplicación en contenedor escribe sus logs en la consola. Mira lo que ha registrado una de las APIs:

```powershell
docker logs api-des
```

Si un contenedor arranca y se detiene enseguida, `docker logs` es lo primero que hay que mirar. Pruébalo provocando un error: pide a la imagen que ejecute un ensamblado que no existe.

```powershell
docker run --name api-rota --entrypoint dotnet convocatorias-api:1.0 NoExiste.dll
docker ps -a --filter name=api-rota
docker logs api-rota
```

El contenedor aparece como *Exited* con un código distinto de 0, y el log explica por qué.

## Paso 8. Limpieza

```powershell
docker rm -f api-des api-pre api-pro api-puerto api-rota
```

---

## Solución

En `soluciones/lab06/` tienes `config.env` y `appsettings.Production.json`.

## Para comprobar lo aprendido

1. ¿Qué nombre tendría la variable de entorno para sobrescribir la clave `Logging:LogLevel:Default`?
2. Si `appsettings.json` y una variable de entorno definen la misma clave, ¿cuál gana?
3. ¿Qué ventaja tiene desplegar la misma imagen en todos los entornos frente a generar un paquete por entorno?

<details>
<summary>Respuestas</summary>

1. `Logging__LogLevel__Default`.
2. La variable de entorno, porque se aplica después de los ficheros JSON.
3. Lo que se valida en preproducción es exactamente lo que llega a producción. Se eliminan los errores de "en este entorno se compiló distinto" y las marchas atrás son triviales.

</details>
