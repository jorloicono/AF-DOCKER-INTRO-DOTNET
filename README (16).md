# Lab 07 — Volúmenes y redes

**Día 3 · Duración estimada: 60 minutos**

## Objetivo

Resolver los dos problemas que deja abiertos un contenedor aislado: **dónde guardar los datos** para que sobrevivan al contenedor, y **cómo se comunican varios contenedores** entre sí. Lo haremos con SQL Server en contenedor, algo que os resultará familiar.

> SQL Server necesita al menos 2 GB de memoria. Si el contenedor se detiene nada más arrancar, revisa la memoria asignada a Docker Desktop (*Settings → Resources*) o a WSL.

En PowerShell, guarda la contraseña en una variable para no repetirla:

```powershell
$SA = "Curso.Docker2026!"
```

---

## Parte A — Volúmenes

### Paso 1. Datos sin volumen: se pierden

Arranca un SQL Server sin ningún volumen:

```powershell
docker run -d --name sql1 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$SA -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Espera unos 20 segundos y comprueba en el log que ha arrancado (busca *SQL Server is now ready for client connections*):

```powershell
docker logs sql1 | Select-String "ready"
```

La imagen incluye la herramienta `sqlcmd`. Crea una base de datos con una tabla y un registro:

```powershell
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA -C -Q "CREATE DATABASE Curso; "
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA -C -d Curso -Q "CREATE TABLE Notas (Texto NVARCHAR(100)); INSERT INTO Notas VALUES (N'Este dato debería sobrevivir');"
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA -C -d Curso -Q "SELECT * FROM Notas"
```

> El parámetro `-C` indica a `sqlcmd` que confíe en el certificado autofirmado del servidor.

Si tienes SQL Server Management Studio o Azure Data Studio, también puedes conectarte a `localhost,1433` con el usuario `sa`.

Ahora elimina el contenedor y crea otro igual:

```powershell
docker rm -f sql1
docker run -d --name sql1 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$SA -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Espera a que arranque e intenta consultar la tabla:

```powershell
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA -C -d Curso -Q "SELECT * FROM Notas"
```

Error: la base de datos `Curso` no existe. Los datos vivían en la capa de escritura del contenedor y se fueron con él.

```powershell
docker rm -f sql1
```

### Paso 2. Con un volumen con nombre: persisten

Crea un volumen gestionado por Docker y móntalo en la carpeta donde SQL Server guarda sus datos (`/var/opt/mssql`):

```powershell
docker volume create sqldata
docker run -d --name sql1 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$SA -p 1433:1433 -v sqldata:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
```

Repite la creación de la base de datos, la tabla y el registro del paso 1. Después destruye el contenedor y crea uno nuevo **con el mismo volumen**:

```powershell
docker rm -f sql1
docker run -d --name sql1 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$SA -p 1433:1433 -v sqldata:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
```

Tras unos segundos:

```powershell
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA -C -d Curso -Q "SELECT * FROM Notas"
```

El dato sigue ahí. El contenedor es desechable, el volumen no. Esto permite, por ejemplo, actualizar SQL Server a una versión nueva cambiando solo la imagen y conservando los datos.

```powershell
docker volume ls
docker volume inspect sqldata
```

`inspect` muestra dónde guarda Docker el volumen (*Mountpoint*). No necesitas acceder a esa ruta: Docker lo gestiona por ti.

### Paso 3. Bind mounts: una carpeta de tu equipo dentro del contenedor

El otro tipo de montaje enlaza una carpeta concreta de tu equipo. Es muy útil en desarrollo. Desde la raíz del repositorio:

```powershell
docker run -d --name web-dev -p 8080:80 -v "${PWD}\labs\lab04-primer-dockerfile\web:/usr/share/nginx/html:ro" nginx:alpine
```

Abre http://localhost:8080, edita el `index.html` en Visual Studio Code, guarda y recarga el navegador. El cambio se ve al instante, sin reconstruir ninguna imagen, porque el contenedor está leyendo directamente tu carpeta.

