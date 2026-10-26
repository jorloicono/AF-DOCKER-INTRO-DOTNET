# Lab 08 — Docker Compose: la aplicación completa

**Día 3 · Duración estimada: 75 minutos**

## Objetivo

Describir en un único fichero una aplicación de varios contenedores (una API .NET que guarda sus datos en SQL Server, con un nginx delante como proxy inverso) y levantarla, pararla y actualizarla con un solo comando. Al terminar tendrás un `compose.yaml` que cualquier compañero puede usar para tener el entorno completo en su equipo en un par de minutos.

En la carpeta de este lab tienes:

| Fichero | Contenido |
|---|---|
| `ConvocatoriasApi/` | La API, ahora guardando las convocatorias en SQL Server. Incluye su `Dockerfile` (multi-etapa, igual que en el lab 05) y `.dockerignore` |
| `.env` | La contraseña de SQL Server, que Compose sustituye en `${SA_PASSWORD}` |
| `nginx/default.conf` | Configuración de nginx como proxy inverso |
| `compose.yaml` | Esqueleto que vas a completar |

La API crea la base de datos y la tabla en el primer arranque, y añade endpoints para escribir:

| Método y ruta | Acción |
|---|---|
| `GET /convocatorias` | Lee las convocatorias de SQL Server |
| `POST /convocatorias` | Inserta una convocatoria |
| `GET /health` | Comprueba que la base de datos responde |

```powershell
cd labs\lab08-docker-compose
```

---

## Paso 1. Primero, a mano (para valorar lo que viene)

Así sería levantar esta aplicación con lo que sabemos hasta ahora. **No hace falta que lo ejecutes**, basta con leerlo:

```powershell
docker network create convocatorias-net
docker volume create convocatorias-sqldata
docker run -d --name db --network convocatorias-net -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="Curso.Docker2026!" -v convocatorias-sqldata:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
docker build -t convocatorias-api:1.0.0 ./ConvocatoriasApi
# ...esperar a que SQL Server esté listo...
docker run -d --name api --network convocatorias-net -p 8080:8080 -e ConnectionStrings__Convocatorias="Server=db;Database=Convocatorias;User Id=sa;Password=Curso.Docker2026!;TrustServerCertificate=True" convocatorias-api:1.0.0
```

Funciona, pero depende de que alguien recuerde todos los comandos, el orden y los parámetros. Compose convierte esto en un fichero declarativo, versionable en Git junto al código.

## Paso 2. El servicio de la API

Abre `compose.yaml`. Ya tiene definidos los dos servicios, `db` y `api`, con lo básico. Completa el servicio `api` con sus variables de entorno y su puerto:

```yaml
  api:
    build: ./ConvocatoriasApi
    image: convocatorias-api:1.0.0
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      Mensaje: "Hola desde Docker Compose"
      ConnectionStrings__Convocatorias: "Server=db;Database=Convocatorias;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
    ports:
      - "8080:8080"
```

Fíjate en tres cosas. `build` indica la carpeta con el `Dockerfile`, e `image` el nombre que tendrá la imagen construida. En la cadena de conexión, el servidor es `db`, **el nombre del servicio**: Compose crea una red y su DNS resuelve ese nombre. Y `${SA_PASSWORD}` se sustituye con el valor del fichero `.env`, que Compose lee automáticamente.

Prueba lo que llevas:

```powershell
docker compose up -d --build
docker compose ps
docker compose logs -f api
```

Verás en el log de la API algunos avisos de *SQL Server todavía no responde* y después *Base de datos 'Convocatorias' lista*. La API reintenta porque arranca antes de que SQL Server esté preparado (lo arreglaremos en el paso 4). Pulsa `Ctrl+C` para salir del log y prueba:

```powershell
Invoke-RestMethod http://localhost:8080/convocatorias
```

## Paso 3. Volumen y redes

Ahora mismo los datos se perderían al recrear el contenedor de base de datos, y todos los servicios comparten la red por defecto del proyecto. Añade al servicio `db`:

```yaml
    volumes:
      - sqldata:/var/opt/mssql
    networks:
      - backend
```

Al servicio `api`:

```yaml
    networks:
      - backend
      - frontend
```

Y al final del fichero, al mismo nivel que `services`, declara el volumen y las dos redes:

```yaml
volumes:
  sqldata:

networks:
  backend:
  frontend:
```

La idea es separar responsabilidades: la base de datos solo está en la red `backend`, la API está en las dos, y el proxy que añadiremos después solo estará en `frontend`. Así el proxy **no puede** hablar directamente con la base de datos.

Aplica los cambios. Compose compara el fichero con lo que hay en marcha y solo recrea lo necesario:

```powershell
docker compose up -d
docker compose ps
docker network ls
docker volume ls
```

Los nombres llevan el prefijo del proyecto (`convocatorias_`), definido con `name:` al principio del fichero.

## Paso 4. Orden de arranque con healthcheck

`depends_on` sin más solo garantiza que un contenedor **arranca** antes que otro, no que esté **listo**. SQL Server tarda unos segundos en aceptar conexiones. Lo correcto es definir cómo se comprueba que la base de datos está lista y hacer que la API espere a ese estado. Añade al servicio `db`:

