# De IIS a contenedores: guía de infraestructura y hoja de ruta

Este documento acompaña a la última sesión del curso. Responde a la pregunta que queda cuando Docker ya funciona en el portátil: **¿qué hace falta para desplegar contenedores en nuestros servidores?** Está pensado para servir de punto de partida a la conversación entre desarrollo y sistemas, no como un diseño cerrado.

## 1. Por qué merece la pena

Hoy, desplegar una aplicación significa preparar un servidor con IIS, instalar la versión correcta de .NET y sus dependencias, configurar el sitio y el *application pool*, copiar ficheros o un paquete de Web Deploy, aplicar transformaciones de `web.config` por entorno y confiar en que el servidor de producción se parezca al de preproducción. Cada servidor acumula con el tiempo cambios manuales que nadie documentó.

Con contenedores, la aplicación viaja con todo lo que necesita. Los beneficios concretos son estos:

- **Entornos idénticos.** La imagen que se prueba en desarrollo y preproducción es exactamente la que se ejecuta en producción. Desaparece el "en mi máquina funciona".
- **Despliegues y marchas atrás triviales.** Desplegar es arrancar una imagen nueva; volver atrás es arrancar la anterior, que sigue intacta en el registro.
- **Convivencia de versiones.** Una aplicación en .NET 8 y otra en .NET 10 pueden ejecutarse en el mismo servidor sin interferir, y migrar una no obliga a tocar las demás.
- **Incorporación inmediata.** Un desarrollador nuevo levanta la aplicación con su base de datos con `docker compose up`, sin días de instalación.
- **Infraestructura como código.** El Dockerfile y el `compose.yaml` documentan con exactitud cómo se construye y ejecuta cada aplicación, y se revisan en Git como el resto del código.
- **Mejor aprovechamiento del hardware.** Los contenedores comparten el núcleo del sistema y arrancan en segundos; se pueden ejecutar muchos más que máquinas virtuales en el mismo servidor.
- **Encaje con la migración a .NET.** El .NET moderno está diseñado para contenedores Linux. Migrar a .NET y contenerizar son dos pasos que se refuerzan.

Y, para ser justos, lo que cuesta: aprender una herramienta nueva, montar algunas piezas de infraestructura (las de este documento), cambiar hábitos de operación (no se "arregla" un servidor en caliente) y revisar cómo se gestionan los logs, los secretos y las copias de seguridad.

## 2. Qué cambia y qué no

| Hoy (IIS) | Con contenedores |
|---|---|
| Servidor Windows con IIS y runtime de .NET instalado | Servidor (Linux o Windows) con un motor de contenedores; el runtime va dentro de la imagen |
| Paquete Web Deploy o copia de ficheros | Imagen versionada publicada en un registro |
| `web.config` transformado por entorno | La misma imagen en todos los entornos; configuración por variables de entorno o ficheros montados |
| Sitio y *application pool* | Contenedor (uno por aplicación o servicio) |
| IIS como servidor web y proxy (ARR) | Kestrel dentro del contenedor y un proxy inverso delante (nginx, Traefik, YARP o el propio IIS) |
| Logs en ficheros del servidor | Logs a la salida estándar, recogidos por el motor y enviados a una plataforma central |
| Marcha atrás: restaurar copia o redesplegar | Marcha atrás: arrancar la imagen anterior |

Lo que **no** cambia: las bases de datos de producción pueden seguir donde están. Contenerizar la aplicación no obliga a contenerizar SQL Server; de hecho, lo habitual es empezar por las aplicaciones y mantener las bases de datos de producción en su infraestructura actual.

## 3. Las piezas de infraestructura necesarias

### 3.1. Hosts de contenedores

Son los servidores donde se ejecutan los contenedores.

**Para aplicaciones .NET moderno (recomendado):** servidores Linux (por ejemplo, Ubuntu Server LTS o Red Hat Enterprise Linux) con Docker Engine o, en entornos Red Hat, Podman. Son la opción más madura, ligera y documentada.

