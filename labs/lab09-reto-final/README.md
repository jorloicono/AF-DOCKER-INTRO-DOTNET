# Lab 09 — Reto final: de mi equipo a un servidor sin internet

**Día 3 · Duración estimada: 45 minutos**

## Objetivo

Simular en tu equipo el camino que seguiría la aplicación del lab 08 hasta un servidor de vuestro CPD sin salida a internet, aplicando las buenas prácticas vistas en el curso: versionar la imagen, publicarla en un **registro privado**, desplegarla desde ahí con una configuración de producción y reducir su tamaño y superficie de ataque.

Este lab es más abierto que los anteriores. Cada parte tiene las pistas necesarias, y la solución completa está en `soluciones/lab09/`.

Necesitas haber completado el lab 08. Trabaja en su carpeta:

```powershell
cd labs\lab08-docker-compose
```

---

## Parte 1. Un registro privado local

En vuestro CPD, el registro privado sería un producto como Harbor, Nexus Repository o el registro integrado de GitLab. Para el curso usaremos la imagen oficial `registry`, que implementa el mismo protocolo:

```powershell
docker run -d -p 5000:5000 --restart unless-stopped --name registro -v registro-datos:/var/lib/registry registry:3
```

Ahora tienes un registro escuchando en `localhost:5000`.

## Parte 2. Versionar y publicar la imagen

Para publicar una imagen en un registro, su nombre debe empezar por la dirección del registro. Asegúrate de tener construida la imagen del lab 08 y etiquétala:

```powershell
docker compose build api
docker tag convocatorias-api:1.0.0 localhost:5000/convocatorias-api:1.0.0
docker tag convocatorias-api:1.0.0 localhost:5000/convocatorias-api:1.0
docker push localhost:5000/convocatorias-api:1.0.0
docker push localhost:5000/convocatorias-api:1.0
```

> Si en el lab 08 cambiaste la etiqueta a `1.1.0`, usa esa en el primer `docker tag`.

La segunda subida es casi instantánea: las capas ya están en el registro y solo se añade otra etiqueta. Consulta lo que hay publicado con la API del registro:

```powershell
Invoke-RestMethod http://localhost:5000/v2/_catalog
Invoke-RestMethod http://localhost:5000/v2/convocatorias-api/tags/list
```

Para comprobar que la imagen se puede recuperar desde el registro, bórrala de tu equipo y descárgala:

```powershell
docker rmi localhost:5000/convocatorias-api:1.0.0
docker pull localhost:5000/convocatorias-api:1.0.0
```

Así funcionaría en vuestros servidores: el pipeline de compilación publica la imagen en el registro interno y los servidores la descargan de ahí, sin tocar internet.

## Parte 3. Despliegue "de producción"

Copia `soluciones/lab09/compose.prod.yaml` a la carpeta del lab 08 (o escríbelo tú comparándolo con tu `compose.yaml`) y revisa las diferencias:

- La API usa `image: localhost:5000/convocatorias-api:1.0.0` en lugar de `build`: **en producción no se compila, se despliega una imagen ya probada**.
- Ni la API ni la base de datos publican puertos. Solo el proxy está expuesto.
- La red `backend` es `internal: true`: los contenedores conectados a ella no tienen salida al exterior.
- Hay límites de memoria y CPU para que un servicio no pueda acaparar el servidor.
- Los logs tienen rotación para no llenar el disco.
- El entorno de ASP.NET Core es `Production`.

Despliégalo (si el despliegue del lab 08 sigue en marcha, páralo antes con `docker compose down`, porque comparten el puerto 8000):

```powershell
docker compose -f compose.prod.yaml up -d
docker compose -f compose.prod.yaml ps
Invoke-RestMethod http://localhost:8000/convocatorias
```

Intenta ahora acceder directamente a la API en el puerto 8080: ya no responde, porque no está publicada.

```powershell
docker compose -f compose.prod.yaml down
```

## Parte 4. Una imagen más pequeña y más segura

