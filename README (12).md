# Lab 04 — Mi primer Dockerfile

**Día 2 · Duración estimada: 40 minutos**

## Objetivo

Escribir tu primer `Dockerfile`, construir una imagen propia, versionarla y comprobar cómo funciona la caché de capas. Usamos una web estática servida por nginx para centrarnos en el mecanismo antes de pasar a .NET en el lab siguiente.

Trabaja dentro de la carpeta de este lab:

```powershell
cd labs\lab04-primer-dockerfile
```

Contiene una carpeta `web` con un `index.html`.

---

## Paso 1. Escribir el Dockerfile

Crea un fichero llamado exactamente `Dockerfile` (sin extensión) en la carpeta del lab, al lado de la carpeta `web`, con este contenido:

```dockerfile
FROM nginx:alpine

LABEL org.opencontainers.image.title="mi-web" \
      org.opencontainers.image.description="Primer Dockerfile del curso"

COPY web/ /usr/share/nginx/html/

EXPOSE 80
```

Línea a línea: `FROM` indica la imagen base sobre la que construimos; `LABEL` añade metadatos descriptivos; `COPY` copia la carpeta `web` de tu equipo a la carpeta que sirve nginx dentro de la imagen; `EXPOSE` documenta en qué puerto escucha la aplicación. No necesitamos indicar cómo arrancar nginx porque la imagen base ya lo define.

> **Cuidado con el Bloc de notas**: puede guardar el fichero como `Dockerfile.txt`. Créalo desde Visual Studio Code para evitarlo.

## Paso 2. Construir la imagen

```powershell
docker build -t mi-web:v1 .
```

El punto final es importante: es el **contexto de construcción**, la carpeta cuyo contenido se envía al motor de Docker para que `COPY` pueda acceder a los ficheros. `-t` le da nombre y etiqueta a la imagen resultante.

Observa la salida: aparece un paso por cada instrucción.

```powershell
docker images mi-web
```

## Paso 3. Ejecutarla

```powershell
docker run -d -p 8080:80 --name miweb mi-web:v1
```

Abre http://localhost:8080. Esta vez la página es la tuya, empaquetada dentro de una imagen que puedes llevar a cualquier equipo con Docker.

## Paso 4. Nueva versión

Edita `web/index.html` y cambia `v1` por `v2` (y el texto que quieras). Construye una nueva versión con otra etiqueta:

```powershell
docker build -t mi-web:v2 .
docker images mi-web
```

Ahora tienes las dos versiones a la vez. Sustituye el contenedor por la versión nueva:

```powershell
docker rm -f miweb
docker run -d -p 8080:80 --name miweb mi-web:v2
```

Y si la v2 tuviera un problema, volver atrás es inmediato:

```powershell
docker rm -f miweb
docker run -d -p 8080:80 --name miweb mi-web:v1
```

Piensa en lo que supone hoy una marcha atrás en un despliegue sobre IIS. Aquí es cambiar una etiqueta, porque la versión anterior sigue existiendo, intacta, como imagen.

## Paso 5. La caché de capas

Vuelve a construir sin cambiar nada:

```powershell
docker build -t mi-web:v2 .
```

Termina casi al instante y los pasos aparecen marcados como `CACHED`: Docker detecta que ni la instrucción ni los ficheros implicados han cambiado y reutiliza las capas existentes.

Ahora añade una instrucción **antes** del `COPY`, por ejemplo:

```dockerfile
FROM nginx:alpine
RUN echo "Construida para el curso" > /etc/curso.txt
COPY web/ /usr/share/nginx/html/
EXPOSE 80
```

Construye dos veces seguidas y luego cambia solo el HTML y vuelve a construir. Verás que el `RUN` se reutiliza de la caché y solo se rehace el `COPY`. La regla es: **cuando una capa cambia, se reconstruyen esa y todas las que vienen detrás**. Por eso se ordenan las instrucciones de lo que menos cambia a lo que más cambia. En el lab 05 aplicaremos esta regla a la restauración de paquetes NuGet, donde se nota mucho más.

## Paso 6. El fichero `.dockerignore`

Todo lo que hay en la carpeta del contexto se envía al motor en cada construcción. En proyectos reales eso incluye carpetas como `bin`, `obj`, `.git` o `node_modules`, que no deben acabar en la imagen. Para excluirlas se usa `.dockerignore`, con la misma sintaxis que `.gitignore`. Crea uno en la carpeta del lab:

```
Dockerfile
.dockerignore
README.md
*.bak
```

Construye de nuevo. Con un proyecto tan pequeño no hay diferencia visible, pero en una solución .NET real reduce mucho el tiempo de construcción y evita meter en la imagen ficheros que no deberían estar.

## Paso 7. Inspeccionar tu imagen

```powershell
docker history mi-web:v2
docker inspect --format "{{json .Config.Labels}}" mi-web:v2
```

Verás tus capas encima de las de `nginx:alpine` y las etiquetas que añadiste con `LABEL`.

## Paso 8. Limpieza

```powershell
docker rm -f miweb
```

Conserva las imágenes `mi-web`, las reutilizaremos en el lab 07.

---

## Solución

La solución completa está en `soluciones/lab04/`.

## Para comprobar lo aprendido

1. ¿Qué significa el punto al final de `docker build -t mi-web:v1 .`?
2. Si cambias solo el HTML, ¿qué capas se reconstruyen?
3. ¿Por qué no escribimos `CMD` en este Dockerfile?

<details>
<summary>Respuestas</summary>

1. Es el contexto de construcción: la carpeta actual, cuyo contenido se envía al motor y desde la que trabaja `COPY`.
2. La del `COPY` y todas las posteriores. Las anteriores se reutilizan de la caché.
3. Porque la imagen base `nginx:alpine` ya define cómo arrancar nginx, y nuestra imagen lo hereda.

</details>