**Para aplicaciones .NET Framework:** servidores Windows Server 2022 o 2025 con un runtime de contenedores Windows (Docker CE/Moby mediante el script de instalación de Microsoft, Mirantis Container Runtime con soporte comercial, o containerd). La versión de Windows del host condiciona qué imágenes se pueden ejecutar.

Dimensionamiento orientativo para empezar: dos servidores por entorno para tener redundancia, con CPU y memoria equivalentes a la suma de lo que hoy consumen las aplicaciones que se vayan a mover, más un margen. Los contenedores no añaden el coste de un sistema operativo invitado por aplicación, como ocurre con las máquinas virtuales.

### 3.2. Registro privado de imágenes

Es la pieza clave en un CPD sin salida a internet. Almacena las imágenes de vuestras aplicaciones y también una **copia controlada de las imágenes base** (las de .NET, nginx, SQL Server...), de forma que los servidores nunca necesitan acceder a registros públicos.

| Opción | Comentario |
|---|---|
| **Harbor** | Proyecto de la CNCF, gratuito. Incluye control de acceso por proyectos, escaneo de vulnerabilidades, replicación y firma de imágenes. Muy extendido en administraciones públicas |
| **Sonatype Nexus Repository** | Además de imágenes Docker, sirve como *feed* interno de NuGet y npm. Interesante si también se necesita un NuGet interno |
| **JFrog Artifactory** | Similar a Nexus, comercial |
| **GitLab Container Registry** | Integrado si el código y los pipelines ya están en GitLab |
| **Azure DevOps Server** | Ofrece Artifacts (NuGet) pero no registro de contenedores *on-premise*; habría que combinarlo con otro producto |

### 3.3. Proceso de entrada de imágenes base

Si el CPD no tiene salida a internet, hay que definir cómo entran las actualizaciones de las imágenes base (por ejemplo, el parche mensual de .NET). Dos patrones habituales:

- **Replicación controlada:** un registro en una zona con salida a internet (DMZ) replica las imágenes aprobadas hacia el registro interno. Harbor y Nexus lo soportan de forma nativa.
- **Transferencia manual:** `docker save` en un equipo con acceso, transferencia del fichero por el canal autorizado y `docker load` + `docker push` en el registro interno. Sencillo, pero manual.

En ambos casos conviene escanear las imágenes antes de admitirlas.

### 3.4. Feed interno de paquetes NuGet

La compilación de aplicaciones .NET descarga paquetes de `nuget.org`. Si los agentes de compilación no tienen salida a internet, hace falta un *feed* interno (Nexus, Artifactory, Azure DevOps Artifacts o BaGet) y un `nuget.config` que apunte a él.

### 3.5. Pipeline de integración y despliegue

El flujo objetivo es:

1. El desarrollador sube código al repositorio.
2. El pipeline construye la imagen (`docker build`), ejecuta las pruebas y la escanea.
3. La imagen se etiqueta con la versión y se publica en el registro privado.
4. El despliegue en cada entorno consiste en actualizar la etiqueta de la imagen y arrancarla (`docker compose pull && docker compose up -d`, o el equivalente del orquestador).

Esto encaja con cualquier herramienta de CI/CD que ya utilicéis (Azure DevOps Server, Jenkins, GitLab CI...), siempre que sus agentes tengan Docker.

### 3.6. Proxy inverso y certificados

Delante de los contenedores se coloca un proxy inverso que publica las aplicaciones, termina TLS con los certificados de la organización y reparte el tráfico entre instancias. Puede ser nginx, Traefik, HAProxy o incluso el IIS actual con ARR durante la transición, lo que permite mover aplicaciones una a una sin cambiar las URL públicas.

### 3.7. Logs, monitorización y copias de seguridad

