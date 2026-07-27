# DigitalOcean App Platform

No ejecutar despliegues hasta configurar URLs reales, bases de datos y Redis externos.

El repositorio es un monorepo. Para las tres APIs el `source_dir` debe ser:

```text
src/eshop-services
```

DigitalOcean debe construir cada servicio con Dockerfile. No configurar un run command que sobrescriba el `ENTRYPOINT` del Dockerfile.

## Auth.API

- Service type: Web Service
- Source directory: `src/eshop-services`
- Dockerfile path, relativo a la raiz del repositorio: `src/eshop-services/Services/Auth/Auth.API/Dockerfile`
- Dockerfile path, relativo al source directory: `Services/Auth/Auth.API/Dockerfile`
- HTTP port: `8080`
- Health check path: `/health`

Variables necesarias:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__AuthDb=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
Cors__AllowedOrigins__0=https://sitio.netlify.app
Jwt__Issuer=...
Jwt__Audience=...
Jwt__Key=...
Jwt__ExpirationMinutes=60
BootstrapAdmin__Enabled=false
BootstrapAdmin__UserName=...
BootstrapAdmin__Email=...
BootstrapAdmin__Password=...
```

`BootstrapAdmin__Password` solo es necesaria cuando `BootstrapAdmin__Enabled=true`.

## Catalog.API

- Service type: Web Service
- Source directory: `src/eshop-services`
- Dockerfile path, relativo a la raiz del repositorio: `src/eshop-services/Services/Catalog/Catalog.API/Dockerfile`
- Dockerfile path, relativo al source directory: `Services/Catalog/Catalog.API/Dockerfile`
- HTTP port: `8080`
- Health check path: `/health`

Variables necesarias:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__CatalogDb=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
Cors__AllowedOrigins__0=https://sitio.netlify.app
Jwt__Issuer=...
Jwt__Audience=...
Jwt__Key=...
Jwt__ExpirationMinutes=60
```

## Basket.API

- Service type: Web Service
- Source directory: `src/eshop-services`
- Dockerfile path, relativo a la raiz del repositorio: `src/eshop-services/Services/Basket/Basket.API/Dockerfile`
- Dockerfile path, relativo al source directory: `Services/Basket/Basket.API/Dockerfile`
- HTTP port: `8080`
- Health check path: `/health`

Variables necesarias:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Database=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
ConnectionStrings__Redis=HOST:PORT,user=default,password=...,ssl=true,abortConnect=false
Cors__AllowedOrigins__0=https://sitio.netlify.app
Jwt__Issuer=...
Jwt__Audience=...
Jwt__Key=...
Jwt__ExpirationMinutes=60
```

## CORS

En Production cada API falla al iniciar si `Cors:AllowedOrigins` no tiene al menos un origen. No usar wildcard `*` en Production.

Ejemplo con dos origenes:

```text
Cors__AllowedOrigins__0=https://sitio.netlify.app
Cors__AllowedOrigins__1=https://dominio-personalizado.com
```

## JWT

Las tres APIs deben compartir exactamente los mismos valores:

```text
Jwt__Issuer
Jwt__Audience
Jwt__Key
```

No guardar valores reales en Git, README, `.env.example` ni archivos `appsettings*.json` versionables.

## Bootstrap Admin

Flujo recomendado:

1. Primer despliegue de `Auth.API`: configurar `BootstrapAdmin__Enabled=true` junto con `BootstrapAdmin__UserName`, `BootstrapAdmin__Email` y `BootstrapAdmin__Password`.
2. Confirmar que el administrador existe.
3. Cambiar `BootstrapAdmin__Enabled=false` y redeplegar.

La API no crea un administrador nuevo si ya existe un usuario con el correo configurado. La contrasena no se registra en logs.

## HTTPS

DigitalOcean termina HTTPS externamente. Las APIs escuchan HTTP en el contenedor por el puerto `8080`. No agregar redireccion HTTPS interna sin configurar forwarded headers.