```yaml
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q 'SELECT 1' -b -o /dev/null"]
      interval: 10s
      timeout: 5s
      retries: 12
      start_period: 20s
```

El `$$` evita que Compose intente sustituir la variable: se pasa tal cual al contenedor, que sí la tiene definida. Y en el servicio `api`:

```yaml
    depends_on:
      db:
        condition: service_healthy
```

Reinicia todo desde cero y observa el orden:

```powershell
docker compose down
docker compose up -d
docker compose ps
```

Durante unos segundos, `db` aparece como *health: starting* y `api` todavía no se ha creado. Cuando `db` pasa a *healthy*, arranca la API, que ya no necesita reintentar.

## Paso 5. Usar la aplicación

Inserta una convocatoria nueva:

```powershell
$nueva = @{ titulo = "Ayudas a la digitalización municipal"; organismo = "Consejería"; importe = 750000 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:8080/convocatorias -ContentType "application/json" -Body $nueva
Invoke-RestMethod http://localhost:8080/convocatorias
Invoke-RestMethod http://localhost:8080/health
```

Ahora comprueba la persistencia. `docker compose down` elimina contenedores y redes, pero **no** los volúmenes:

```powershell
docker compose down
docker compose up -d
```

Cuando la API vuelva a estar disponible, consulta `/convocatorias`: tu convocatoria sigue ahí.

Algunos comandos del día a día con Compose:

```powershell
docker compose logs db                    # logs de un servicio
docker compose exec db bash               # terminal dentro de un servicio
docker compose restart api                # reiniciar un servicio
docker compose stop                       # parar sin eliminar
docker compose config                     # ver el fichero final, con las variables ya sustituidas
```

## Paso 6. Ampliación: nginx como proxy inverso

En producción no se expone la API directamente: delante suele haber un proxy inverso que gestiona el tráfico, los certificados y, si hace falta, reparte la carga. Es el papel que hoy cumple IIS con ARR. Añade un tercer servicio:

```yaml
  proxy:
    image: nginx:alpine
    ports:
      - "8000:80"
    volumes:
      - ./nginx/default.conf:/etc/nginx/conf.d/default.conf:ro
    depends_on:
      - api
    networks:
      - frontend
```

Echa un vistazo a `nginx/default.conf`: reenvía todo a `http://api:8080`, otra vez usando el nombre del servicio.

```powershell
docker compose up -d
Invoke-RestMethod http://localhost:8000/convocatorias
```

La aplicación responde ahora a través del proxy, en el puerto 8000.

## Paso 7. Ampliación: escalar la API

Para tener varias instancias de la API, primero quita el bloque `ports` del servicio `api` (dos instancias no pueden publicar el mismo puerto de tu equipo; a partir de ahora solo se entra por el proxy). Después:

```powershell
docker compose up -d --scale api=3
docker compose ps
docker compose restart proxy
```

Reiniciamos el proxy para que vuelva a resolver el nombre `api`, que ahora corresponde a tres contenedores. Llama varias veces a la raíz y fíjate en el campo `maquina`:

```powershell
1..6 | ForEach-Object { (Invoke-RestMethod http://localhost:8000/).maquina }
```

Las peticiones se reparten entre las tres instancias. Con Compose esto se queda en un único servidor; repartir instancias entre varios servidores es el trabajo de un orquestador, del que hablaremos en la parte de infraestructura.

## Paso 8. Actualizar la API

Simula una nueva versión: en `ConvocatoriasApi/Program.cs`, cambia el texto `"API de Convocatorias (SQL Server)"`, y en `compose.yaml` cambia la etiqueta de la imagen a `1.1.0`. Después:

```powershell
docker compose up -d --build
docker compose ps
docker images convocatorias-api
```

Compose reconstruye la imagen y **solo recrea el servicio `api`**; la base de datos y el proxy no se tocan. Las dos versiones de la imagen quedan disponibles para volver atrás si hiciera falta.

## Paso 9. Limpieza

```powershell
docker compose down -v
```

La opción `-v` elimina también el volumen, es decir, **borra los datos**. Úsala solo cuando quieras empezar de cero.

---

## Solución

`soluciones/lab08/compose.yaml` contiene el fichero completo (con el puerto de la API publicado; para el paso 7 hay que quitarlo).

## Para comprobar lo aprendido

1. ¿Por qué la cadena de conexión usa `Server=db`?
2. ¿Qué diferencia hay entre `depends_on: [db]` y `depends_on` con `condition: service_healthy`?
3. ¿Qué hace `docker compose down` con los datos de SQL Server? ¿Y `docker compose down -v`?

<details>
<summary>Respuestas</summary>

1. Porque `db` es el nombre del servicio y el DNS de la red de Compose lo resuelve a la IP del contenedor de base de datos.
2. La primera solo espera a que el contenedor arranque; la segunda espera a que su healthcheck indique que está listo para aceptar conexiones.
3. `down` elimina contenedores y redes pero conserva el volumen, así que los datos se mantienen. `down -v` elimina también los volúmenes y, con ellos, los datos.

</details>
