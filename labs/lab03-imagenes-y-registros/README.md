# Lab 03 — Imágenes, capas y registros

**Día 1 · Duración estimada: 45 minutos**

## Objetivo

Entender qué es realmente una imagen: cómo se nombra, cómo está hecha de capas, de dónde se descarga y cómo se mueve de un equipo a otro **sin conexión a internet**. Este último punto es especialmente relevante para vuestros entornos, porque es exactamente lo que necesitaréis en un CPD sin salida al exterior.

---

## Paso 1. Las imágenes que tienes

```powershell
docker images
```

Cada fila es una imagen con su repositorio (`REPOSITORY`), etiqueta (`TAG`), identificador y tamaño. Fíjate en la diferencia de tamaño entre `hello-world`, `nginx:alpine` y las imágenes de .NET y SQL Server que descargaste antes del curso.

## Paso 2. Anatomía de un nombre de imagen

El nombre completo de una imagen tiene esta forma:

```
registro/repositorio:etiqueta
mcr.microsoft.com/dotnet/aspnet:10.0
```

- **Registro**: el servidor de donde se descarga. Si no se indica, es Docker Hub (`docker.io`). Microsoft publica sus imágenes en su propio registro, `mcr.microsoft.com`.
- **Repositorio**: el nombre de la imagen, que puede incluir una ruta (`dotnet/aspnet`).
- **Etiqueta**: la versión o variante. Si no se indica, se usa `latest`.

Descarga dos variantes de la misma imagen y compara:

```powershell
docker pull nginx:alpine
docker pull nginx:latest
docker images nginx
```

La variante `alpine` ocupa una fracción de la otra. Misma aplicación, distinta base: elegir bien la imagen base es una de las primeras buenas prácticas.

> **Si `docker pull` falla en tu equipo** por proxy o restricciones de red, sáltate la descarga de `nginx:latest` y continúa: el resto del lab funciona con las imágenes que ya tienes.

## Paso 3. Las capas

Una imagen no es un único fichero, sino una pila de capas de solo lectura. Cada capa corresponde a una instrucción con la que se construyó la imagen:

```powershell
docker history nginx:alpine
docker history mcr.microsoft.com/dotnet/aspnet:10.0
```

Fíjate en que la imagen `aspnet` se construye encima de otras capas (sistema operativo base, dependencias nativas, runtime de .NET, ASP.NET Core). Si dos imágenes comparten capas, **las capas comunes se almacenan una sola vez**. Por eso la imagen del SDK de .NET y la de `aspnet` no ocupan en disco la suma de sus tamaños.

Para ver los identificadores de las capas:

```powershell
docker inspect --format "{{json .RootFS.Layers}}" nginx:alpine
```

Cuando un contenedor arranca, Docker añade encima una capa fina de **lectura y escritura**. Es la capa donde hiciste cambios en el lab 02, y la que desaparece al borrar el contenedor.

## Paso 4. Etiquetar imágenes

Una etiqueta es solo un nombre que apunta a una imagen. Una misma imagen puede tener varias:

```powershell
docker tag nginx:alpine miservidor-web:1.0
docker tag nginx:alpine miservidor-web:latest
docker images
```

Las tres filas (`nginx:alpine`, `miservidor-web:1.0`, `miservidor-web:latest`) tienen el **mismo IMAGE ID**: no se ha copiado nada, son tres nombres para la misma imagen. Eliminar una etiqueta no borra la imagen mientras queden otras:

```powershell
docker rmi miservidor-web:latest
docker images
```

## Paso 5. La tentación de `docker commit`

Existe una forma de crear una imagen a partir de un contenedor modificado a mano:

```powershell
docker run -d --name temporal nginx:alpine
docker exec temporal sh -c "echo '<h1>Imagen creada con commit</h1>' > /usr/share/nginx/html/index.html"
docker commit temporal miweb-commit:1.0
docker rm -f temporal
docker run -d -p 8080:80 --name prueba miweb-commit:1.0
```

Abre http://localhost:8080: funciona. Pero piensa en el problema: dentro de seis meses, ¿alguien sabrá qué se hizo exactamente para crear esa imagen? No queda registro, no se puede revisar en un control de versiones y no se puede reproducir. Es el equivalente a un servidor configurado a mano. Por eso las imágenes se construyen **siempre con un Dockerfile**, que es lo que veremos mañana. `docker commit` solo tiene sentido para depurar.

```powershell
docker rm -f prueba
```

## Paso 6. Mover imágenes sin internet: `save` y `load`

Este paso simula el escenario de un servidor sin acceso a registros externos. Guarda una imagen en un fichero:

```powershell
docker save -o nginx-alpine.tar nginx:alpine
Get-Item nginx-alpine.tar | Select-Object Name, Length
```

Ese `.tar` contiene todas las capas y metadatos. Puede copiarse a un USB, a una carpeta compartida o a un servidor del CPD. Ahora borra la imagen de tu equipo y recupérala desde el fichero:

```powershell
docker rmi miservidor-web:1.0 miweb-commit:1.0
docker rmi nginx:alpine
docker load -i nginx-alpine.tar
docker images nginx
```

La imagen vuelve a estar disponible sin haber contactado con ningún registro. Es la solución más sencilla para entornos aislados. Su límite es que no escala: con decenas de imágenes y versiones se necesita un **registro privado** dentro de la red, que veremos el día 3.

> Si `docker rmi nginx:alpine` se queja de que la imagen está en uso, elimina primero los contenedores que la usan con `docker ps -a` y `docker rm`.

## Paso 7. Espacio en disco y limpieza

```powershell
docker system df
docker image prune
```

`system df` resume cuánto ocupan imágenes, contenedores, volúmenes y caché de construcción. `image prune` elimina las imágenes "colgantes" (*dangling*), que no tienen etiqueta, normalmente restos de construcciones anteriores. Con `docker image prune -a` se eliminarían todas las imágenes que no use ningún contenedor; **no lo ejecutes ahora**, porque borrarías las imágenes del curso.

```powershell
Remove-Item nginx-alpine.tar
```

---

## Para comprobar lo aprendido

1. ¿Por qué descargar `mcr.microsoft.com/dotnet/aspnet:10.0` después de tener `mcr.microsoft.com/dotnet/sdk:10.0` es más rápido de lo que su tamaño sugiere?
2. ¿Qué etiqueta se descarga con `docker pull nginx`?
3. ¿Qué dos formas hay de llevar una imagen a un servidor sin acceso a internet?

<details>
<summary>Respuestas</summary>

1. Porque comparten capas base y Docker solo descarga las que no tiene.
2. `latest`. Por eso en entornos serios se fija siempre la etiqueta: `latest` cambia con el tiempo.
3. Exportarla a fichero con `docker save` e importarla con `docker load`, o publicarla en un registro privado accesible desde la red interna.

</details>
