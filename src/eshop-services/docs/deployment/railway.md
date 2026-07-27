# Railway

Crear cuatro servicios en Railway:

- `auth-postgres`
- `catalog-postgres`
- `basket-postgres`
- `basket-redis`

No guardar credenciales reales en Git ni exponer Redis al frontend.

## PostgreSQL

DigitalOcean App Platform debe usar `DATABASE_PUBLIC_URL` o las credenciales publicas/TCP Proxy de Railway. No usar dominios internos `*.railway.internal` desde DigitalOcean.

Transformar las credenciales publicas de Railway al formato Npgsql esperado por las APIs:

```text
Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require
```

Asignacion de variables en DigitalOcean:

```text
Auth.API     -> ConnectionStrings__AuthDb
Catalog.API  -> ConnectionStrings__CatalogDb
Basket.API   -> ConnectionStrings__Database
```

Si Railway entrega una URL como `postgresql://USER:PASSWORD@HOST:PORT/DATABASE`, mapearla asi:

```text
Host=HOST;Port=PORT;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require
```

Marten crea los esquemas necesarios en bases nuevas con su comportamiento por defecto. No ejecutar migraciones destructivas ni borrar volumenes de desarrollo para simular produccion.

## Redis

`Basket.API` usa `Microsoft.Extensions.Caching.StackExchangeRedis`, por lo que `ConnectionStrings__Redis` debe usar formato compatible con StackExchange.Redis.

Formato recomendado para Railway Redis publico:

```text
HOST:PORT,user=default,password=...,ssl=true,abortConnect=false
```

Para desarrollo local con Docker Compose se mantiene:

```text
distributedcache:6379
```

No usar `REDIS_PUBLIC_URL` en el frontend. Redis nunca debe exponerse al navegador.
