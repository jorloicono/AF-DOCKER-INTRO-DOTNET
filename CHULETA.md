# Chuleta de comandos Docker

## Contenedores

| Comando | Para qué |
|---|---|
| `docker run -d -p 8080:80 --name web nginx:alpine` | Crear y arrancar un contenedor en segundo plano publicando un puerto (`equipo:contenedor`) |
| `docker run --rm -it alpine sh` | Contenedor interactivo que se elimina al salir |
| `docker run -e CLAVE=valor ...` / `--env-file fichero.env` | Pasar variables de entorno |
| `docker run -v volumen:/ruta ...` | Montar un volumen con nombre |
| `docker run -v "${PWD}\carpeta:/ruta:ro" ...` | Montar una carpeta del equipo (bind mount), solo lectura |
| `docker run --network red ...` | Conectar a una red concreta |
| `docker run --restart unless-stopped ...` | Reinicio automático salvo parada manual |
| `docker ps` / `docker ps -a` | Contenedores en ejecución / todos |
| `docker logs -f web` | Seguir los logs |
| `docker exec -it web sh` | Abrir una terminal dentro |
| `docker inspect web` | Configuración completa en JSON |
| `docker stats` / `docker top web` | Consumo de recursos / procesos |
| `docker cp fichero web:/ruta` | Copiar ficheros entre equipo y contenedor |
| `docker stop web` / `docker start web` | Parar / arrancar |
| `docker rm web` / `docker rm -f web` | Eliminar / forzar eliminación |
| `docker container prune` | Eliminar todos los contenedores parados |

## Imágenes

| Comando | Para qué |
|---|---|
| `docker images` | Listar imágenes |
| `docker pull imagen:etiqueta` | Descargar |
| `docker build -t nombre:1.0 .` | Construir desde el Dockerfile de la carpeta actual |
| `docker build -f Dockerfile.otro -t nombre:1.0 .` | Construir con otro Dockerfile |
| `docker tag origen:1.0 registro:5000/destino:1.0` | Añadir una etiqueta |
| `docker push registro:5000/destino:1.0` | Publicar en un registro |
| `docker history imagen` | Ver las capas |
| `docker save -o fichero.tar imagen:1.0` | Exportar a fichero (entornos sin internet) |
| `docker load -i fichero.tar` | Importar desde fichero |
| `docker rmi imagen:1.0` | Eliminar una imagen o etiqueta |
| `docker image prune` | Eliminar imágenes sin etiqueta |

## Volúmenes y redes

| Comando | Para qué |
|---|---|
| `docker volume create datos` / `ls` / `inspect` / `rm` | Gestionar volúmenes |
| `docker network create red` / `ls` / `inspect` / `rm` | Gestionar redes |
| `docker network connect red contenedor` | Conectar un contenedor existente a una red |

## Docker Compose

| Comando | Para qué |
|---|---|
| `docker compose up -d` | Crear y arrancar todos los servicios |
| `docker compose up -d --build` | Reconstruir imágenes y arrancar |
| `docker compose up -d --scale api=3` | Varias instancias de un servicio |
| `docker compose ps` | Estado de los servicios |
| `docker compose logs -f api` | Logs de un servicio |
| `docker compose exec db bash` | Terminal en un servicio |
| `docker compose restart api` | Reiniciar un servicio |
| `docker compose stop` | Parar sin eliminar |
| `docker compose down` | Eliminar contenedores y redes (conserva volúmenes) |
| `docker compose down -v` | Eliminar también los volúmenes (**borra datos**) |
| `docker compose config` | Ver el fichero final con variables sustituidas |
| `docker compose -f compose.prod.yaml up -d` | Usar otro fichero |

## Sistema

| Comando | Para qué |
|---|---|
| `docker version` / `docker info` | Versión y estado del motor |
| `docker system df` | Espacio ocupado |
| `docker system prune` | Limpiar todo lo que no se usa (con `-a --volumes`, también imágenes y volúmenes) |

## Dockerfile

| Instrucción | Para qué |
|---|---|
| `FROM imagen AS etapa` | Imagen base (y nombre de la etapa en multi-etapa) |
| `WORKDIR /app` | Carpeta de trabajo |
| `COPY origen destino` / `COPY --from=etapa ...` | Copiar ficheros del contexto o de otra etapa |
| `RUN comando` | Ejecutar durante la construcción |
| `ENV CLAVE=valor` | Variable de entorno por defecto |
| `EXPOSE 8080` | Documentar el puerto |
| `USER $APP_UID` | Usuario con el que se ejecuta (en imágenes .NET, el usuario `app`) |
| `ENTRYPOINT ["dotnet", "App.dll"]` | Proceso principal |
| `CMD [...]` | Argumentos por defecto (sustituibles al ejecutar) |
| `LABEL clave="valor"` | Metadatos |
| `HEALTHCHECK CMD ...` | Comprobación de salud |

## Configuración de .NET en contenedores

| Variable | Efecto |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Entorno (`Development`, `Staging`, `Production`). Por defecto `Production` |
| `ASPNETCORE_HTTP_PORTS` | Puerto de escucha. Por defecto `8080` en las imágenes de .NET 8+ |
| `ConnectionStrings__Nombre` | Equivale a `ConnectionStrings:Nombre` en `appsettings.json` |
| `Seccion__Clave` | El doble guion bajo sustituye a los dos puntos en cualquier clave anidada |