Las imágenes **chiseled** de .NET son variantes mínimas: no tienen shell, ni gestor de paquetes, ni utilidades del sistema. Solo lo imprescindible para ejecutar la aplicación. Son más pequeñas y, sobre todo, ofrecen mucha menos superficie a un posible atacante.

En el `Dockerfile` de `ConvocatoriasApi`, cambia la imagen de la etapa final:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
```

Construye y compara:

```powershell
docker build -t convocatorias-api:1.0.0-chiseled ./ConvocatoriasApi
docker images convocatorias-api
```

Intenta abrir una terminal en un contenedor creado a partir de ella, y compáralo con la imagen normal:

```powershell
docker run --rm -it --entrypoint sh convocatorias-api:1.0.0
docker run --rm -it --entrypoint sh convocatorias-api:1.0.0-chiseled
```

Con la imagen normal entras en una terminal (sal con `exit`). Con la chiseled falla, porque no existe ningún `sh` dentro. Es el comportamiento esperado y el motivo por el que estas imágenes son más seguras: si alguien comprometiera la aplicación, no encontraría herramientas con las que moverse. A cambio, la depuración se hace desde fuera (logs, métricas, healthchecks) y no entrando en el contenedor.

Para usarla en el despliegue de producción, publícala en el registro con su propia etiqueta y cámbiala en `compose.prod.yaml`:

```powershell
docker tag convocatorias-api:1.0.0-chiseled localhost:5000/convocatorias-api:1.0.0-chiseled
docker push localhost:5000/convocatorias-api:1.0.0-chiseled
```

## Parte 5. El plan B sin registro: `save` y `load`

Si el registro privado todavía no existe, la imagen se puede llevar a un servidor en fichero, como vimos el primer día:

```powershell
docker save -o convocatorias-api-1.0.0.tar localhost:5000/convocatorias-api:1.0.0
```

Ese fichero, junto con `compose.prod.yaml`, `nginx/default.conf` y un `.env` con la contraseña real, es todo lo que necesita un servidor con Docker para ejecutar la aplicación: `docker load -i convocatorias-api-1.0.0.tar` y `docker compose -f compose.prod.yaml up -d`.

```powershell
Remove-Item convocatorias-api-1.0.0.tar
```

## Parte 6. Repaso de buenas prácticas

Revisa tu `compose.yaml` y tu `Dockerfile` con esta lista y anota qué cumplirías y qué no en un proyecto real:

| Práctica | ¿La aplicamos? |
|---|---|
| Construcción multi-etapa | Sí, desde el lab 05 |
| Imagen base mínima (aspnet o chiseled, no sdk) | Sí |
| Etiquetas de versión explícitas, nunca `latest` en producción | Parte 2 |
| Ejecución con usuario sin privilegios | `USER $APP_UID` |
| `.dockerignore` para no meter `bin`, `obj` ni secretos | Sí |
| Un proceso por contenedor | Sí: API, base de datos y proxy por separado |
| Logs a la salida estándar | Sí, comportamiento por defecto de .NET |
| Configuración por variables de entorno, misma imagen en todos los entornos | Lab 06 |
| Datos en volúmenes, contenedores desechables | Lab 07 |
| Healthchecks y orden de arranque | Lab 08 |
| Solo se publican los puertos imprescindibles | Parte 3 |
| Límites de recursos y rotación de logs | Parte 3 |
| Secretos fuera de la imagen y del repositorio | Parcial: `.env` fuera de Git, pero en producción deberían ir en un gestor de secretos |
| Escaneo de vulnerabilidades de las imágenes | Pendiente: `docker scout` o Trivy en el pipeline |

## Limpieza final del curso

Cuando hayas terminado:

```powershell
docker compose down -v
docker rm -f registro
docker volume rm registro-datos
```

Si quieres recuperar todo el espacio que ha usado el curso (borra **todas** las imágenes, contenedores parados, redes y volúmenes sin uso):

```powershell
docker system prune -a --volumes
```
