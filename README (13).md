# Lab 02 — Ciclo de vida de un contenedor

**Día 1 · Duración estimada: 45 minutos**

## Objetivo

Trabajar con contenedores como lo harás en el día a día: entrar en ellos, inspeccionarlos, pasarles variables, copiar ficheros y, sobre todo, comprobar por ti mismo que **los cambios dentro de un contenedor son efímeros**. Esa comprobación es la que justifica los Dockerfile del día 2 y los volúmenes del día 3.

---

## Paso 1. Entrar en un contenedor

Arranca un nginx como en el lab anterior:

```powershell
docker run -d -p 8080:80 --name web1 nginx:alpine
```

Ahora abre una terminal **dentro** del contenedor:

```powershell
docker exec -it web1 sh
```

`exec` ejecuta un comando en un contenedor que ya está en marcha; `-it` lo hace interactivo, con terminal. El prompt cambia: estás dentro de un Linux mínimo. Prueba algunos comandos:

```sh
cat /etc/os-release
ps
ls /usr/share/nginx/html
hostname
```

Fíjate en `ps`: dentro del contenedor solo hay unos pocos procesos, y el primero (PID 1) es nginx. El contenedor no "arranca un sistema operativo", solo ejecuta ese proceso aislado. El `hostname` coincide con el identificador corto del contenedor.

## Paso 2. Modificar el contenedor desde dentro

Sin salir del contenedor, cambia la página de inicio:

```sh
echo "<h1>Modificado a mano dentro del contenedor</h1>" > /usr/share/nginx/html/index.html
exit
```

Recarga http://localhost:8080 y verás el cambio.

## Paso 3. ¿Sobrevive el cambio?

```powershell
docker stop web1
docker start web1
```

Recarga el navegador: el cambio **sigue ahí**. Parar y arrancar conserva el contenedor y su sistema de ficheros.

Ahora elimínalo y crea uno nuevo a partir de la misma imagen:

```powershell
docker rm -f web1
docker run -d -p 8080:80 --name web1 nginx:alpine
```

Recarga: vuelve a aparecer la página original de nginx. **El cambio se ha perdido**, porque vivía en la capa de escritura de ese contenedor concreto y la imagen nunca se modificó.

> Esta es la idea más importante del día: un contenedor es desechable. Lo que deba perdurar va en la **imagen** (si es código o configuración que se construye) o en un **volumen** (si son datos). Nunca se "arregla" un contenedor en caliente como se arreglaría un servidor.

## Paso 4. Inspeccionar

```powershell
docker inspect web1
```

La salida es un JSON con toda la configuración del contenedor: imagen, variables de entorno, puertos, red, IP, montajes... Puedes extraer un dato concreto con `--format`:

```powershell
docker inspect --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" web1
docker inspect --format "{{.Config.Image}}" web1
```

Otros comandos útiles de observación:

```powershell
docker top web1
docker stats --no-stream
docker logs --tail 5 web1
docker logs -f web1
```

`docker logs -f` sigue el log en vivo: recarga el navegador y verás aparecer las peticiones. Pulsa `Ctrl+C` para dejar de seguirlo (el contenedor no se detiene).

## Paso 5. Copiar ficheros entre tu equipo y el contenedor

Crea un fichero HTML en tu carpeta de trabajo y cópialo dentro:

```powershell
"<h1>Copiado con docker cp</h1>" | Out-File -Encoding utf8 pagina.html
docker cp pagina.html web1:/usr/share/nginx/html/pagina.html
```

Abre http://localhost:8080/pagina.html. También funciona en sentido contrario, por ejemplo para sacar la configuración de nginx y estudiarla:

```powershell
docker cp web1:/etc/nginx/conf.d/default.conf .
```

## Paso 6. Variables de entorno

Las variables de entorno son la forma estándar de configurar un contenedor desde fuera. Compruébalo con un contenedor de usar y tirar:

```powershell
docker run --rm -e ENTORNO=Desarrollo -e ORGANISMO="Consejería de Hacienda" alpine env
```

La opción `--rm` elimina el contenedor automáticamente al terminar, ideal para pruebas. Verás tus dos variables junto a las que define la propia imagen. El día 2 usaremos exactamente este mecanismo para configurar la API .NET sin tocar su código.

## Paso 7. Contenedores interactivos

No todo contenedor es un servidor. Puedes usar una imagen como "caja de herramientas" temporal:

```powershell
docker run -it --rm alpine sh
```

Dentro puedes instalar algo con `apk add curl`, probarlo y salir con `exit`. Al salir, el contenedor desaparece con todo lo que instalaste, sin ensuciar tu equipo.

## Paso 8. Políticas de reinicio

En un servidor queremos que los contenedores se levanten solos tras un reinicio o un fallo:

```powershell
docker run -d --name web2 --restart unless-stopped -p 8081:80 nginx:alpine
docker inspect --format "{{.HostConfig.RestartPolicy.Name}}" web2
```

Las políticas habituales son `no` (por defecto), `on-failure`, `always` y `unless-stopped`. Esta última es la más usada: reinicia siempre salvo que tú lo hayas parado a propósito. Es el equivalente a configurar un servicio de Windows en inicio automático.

## Paso 9. Limpieza

```powershell
docker rm -f web1 web2
Remove-Item pagina.html, default.conf
```

---

## Reto (si te sobra tiempo)

Arranca tres contenedores nginx a la vez, en los puertos 8081, 8082 y 8083, cada uno con un nombre distinto. Usa `docker exec` para que cada uno muestre un texto diferente en su página de inicio y comprueba los tres en el navegador. Después elimínalos todos con un único comando.

<details>
<summary>Solución</summary>

```powershell
docker run -d --name n1 -p 8081:80 nginx:alpine
docker run -d --name n2 -p 8082:80 nginx:alpine
docker run -d --name n3 -p 8083:80 nginx:alpine
docker exec n1 sh -c "echo '<h1>Uno</h1>' > /usr/share/nginx/html/index.html"
docker exec n2 sh -c "echo '<h1>Dos</h1>' > /usr/share/nginx/html/index.html"
docker exec n3 sh -c "echo '<h1>Tres</h1>' > /usr/share/nginx/html/index.html"
docker rm -f n1 n2 n3
```

Fíjate en que los tres usan el puerto 80 **dentro** de su contenedor sin conflicto, porque cada contenedor tiene su propia red aislada. Solo los puertos publicados en tu equipo (8081, 8082, 8083) tienen que ser distintos.

</details>
