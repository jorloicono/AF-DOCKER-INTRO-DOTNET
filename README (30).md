# Bienvenido a Docker — Introducción a contenedores para equipos .NET

Material del alumno del curso introductorio de Docker (15 horas, 3 sesiones de 5 horas). El curso parte de cero y llega hasta aplicaciones de varios contenedores con Docker Compose, con los ejemplos centrados en aplicaciones .NET y SQL Server, y con una sesión final sobre qué hace falta para llevar los contenedores a vuestros servidores.

## Antes de la primera sesión

Sigue la guía [Preparación del entorno](PREPARACION-ENTORNO.md). Lleva unos 30-40 minutos y es importante llegar con Docker funcionando y las imágenes descargadas, para dedicar el tiempo de clase a aprender y no a instalar.

## Programa

### Día 1 — Fundamentos y comandos esenciales

Por qué contenedores y qué problemas resuelven en el día a día de un equipo de desarrollo. Contenedores frente a máquinas virtuales. Arquitectura de Docker: cliente, motor, imágenes, contenedores y registros. Ciclo de vida de un contenedor y comandos esenciales. Imágenes, capas, etiquetas y cómo moverlas sin internet.

| Lab | Contenido |
|---|---|
| [Lab 01 — Primeros pasos](labs/lab01-primeros-pasos/README.md) | Verificar la instalación, primer contenedor, servidor web en un segundo |
| [Lab 02 — Ciclo de vida](labs/lab02-ciclo-de-vida/README.md) | `exec`, `inspect`, `logs`, `cp`, variables de entorno, reinicios; los contenedores son efímeros |
| [Lab 03 — Imágenes y registros](labs/lab03-imagenes-y-registros/README.md) | Capas, etiquetas, `commit` y por qué no usarlo, `save` y `load` para entornos sin internet |

### Día 2 — Dockerfile y aplicaciones .NET

El Dockerfile y sus instrucciones. Contexto de construcción, `.dockerignore` y caché de capas. Imágenes oficiales de .NET y construcción multi-etapa. Configuración por variables de entorno: una imagen, muchos entornos. Aplicaciones .NET Framework y contenedores Windows.

| Lab | Contenido |
|---|---|
| [Lab 04 — Primer Dockerfile](labs/lab04-primer-dockerfile/README.md) | Construir, versionar y volver atrás; caché de capas |
| [Lab 05 — API .NET en contenedor](labs/lab05-api-dotnet/README.md) | Dockerfile multi-etapa para ASP.NET Core 10, usuario sin privilegios |
| [Lab 06 — Configuración](labs/lab06-configuracion/README.md) | Variables de entorno, `--env-file`, ficheros montados, mismo artefacto en tres entornos |

### Día 3 — Datos, redes, Compose e infraestructura

Volúmenes y persistencia. Redes y comunicación entre contenedores. Docker Compose. Buenas prácticas. De IIS a contenedores: qué infraestructura hace falta para desplegar en servidores, registros privados, entornos sin internet y orquestación.

| Lab | Contenido |
|---|---|
| [Lab 07 — Volúmenes y redes](labs/lab07-volumenes-y-redes/README.md) | SQL Server con datos persistentes, DNS entre contenedores, aislamiento |
| [Lab 08 — Docker Compose](labs/lab08-docker-compose/README.md) | API .NET + SQL Server + nginx en un único fichero, healthchecks, escalado |
| [Lab 09 — Reto final](labs/lab09-reto-final/README.md) | Registro privado, despliegue de producción, imágenes chiseled |

## Estructura del repositorio

```
├── PREPARACION-ENTORNO.md      Instalación y verificación antes del curso
├── CHULETA.md                  Referencia rápida de comandos
├── presentaciones/             Diapositivas de cada día en PDF
├── labs/                       Enunciados y código de partida de cada laboratorio
├── soluciones/                 Ficheros resueltos de los labs que piden escribir código
├── referencia/dotnet-framework Plantilla para contenerizar ASP.NET (.NET Framework) en contenedores Windows
└── docs/
    └── DE-IIS-A-CONTENEDORES.md   Guía de infraestructura y hoja de ruta para adoptar contenedores
```

## Convenciones

Los comandos están escritos para **PowerShell** en Windows. Las rutas dentro de los contenedores son de Linux (`/app`, `/var/opt/mssql`), y las de vuestro equipo, de Windows. Cuando un comando difiere en CMD, el enunciado lo indica.

La contraseña de SQL Server que usamos en los labs (`Curso.Docker2026!`) es solo para el curso. Nunca uses contraseñas de ejemplo fuera de un entorno de prácticas.

## Material de consulta

- [Chuleta de comandos](CHULETA.md)
- [De IIS a contenedores: guía de infraestructura](docs/DE-IIS-A-CONTENEDORES.md)
- Documentación oficial de Docker: https://docs.docker.com
- Imágenes oficiales de .NET: https://github.com/dotnet/dotnet-docker
- Contenedores con .NET (Microsoft Learn): https://learn.microsoft.com/dotnet/core/docker/introduction
