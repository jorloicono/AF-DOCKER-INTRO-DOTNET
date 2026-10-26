# Imagen base: nginx sobre Alpine Linux (pequeña, unos 50 MB)
FROM nginx:alpine

# Metadatos de la imagen
LABEL org.opencontainers.image.title="mi-web" \
      org.opencontainers.image.description="Primer Dockerfile del curso" \
      org.opencontainers.image.authors="Curso Docker"

# Copiamos nuestra web a la carpeta que sirve nginx
COPY web/ /usr/share/nginx/html/

# Documentamos el puerto en el que escucha nginx
EXPOSE 80

# No hace falta CMD: la imagen base ya arranca nginx