- **Logs:** las aplicaciones escriben en la salida estándar; el motor los recoge y un agente los envía a la plataforma central que ya uséis (ELK/OpenSearch, Graylog, Splunk, Loki...). Hay que configurar la rotación en los hosts.
- **Monitorización:** métricas de hosts y contenedores (Prometheus y Grafana son la combinación más habitual) y los *healthchecks* de cada aplicación.
- **Copias de seguridad:** se respaldan los volúmenes con datos y el registro de imágenes. Los contenedores no se respaldan: se recrean desde la imagen.

### 3.8. Secretos

Las contraseñas y cadenas de conexión no deben ir en las imágenes ni en el repositorio. En un único servidor con Compose pueden ir en ficheros `.env` protegidos o en *secrets* de Compose; con un orquestador, en su gestor de secretos; y a medio plazo, en una solución dedicada como HashiCorp Vault.

## 4. Orquestación: ¿hace falta Kubernetes?

Un **orquestador** gestiona contenedores repartidos entre varios servidores: decide dónde se ejecuta cada uno, los reinicia si fallan, los mueve si un servidor cae y permite actualizaciones sin corte.

| Nivel | Qué es | Cuándo encaja |
|---|---|---|
| **Docker Compose en cada servidor** | Lo visto en el curso, desplegado en uno o varios hosts | Primer paso. Pocas aplicaciones, alta disponibilidad resuelta con dos hosts y el proxy repartiendo carga |
| **Docker Swarm** | Orquestador integrado en Docker, usa ficheros casi idénticos a Compose | Varios servidores con poca complejidad añadida. Menos ecosistema que Kubernetes |
| **Kubernetes** | El estándar del sector. Distribuciones *on-premise*: RKE2, k3s, OpenShift, Tanzu... | Muchas aplicaciones y equipos, necesidad de autoescalado y despliegues sin corte. Requiere personal formado para operarlo |

Recomendación: **no empezar por Kubernetes**. Empezar con Compose en un par de servidores permite ganar experiencia con contenedores sin asumir la complejidad de operar un clúster, y todo lo aprendido (imágenes, registro, pipeline, configuración por entorno) se reutiliza tal cual si más adelante se da el salto.

## 5. Hoja de ruta propuesta

**Fase 0 — Desarrollo local (inmediata).** Docker en los equipos de desarrollo. Bases de datos y dependencias de desarrollo en contenedores con Compose. Sin impacto en producción, y ya aporta beneficios.

**Fase 1 — Piloto.** Una aplicación interna, poco crítica y ya migrada a .NET moderno. Montar el registro privado, el pipeline y dos hosts Linux en un entorno de preproducción. Medir y documentar.

**Fase 2 — Producción del piloto.** Llevar la aplicación piloto a producción con Compose detrás del proxy. Definir los procedimientos de operación: despliegue, marcha atrás, logs, actualización de imágenes base.

**Fase 3 — Adopción progresiva.** Contenerizar cada aplicación a medida que se migra a .NET. Dejar los contenedores Windows para casos .NET Framework concretos que lo justifiquen.

**Fase 4 — Orquestación (si se necesita).** Con un número de aplicaciones suficiente, valorar Swarm o Kubernetes.

## 6. Preguntas para la reunión con sistemas

1. ¿Qué sistema operativo preferimos para los hosts de contenedores, y quién los opera?
2. ¿Dónde se ubicaría el registro privado y cómo se replican las imágenes base desde el exterior?
3. ¿Existe ya un *feed* NuGet interno o hay que montarlo?
4. ¿Qué herramienta de CI/CD usamos y sus agentes pueden ejecutar Docker?
5. ¿Cómo se integran los logs y la monitorización con las plataformas actuales?
6. ¿Qué licencias aplican (Docker Desktop en desarrollo, runtime en Windows Server, SQL Server en contenedores)?
7. ¿Qué aplicación elegimos como piloto?
