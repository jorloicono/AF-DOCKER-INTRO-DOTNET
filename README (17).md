# Referencia: aplicaciones .NET Framework en contenedores Windows

Esta carpeta no forma parte de los laboratorios obligatorios. Es una plantilla para que veáis cómo se contenerizaría hoy una aplicación ASP.NET clásica (.NET Framework 4.8.x sobre IIS), y qué implica hacerlo.

## Lo que hay que saber antes de nada

Una aplicación de .NET Framework solo se ejecuta sobre Windows, así que su contenedor tiene que ser un **contenedor Windows**. Eso tiene varias consecuencias prácticas:

- **El host tiene que ser Windows.** En un portátil, Docker Desktop permite cambiar a "Windows containers" (menú de la ballena → *Switch to Windows containers*), lo que exige Windows 10/11 Pro o Enterprise con Hyper-V. En servidor, Windows Server 2019, 2022 o 2025 con un runtime de contenedores (Docker CE/Moby, Mirantis Container Runtime o containerd).
- **La versión de Windows de la imagen debe ser compatible con la del host.** Una imagen `ltsc2022` necesita Windows Server 2022 (o un host más nuevo con aislamiento Hyper-V). Por eso la etiqueta de la imagen se elige según el servidor de destino.
- **Las imágenes son grandes.** La base `windowsservercore` ocupa varios GB, frente a las decenas o pocos cientos de MB de una imagen Linux de .NET moderno. La primera descarga y el almacenamiento en el registro hay que planificarlos.
- **Dependencias del sistema.** Todo lo que hoy se instala a mano en el servidor (componentes COM, fuentes, drivers, certificados, configuración de IIS, cuentas de dominio) hay que reproducirlo en el Dockerfile. La autenticación Windows integrada en contenedores requiere gMSA (cuentas de servicio administradas de grupo), que es una pieza más de infraestructura.

## Cómo usar la plantilla

1. Copia el `Dockerfile` a la carpeta de la solución (`.sln`).
2. Sustituye `MiAplicacionWeb` por el nombre real del proyecto.
3. Si el proyecto usa `PackageReference` en lugar de `packages.config`, elimina la línea que copia `packages.config`.
4. Con Docker Desktop en modo Windows containers:

```powershell
docker build -t miaplicacion-legacy:1.0 .
docker run -d -p 8081:80 --name legacy miaplicacion-legacy:1.0
```

5. Abre `http://localhost:8081`.

## Recomendación para vuestra hoja de ruta

Contenerizar aplicaciones de .NET Framework es posible y a veces útil como paso intermedio (por ejemplo, para retirar servidores antiguos o estandarizar despliegues), pero el retorno es mucho mayor en las aplicaciones que ya hayáis migrado a .NET moderno: imágenes Linux pequeñas, arranque en segundos, hosts más baratos y el ecosistema completo de herramientas. La recomendación del curso es **contenerizar a medida que se migra a .NET**, y dejar los contenedores Windows solo para casos concretos que lo justifiquen.

Referencias oficiales: el repositorio `microsoft/dotnet-framework-docker` en GitHub incluye ejemplos mantenidos por Microsoft.
