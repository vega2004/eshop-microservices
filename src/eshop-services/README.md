# eShop Services

Aplicativo academico de microservicios para un flujo basico de comercio electronico. El alcance actual cubre autenticacion, catalogo y carrito de compras. Ordering.API, checkout, pedidos y pagos quedan pendientes.

## Arquitectura

Produccion prevista:

```text
Netlify React + Vite
-> DigitalOcean App Platform APIs
-> Railway PostgreSQL / Redis
```

Servicios:

- `Auth.API`: registra usuarios, autentica credenciales, emite JWT y permite crear un administrador inicial configurable.
- `Catalog.API`: expone productos, detalle por identificador y consulta por categoria.
- `Basket.API`: administra el carrito del usuario autenticado con JWT y cache distribuida en Redis.
- `AuthDb`: PostgreSQL para `Auth.API`.
- `CatalogDb`: PostgreSQL para `Catalog.API`.
- `BasketDb`: PostgreSQL para `Basket.API`.
- `Redis`: cache de `Basket.API`.
- Frontend React: `../Frontend/eshop-web`.

## Raices Del Monorepo

- Backend: `src/eshop-services`
- Frontend: `src/Frontend/eshop-web`
- Solucion backend: `src/eshop-services/eshop-services.slnx`
- Netlify config: `netlify.toml` en la raiz del repositorio
- DigitalOcean source directory: `src/eshop-services`

## Puertos Locales

- Catalog.API: `http://localhost:6002`
- Basket.API: `http://localhost:6001`
- Auth.API: `http://localhost:6003`
- React: `http://localhost:5173` o `http://localhost:5174`
- CatalogDb: `5433:5432`
- BasketDb: `5434:5432`
- AuthDb: `5435:5432`
- Redis: `6379:6379`

Las imagenes de produccion escuchan HTTP en el puerto de contenedor `8080` mediante `ASPNETCORE_HTTP_PORTS=8080`.

## Endpoints Principales

Catalog.API:

- `GET /health`
- `GET /products`
- `GET /products/{id:guid}`
- `GET /products/category/{category}`
- `POST /products` requiere rol Admin y acepta `name`, `description`, `category`, `imageFiles`, `price`, `stock`.
- `PATCH /products/{id:guid}/stock` requiere rol Admin y actualiza solamente `stock`.

Basket.API:

- `GET /health`
- `GET /basket` requiere JWT.
- `POST /basket` requiere JWT.
- `DELETE /basket` requiere JWT.

Auth.API:

- `GET /health`
- `POST /auth/register`
- `POST /auth/login`
- `GET /auth/me` requiere JWT.

## Variables Locales

Antes de ejecutar Docker Compose, crea `.env` desde `.env.example` en esta carpeta:

```powershell
Copy-Item .env.example .env
```

Configura `AUTH_JWT_KEY` con una llave local de desarrollo de al menos 32 bytes. No guardes claves reales, tokens ni secretos en archivos versionables.

Las conexiones locales viven en `appsettings.Development.json` y `docker-compose.override.yml`; `appsettings.json` no debe contener conexiones locales ni secretos.

## Variables De Produccion

Comunes a las tres APIs:

```text
ASPNETCORE_ENVIRONMENT=Production
Cors__AllowedOrigins__0=https://sitio.netlify.app
Jwt__Issuer=...
Jwt__Audience=...
Jwt__Key=...
Jwt__ExpirationMinutes=60
```

`Jwt__Issuer`, `Jwt__Audience` y `Jwt__Key` deben ser exactamente iguales en `Auth.API`, `Catalog.API` y `Basket.API`. `Jwt__Key` debe tener al menos 32 bytes.

Auth.API:

```text
ConnectionStrings__AuthDb=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
BootstrapAdmin__Enabled=false
BootstrapAdmin__UserName=...
BootstrapAdmin__Email=...
BootstrapAdmin__Password=...
```

Catalog.API:

```text
ConnectionStrings__CatalogDb=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
```

