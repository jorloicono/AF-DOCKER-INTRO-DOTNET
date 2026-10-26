# Lab 01 — Primeros pasos con Docker

**Día 1 · Duración estimada: 30 minutos**

## Objetivo

Comprobar que Docker funciona en tu equipo, entender qué ocurre cuando ejecutas tu primer contenedor y levantar un servidor web en segundos sin instalar nada en Windows. Al terminar sabrás arrancar, listar, parar y eliminar contenedores.

Todos los comandos están pensados para **PowerShell** o la terminal integrada de Visual Studio Code. Funcionan igual en CMD, salvo que se indique lo contrario.

---

## Paso 1. Comprobar la instalación

Empieza preguntándole a Docker qué versión tienes. Fíjate en que la salida tiene dos bloques, *Client* y *Server*: el cliente es el comando `docker` que escribes, y el servidor es el motor (el *daemon*) que realmente crea y ejecuta los contenedores. Si el bloque *Server* da error, Docker Desktop no está arrancado.

```powershell
docker version
```

A continuación pide información general del motor. Busca en la salida cuántos contenedores e imágenes tienes, qué sistema operativo usa el motor (*OSType: linux*) y cuánta memoria tiene asignada.

```powershell
docker info
```

## Paso 2. El primer contenedor

```powershell
docker run hello-world
```

Lee con calma el mensaje que aparece, porque describe exactamente los cuatro pasos que acaban de ocurrir: el cliente contactó con el motor, el motor buscó la imagen `hello-world` (en local o en Docker Hub), creó un contenedor a partir de ella y ese contenedor escribió el texto que estás leyendo antes de terminar.

Ejecuta el mismo comando una segunda vez. Ya no aparece "Unable to find image locally" porque la imagen está en tu equipo: **las imágenes se descargan una vez y se reutilizan**.

## Paso 3. ¿Dónde está el contenedor?

```powershell
docker ps
docker ps -a
```

`docker ps` muestra solo los contenedores en ejecución, y probablemente esté vacío. Con `-a` aparecen también los que ya han terminado: deberías ver dos contenedores de `hello-world` en estado *Exited*. El contenedor terminó porque su proceso principal terminó. **Un contenedor vive mientras vive su proceso principal**, y esta idea es clave para todo el curso.

Fíjate también en la columna NAMES: Docker les ha puesto nombres aleatorios porque no les dimos uno.

## Paso 4. Un servidor web en un segundo

Ahora vamos a arrancar algo que se quede funcionando: un servidor web nginx.

```powershell
docker run -d -p 8080:80 --name miweb nginx:alpine
```

Cada parte del comando significa algo:

| Parte | Significado |
|---|---|
| `-d` | *detached*: el contenedor se ejecuta en segundo plano y te devuelve la terminal |
| `-p 8080:80` | publica el puerto 80 **del contenedor** en el puerto 8080 **de tu equipo** (`equipo:contenedor`) |
| `--name miweb` | nombre que tú eliges para referirte al contenedor |
| `nginx:alpine` | imagen (`nginx`) y etiqueta (`alpine`, una variante pequeña basada en Alpine Linux) |

Abre el navegador en **http://localhost:8080** y verás la página de bienvenida de nginx. No has instalado nginx en Windows ni has configurado IIS: el servidor web vive dentro del contenedor.

```powershell
docker ps
```

Ahora sí aparece en ejecución, con la columna PORTS mostrando `0.0.0.0:8080->80/tcp`.

## Paso 5. Ver lo que pasa dentro

Recarga la página varias veces en el navegador y después mira los registros del contenedor:

```powershell
docker logs miweb
```

Cada petición del navegador aparece como una línea. En contenedores, las aplicaciones escriben sus logs en la salida estándar y Docker los recoge; no hace falta buscar ficheros de log en el servidor.

## Paso 6. Parar, arrancar y eliminar

```powershell
docker stop miweb
docker ps -a
docker start miweb
docker stop miweb
docker rm miweb
docker ps -a
```

`stop` detiene el contenedor pero lo conserva (puedes volver a arrancarlo con `start`); `rm` lo elimina definitivamente. Un contenedor en marcha no se puede eliminar sin pararlo antes, salvo que uses `docker rm -f`.

## Paso 7. Docker Desktop

Abre Docker Desktop y recorre las secciones **Containers** e **Images**. Todo lo que has hecho por comandos se puede ver y gestionar desde aquí, incluidos los logs y una terminal dentro del contenedor. En el curso usaremos sobre todo la línea de comandos, porque es lo que luego se automatiza en servidores y pipelines, pero la interfaz es útil para echar un vistazo rápido.

## Paso 8. Limpieza

Elimina los contenedores de `hello-world` que quedaron parados:

```powershell
docker container prune
```

Confirma con `y`. Este comando borra todos los contenedores detenidos.

---

## Para comprobar lo aprendido

1. ¿Qué diferencia hay entre `docker ps` y `docker ps -a`?
2. Si ejecutas `docker run -d -p 9090:80 nginx:alpine`, ¿en qué URL verás la web?
3. ¿Por qué el contenedor de `hello-world` aparece como *Exited* y el de nginx no?

<details>
<summary>Respuestas</summary>

1. `docker ps` lista solo los contenedores en ejecución; con `-a` lista también los detenidos.
2. En http://localhost:9090, porque el primer número es el puerto del equipo.
3. Porque el proceso principal de `hello-world` escribe un mensaje y termina, mientras que nginx es un servidor que se queda escuchando peticiones indefinidamente.

</details>