| | Volumen con nombre | Bind mount |
|---|---|---|
| Lo gestiona | Docker | Tú (una ruta de tu equipo) |
| Uso típico | Datos de bases de datos, ficheros generados por la aplicación | Código y configuración en desarrollo, ficheros de configuración en servidor |
| Portabilidad | Alta, no depende de rutas del host | Depende de la estructura de carpetas del host |

```powershell
docker rm -f web-dev
```

---

## Parte B — Redes

### Paso 4. La red por defecto no resuelve nombres

Arranca un nginx y, desde otro contenedor, intenta llegar a él por su nombre:

```powershell
docker run -d --name web1 nginx:alpine
docker run --rm nginx:alpine wget -qO- -T 3 http://web1
```

Falla con *bad address 'web1'*: en la red `bridge` por defecto, los contenedores no se encuentran por nombre.

```powershell
docker network ls
```

### Paso 5. Una red definida por el usuario

```powershell
docker network create red-curso
docker network connect red-curso web1
docker run --rm --network red-curso nginx:alpine wget -qO- -T 3 http://web1
```

Ahora sí devuelve el HTML de nginx. **En una red creada por ti, Docker incluye un DNS interno** y cada contenedor es accesible por su nombre. Es lo que permite escribir `Server=sql1` en una cadena de conexión en lugar de una IP que cambia cada vez que se recrea el contenedor.

```powershell
docker network inspect red-curso
```

Verás los contenedores conectados y sus IP internas.

### Paso 6. Conectar con SQL Server por nombre

Conecta también el SQL Server a la red y consúltalo desde **otro** contenedor, usando su nombre como servidor:

```powershell
docker network connect red-curso sql1
docker run --rm --network red-curso mcr.microsoft.com/mssql/server:2022-latest /opt/mssql-tools18/bin/sqlcmd -S sql1 -U sa -P $SA -C -d Curso -Q "SELECT * FROM Notas"
```

Este es exactamente el patrón que usará la API en el lab 08: un contenedor de aplicación hablando con un contenedor de base de datos por su nombre, dentro de una red privada.

### Paso 7. Aislamiento: publicar solo lo necesario

Hasta ahora hemos publicado el puerto 1433 de SQL Server en tu equipo con `-p`. Dentro de una red de Docker no hace falta: los contenedores de la misma red se ven entre sí sin publicar nada. **Publicar un puerto es abrirlo al exterior**, y una base de datos normalmente solo debe ser accesible para su aplicación.

Recrea el SQL Server sin `-p`, conectado directamente a la red:

```powershell
docker rm -f sql1
docker run -d --name sql1 --network red-curso -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$SA -v sqldata:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
```

Tras unos segundos, repite la consulta del paso 6: funciona desde dentro de la red, pero desde tu equipo el puerto 1433 ya no está abierto.

## Paso 8. Limpieza

```powershell
docker rm -f sql1 web1
docker network rm red-curso
docker volume rm sqldata
```

---

## Reflexión antes del lab 08

Para levantar una aplicación con su base de datos "a mano" hemos necesitado crear una red, un volumen, recordar las variables de entorno, el orden de arranque y varios comandos largos. Si mañana lo tuviera que repetir un compañero, o hubiera que levantar diez servicios, esto no sería manejable. **Docker Compose** resuelve exactamente este problema: describe todo eso en un único fichero.

## Para comprobar lo aprendido

1. ¿Por qué se pierden los datos de SQL Server al eliminar el contenedor si no hay volumen?
2. ¿Qué necesitan dos contenedores para poder encontrarse por nombre?
3. ¿Hace falta publicar el puerto de la base de datos con `-p` para que la API la use?

<details>
<summary>Respuestas</summary>

1. Porque se guardaban en la capa de escritura del contenedor, que se elimina con él.
2. Estar conectados a la misma red definida por el usuario, que incluye DNS interno.
3. No. Basta con que estén en la misma red. Publicar el puerto solo sirve para acceder desde fuera de Docker, y en producción conviene evitarlo.

</details>