Basket.API:

```text
ConnectionStrings__Database=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
ConnectionStrings__Redis=HOST:PORT,user=default,password=...,ssl=true,abortConnect=false
```

## CORS

Las APIs leen origenes desde `Cors:AllowedOrigins`. En desarrollo se permiten `http://localhost:5173` y `http://localhost:5174`.

En Production no se permite wildcard `*`. Si no se configura ningun origen, la API falla al iniciar con un mensaje claro y sin mostrar secretos.

## Bootstrap Admin

Flujo recomendado:

1. Primer despliegue de `Auth.API`: `BootstrapAdmin__Enabled=true`.
2. Confirmar que el administrador se creo correctamente.
3. Cambiar `BootstrapAdmin__Enabled=false`.

No se crea automaticamente un nuevo administrador si ya existe el correo configurado. La contrasena no se registra en logs.

## Railway

Crear en Railway:

- `auth-postgres`
- `catalog-postgres`
- `basket-postgres`
- `basket-redis`

DigitalOcean debe usar `DATABASE_PUBLIC_URL` o credenciales publicas/TCP Proxy. No usar `*.railway.internal` desde DigitalOcean.

Formato PostgreSQL esperado por Npgsql:

```text
Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
```

Formato Redis esperado por StackExchange.Redis:

```text
HOST:PORT,user=default,password=...,ssl=true,abortConnect=false
```

## Netlify

`netlify.toml` esta en la raiz del repositorio y configura:

- Base directory: `src/Frontend/eshop-web`
- Build command: `npm run build`
- Publish directory: `dist`
- SPA fallback: `/*` hacia `/index.html` con estado `200`

Variables requeridas:

```text
VITE_AUTH_API_URL=https://auth-api.example.com
VITE_CATALOG_API_URL=https://catalog-api.example.com
VITE_BASKET_API_URL=https://basket-api.example.com
```

No guardar `REDIS_PUBLIC_URL` ni credenciales de Redis en frontend.

## Orden De Despliegue

1. Crear servicios Railway PostgreSQL y Redis.
2. Configurar las tres APIs en DigitalOcean App Platform con puerto HTTP `8080` y health check `/health`.
3. Desplegar `Auth.API` con `BootstrapAdmin__Enabled=true` solo si se necesita crear el administrador inicial.
4. Confirmar `/health` en las tres APIs.
5. Confirmar login y creacion del administrador.
6. Cambiar `BootstrapAdmin__Enabled=false`.
7. Configurar Netlify con `VITE_*` apuntando a las URLs publicas de DigitalOcean.
8. Configurar `Cors__AllowedOrigins__0` con la URL real de Netlify y redeplegar APIs si cambia.

## Pruebas Posteriores

- `GET /health` en Auth, Catalog y Basket debe responder `200`.
- Registro y login deben emitir JWT valido.
- Catalogo debe listar productos.
- Operaciones de administrador deben requerir rol `Admin`.
- Carrito debe persistir en BasketDb y usar Redis sin exponerlo al navegador.
- Frontend debe cargar rutas internas directamente gracias al fallback SPA.

## Comandos Backend

Desde `src/eshop-services`:

```powershell
dotnet restore "eshop-services.slnx" --ignore-failed-sources
dotnet build "eshop-services.slnx"
docker compose config
docker compose up -d --build
docker compose ps
```

No ejecutes `docker compose down -v` en desarrollo salvo que quieras borrar definitivamente las bases locales.

## Comandos Frontend

Desde `src/Frontend/eshop-web`:

```powershell
npm run dev
npm run lint
npm run build
```

El frontend local usa `.env` con URLs locales. `.env.example` contiene placeholders y no debe incluir secretos reales.

## Seguridad

El frontend usa `sessionStorage` para conservar la sesion durante la vida de la pestana. Esta es una solucion academica para el alcance actual y no reemplaza un esquema completo de refresh tokens con cookies `HttpOnly`, rotacion de tokens y controles adicionales de produccion.
