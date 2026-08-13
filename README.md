# 🛒 E-Shop Microservices

Proyecto de comercio electrónico desarrollado con una arquitectura basada en **microservicios**, utilizando **ASP.NET Core .NET 9**, **React**, **PostgreSQL**, **Redis** y **MongoDB Atlas**.

El sistema permite autenticación de usuarios, administración de productos, manejo de carrito de compras, generación y consulta de órdenes, administración de órdenes por parte de usuarios con rol `Admin` y generación de tickets de compra en formato PDF.

---

## 📌 Arquitectura del proyecto

La solución está compuesta por los siguientes microservicios:

| Servicio | Tecnología | Persistencia |
|---|---|---|
| Auth.API | ASP.NET Core .NET 9 | PostgreSQL + Marten |
| Catalog.API | ASP.NET Core .NET 9 | PostgreSQL + Marten |
| Basket.API | ASP.NET Core .NET 9 | PostgreSQL + Marten + Redis |
| Orders.API | ASP.NET Core Minimal API .NET 9 | MongoDB Atlas |
| Tickets.API | ASP.NET Core Minimal API .NET 9 | Sin base de datos |
| Frontend | React + Vite | — |

Arquitectura general:

```text
                         ┌─────────────────────┐
                         │       React         │
                         │      Netlify        │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
         Auth.API             Catalog.API          Basket.API
              │                    │                    │
              ▼                    ▼                    ├──── Redis
         PostgreSQL           PostgreSQL                │
                                                        ▼
                                                   PostgreSQL

                                   │
                                   ▼
                              Orders.API
                                   │
                  ┌────────────────┼────────────────┐
                  │                │                │
                  ▼                ▼                ▼
             Basket.API       Catalog.API      MongoDB Atlas

                                   │
                                   ▼
                              Tickets.API
                                   │
                                   ▼
                              Orders.API
```

---

# 🚀 Funcionalidades

## 🔐 Autenticación

El microservicio `Auth.API` permite:

- Registro de usuarios.
- Inicio de sesión.
- Consulta del usuario autenticado.
- Manejo de roles.
- Autenticación mediante JWT.

Endpoints principales:

```http
POST /auth/register
POST /auth/login
GET  /auth/me
```

Los JWT contienen información como:

- `sub`
- `unique_name`
- `email`
- `role`

---

## 📦 Catálogo de productos

`Catalog.API` permite administrar los productos disponibles en la tienda.

Cada producto contiene información como:

- Id
- Nombre
- Descripción
- Categorías
- Imagen
- Precio
- Stock

Permite:

- Consultar productos.
- Consultar producto por Id.
- Crear productos.
- Modificar stock.
- Eliminar productos.
- Consultar productos por categoría.

---

## 🛒 Carrito de compras

`Basket.API` administra el carrito del usuario autenticado.

Utiliza:

- PostgreSQL para persistencia.
- Redis como sistema de caché.

Permite:

```http
GET    /basket
POST   /basket
DELETE /basket
```

El usuario del carrito se obtiene mediante el JWT.

Ejemplo conceptual de carrito:

```json
{
  "cart": {
    "userId": "usuario",
    "items": [
      {
        "quantity": 2,
        "color": "Estándar",
        "price": 549.00,
        "productId": "guid-producto",
        "productName": "Mouse inalámbrico"
      }
    ],
    "totalPrice": 1098.00
  }
}
```

---

# 🧾 Microservicio de Órdenes

`Orders.API` fue desarrollado como **ASP.NET Core Minimal API** y utiliza **MongoDB Atlas** como base de datos.

Su responsabilidad es:

- Crear órdenes.
- Consultar una orden.
- Consultar órdenes por cliente.
- Consultar todas las órdenes como administrador.
- Buscar órdenes.
- Cambiar el estado de una orden.
- Validar Basket y Catalog antes de generar la compra.
- Evitar órdenes duplicadas mediante idempotencia.

---

## 📋 Modelo de Orden

Una orden almacena:

```text
Id
OrderNumber
CustomerId
CustomerUserName
CustomerEmail
CreatedAt
Status
Items
Subtotal
Tax
Total
IdempotencyKey
```

Cada `OrderItem` contiene:

```text
ProductId
ProductName
Quantity
UnitPrice
LineTotal
```

---

## 🔢 Folio público

Aunque cada orden mantiene un `Id` técnico interno, al usuario se le muestra un folio amigable:

```text
ORD-20260813-FKW9TK
```

El formato utilizado es:

```text
ORD-YYYYMMDD-XXXXXX
```

El GUID interno no se muestra en la interfaz.

---

## ➕ Crear una orden

```http
POST /api/orders
```

Requiere:

```http
Authorization: Bearer <JWT>
Idempotency-Key: order-<uuid>
```

Ejemplo de body:

```json
{}
```

Antes de crear la orden, `Orders.API` realiza las siguientes validaciones:

1. Obtiene el Basket del usuario autenticado.
2. Valida que el carrito exista.
3. Valida que contenga productos.
4. Consulta cada producto en `Catalog.API`.
5. Valida existencia del producto.
6. Valida cantidad.
7. Valida stock disponible.
8. Valida que el precio coincida con Catalog.
9. Calcula subtotal.
10. Calcula impuestos.
11. Calcula total.
12. Persiste la orden en MongoDB Atlas.

---

## 🔁 Idempotencia

La creación de órdenes utiliza el header:

```http
Idempotency-Key
```

Esto permite que una misma solicitud enviada más de una vez no genere órdenes duplicadas.

La combinación:

```text
CustomerId + IdempotencyKey
```

es única.

Si el cliente vuelve a enviar la misma solicitud con la misma clave, se devuelve la orden creada anteriormente.

---

## 🔍 Consultar una orden

```http
GET /api/orders/{id}
```

Un cliente solamente puede consultar sus propias órdenes.

Un administrador puede consultar cualquier orden.

---

## 👤 Órdenes por cliente

```http
GET /api/orders/customer/{customerId}
```

Permite consultar todas las órdenes pertenecientes a un cliente.

---

## 👨‍💼 Gestión administrativa de órdenes

Los usuarios con rol `Admin` pueden consultar todas las órdenes:

```http
GET /api/orders
```

También pueden realizar búsquedas:

```http
GET /api/orders?search=valor
```

La búsqueda permite localizar órdenes utilizando:

- Folio.
- Nombre de usuario.
- Correo electrónico.
- CustomerId.

Ejemplos:

```http
GET /api/orders?search=Alexvega
```

```http
GET /api/orders?search=usuario@correo.com
```

```http
GET /api/orders?search=ORD-20260813-FKW9TK
```

---

# 🔄 Estados de una orden

Los estados disponibles son:

```text
Pending
Confirmed
Cancelled
```

En la interfaz se muestran como:

```text
Pending   → Pendiente
Confirmed → Confirmada
Cancelled → Cancelada
```

Transiciones válidas:

```text
Pending → Confirmed
Pending → Cancelled
```

Transiciones inválidas:

```text
Confirmed → Cancelled
Confirmed → Pending
Cancelled → Confirmed
Cancelled → Pending
```

Una transición inválida devuelve:

```http
409 Conflict
```

---

## ✅ Cambiar estado

```http
PATCH /api/orders/{id}/status
```

Ejemplo para confirmar:

```json
{
  "status": "Confirmed"
}
```

Ejemplo para cancelar:

```json
{
  "status": "Cancelled"
}
```

Esta funcionalidad se utiliza desde la pantalla administrativa.

---

# 🧾 Tickets.API

La generación de tickets se encuentra separada de `Orders.API`.

`Tickets.API` es una **Minimal API independiente**, responsable únicamente de generar documentos PDF.

No se conecta directamente a MongoDB.

Arquitectura:

```text
React
   │
   ▼
Tickets.API
   │
   │ GET /api/orders/{id}
   ▼
Orders.API
   │
   ▼
MongoDB Atlas
```

Tickets reenvía el JWT recibido hacia Orders para conservar las reglas de autorización.

---

## 📄 Generar ticket PDF

```http
GET /api/tickets/orders/{orderId}
```

Requiere:

```http
Authorization: Bearer <JWT>
```

Respuesta:

```http
Content-Type: application/pdf
```

Nombre del archivo:

```text
ticket-ORD-20260813-FKW9TK.pdf
```

---

## 🎨 Contenido del ticket

El ticket contiene:

- Identidad visual E-Shop.
- Folio.
- Fecha.
- Estado.
- Nombre del cliente.
- Correo electrónico.
- Tabla de productos.
- Cantidad.
- Precio unitario.
- Importe.
- Subtotal.
- Impuestos.
- Total.
- Mensaje de agradecimiento.

No contiene información técnica como:

- `Order.Id`
- `CustomerId`
- `IdempotencyKey`

El documento puede ser:

- Visualizado.
- Descargado.
- Impreso.

---

# 💻 Frontend React

El frontend está desarrollado utilizando:

```text
React
Vite
JavaScript
CSS
```

El frontend consume todos los microservicios mediante HTTP.

---

## 🛍️ Flujo de compra

```text
Login
   ↓
Catálogo
   ↓
Agregar productos
   ↓
Carrito
   ↓
Realizar compra
   ↓
Orders.API
   ↓
MongoDB Atlas
   ↓
Orden creada
   ↓
Confirmación
```

Después de realizar una compra se muestra:

- Folio.
- Fecha.
- Estado.
- Productos.
- Subtotal.
- Impuestos.
- Total.

---

# 📋 Mis órdenes

Ruta:

```text
/orders
```

Permite al usuario autenticado:

- Consultar sus órdenes.
- Buscar por folio.
- Buscar por estado.
- Buscar por fecha.
- Consultar detalle.
- Visualizar ticket PDF.

Filtros disponibles:

```text
Todas
Pendientes
Confirmadas
Canceladas
```

---

# 👨‍💼 Gestión de órdenes

Ruta:

```text
/admin/orders
```

Disponible únicamente para usuarios con rol:

```text
Admin
```

Permite:

- Visualizar órdenes de todos los usuarios.
- Buscar por folio.
- Buscar por nombre de usuario.
- Buscar por correo.
- Consultar detalle.
- Confirmar órdenes.
- Cancelar órdenes.
- Visualizar tickets PDF.

---

# 🗄️ Bases de datos

## PostgreSQL

Utilizado por:

```text
Auth.API
Catalog.API
Basket.API
```

En producción se encuentra desplegado en:

```text
Railway
```

---

## Redis

Utilizado por:

```text
Basket.API
```

para almacenamiento temporal y caché del carrito.

En producción se encuentra desplegado en:

```text
Railway
```

---

## MongoDB Atlas

Utilizado exclusivamente por:

```text
Orders.API
```

Base de datos:

```text
OrdersDb
```

Colección:

```text
orders
```

Índices principales:

```text
ux_orders_customer_idempotency
ix_orders_customer_id
ux_orders_order_number
ix_orders_customer_user_name
ix_orders_customer_email
```

---

# ☁️ Despliegue

La aplicación utiliza diferentes servicios cloud.

## Render

Los microservicios se despliegan como contenedores Docker:

```text
eshop-auth-api
eshop-catalog-api
eshop-basket-api
eshop-orders-api
eshop-ticket-api
```

Cada proyecto utiliza:

```text
.NET 9
Docker
Puerto 8080
```

---

## Railway

Utilizado para:

```text
PostgreSQL
Redis
```

---

## MongoDB Atlas

Utilizado para la persistencia de órdenes.

---

## Netlify

El frontend React se encuentra desplegado en Netlify.

```text
https://eshop-vega2004.netlify.app
```

---

# ⚙️ Variables de entorno

> ⚠️ Nunca guardar credenciales reales en GitHub.

## Orders.API

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
PORT=8080

ConnectionStrings__MongoDb=<MONGODB_ATLAS_CONNECTION_STRING>

MongoDb__DatabaseName=OrdersDb
MongoDb__OrdersCollection=orders

Jwt__Issuer=<JWT_ISSUER>
Jwt__Audience=<JWT_AUDIENCE>
Jwt__Key=<JWT_SECRET_KEY>

Services__BasketBaseUrl=<BASKET_API_URL>
Services__CatalogBaseUrl=<CATALOG_API_URL>

Orders__TaxRate=0.16

Cors__AllowedOrigins__0=<FRONTEND_URL>
```

---

## Tickets.API

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
PORT=8080

Jwt__Issuer=<JWT_ISSUER>
Jwt__Audience=<JWT_AUDIENCE>
Jwt__Key=<JWT_SECRET_KEY>

Services__OrdersBaseUrl=<ORDERS_API_URL>

Cors__AllowedOrigins__0=<FRONTEND_URL>
```

---

## Frontend

```env
VITE_AUTH_API_URL=<AUTH_API_URL>
VITE_CATALOG_API_URL=<CATALOG_API_URL>
VITE_BASKET_API_URL=<BASKET_API_URL>
VITE_ORDERS_API_URL=<ORDERS_API_URL>
VITE_TICKETS_API_URL=<TICKETS_API_URL>
```

---

# 🐳 Docker

Los servicios ASP.NET Core utilizan Docker.

Ejemplo:

```bash
docker build -t eshop-orders-api .
```

Los contenedores utilizan:

```text
mcr.microsoft.com/dotnet/aspnet:9.0
mcr.microsoft.com/dotnet/sdk:9.0
```

Puerto:

```text
8080
```

---

# 🧪 Ejecución local

## Infraestructura

Levantar PostgreSQL y Redis:

```bash
docker compose up -d
```

---

## Auth.API

```bash
dotnet run --project Services/Auth/Auth.API/Auth.API.csproj
```

---

## Catalog.API

```bash
dotnet run --project Services/Catalog/Catalog.API/Catalog.API.csproj
```

---

## Basket.API

```bash
dotnet run --project Services/Basket/Basket.API/Basket.API.csproj
```

---

## Orders.API

```bash
dotnet run --project Services/Orders/Orders.API/Orders.API.csproj
```

Puerto local configurado:

```text
http://localhost:5300
```

---

## Tickets.API

```bash
dotnet run --project Services/Tickets/Tickets.API/Tickets.API.csproj
```

Puerto local configurado:

```text
http://localhost:5400
```

---

## Frontend

```bash
cd src/Frontend/eshop-web
npm install
npm run dev
```

Vite:

```text
http://localhost:5173
```

---

# ❤️ Health Checks

Orders:

```http
GET /health
```

Tickets:

```http
GET /health
```

Los demás servicios también exponen mecanismos de verificación según su configuración.

---

# 📚 Swagger / OpenAPI

Los microservicios ASP.NET Core exponen Swagger/OpenAPI en ambiente de desarrollo.

Ejemplo:

```text
http://localhost:5300/swagger
```

```text
http://localhost:5400/swagger
```

---

# ❌ Manejo de errores

El sistema maneja respuestas HTTP controladas.

| Código | Descripción |
|---|---|
| 400 | Solicitud inválida |
| 401 | Usuario no autenticado |
| 403 | Usuario sin autorización |
| 404 | Recurso no encontrado |
| 409 | Conflicto o transición inválida |
| 500 | Error interno controlado |

No se exponen:

- Connection Strings.
- JWT Keys.
- Stack traces.
- Credenciales.
- Información sensible.

---

# 🧪 Casos de prueba principales

## Crear orden válida

```text
Resultado esperado:
201 Created
```

---

## Basket vacío

```text
Resultado esperado:
400 Bad Request
```

---

## Idempotencia

Enviar dos veces:

```text
mismo CustomerId
+
misma Idempotency-Key
```

Resultado:

```text
solo se crea una orden
```

---

## Cambiar estado

```text
Pending → Confirmed
```

Resultado:

```text
200 OK
```

---

## Transición inválida

```text
Cancelled → Confirmed
```

Resultado:

```text
409 Conflict
```

---

## Ticket

```http
GET /api/tickets/orders/{orderId}
```

Resultado:

```text
200 OK
application/pdf
```

---

# 🔒 Seguridad

El proyecto implementa:

- JWT Authentication.
- Autorización basada en roles.
- Restricción de órdenes por cliente.
- Administración exclusiva para rol `Admin`.
- Variables de entorno para secretos.
- Connection Strings fuera del repositorio.
- Idempotencia.
- Validaciones de entrada.
- Manejo seguro de errores.

---

# 📂 Estructura principal

```text
src/
│
├── Frontend/
│   └── eshop-web/
│       └── src/
│           ├── components/
│           ├── features/
│           │   ├── auth/
│           │   ├── basket/
│           │   ├── catalog/
│           │   └── orders/
│           └── config/
│
└── eshop-services/
    │
    ├── Services/
    │   ├── Auth/
    │   │   └── Auth.API/
    │   │
    │   ├── Catalog/
    │   │   └── Catalog.API/
    │   │
    │   ├── Basket/
    │   │   └── Basket.API/
    │   │
    │   ├── Orders/
    │   │   └── Orders.API/
    │   │
    │   └── Tickets/
    │       └── Tickets.API/
    │
    └── eshop-services.slnx
```

---

# 🛠️ Tecnologías utilizadas

### Backend

- .NET 9
- ASP.NET Core
- Minimal APIs
- JWT Bearer Authentication
- Marten
- MongoDB.Driver
- HttpClient
- Swagger / OpenAPI
- Docker

### Frontend

- React
- Vite
- JavaScript
- CSS
- Fetch API

### Datos

- PostgreSQL
- Redis
- MongoDB Atlas

### Cloud

- Render
- Railway
- MongoDB Atlas
- Netlify
- GitHub

---

# 🎯 Flujo general del sistema

```text
Usuario
   ↓
React
   ↓
Auth
   ↓
Catálogo
   ↓
Basket
   ↓
Realizar compra
   ↓
Orders.API
   ├── valida Basket
   ├── valida Catalog
   ├── calcula totales
   ├── genera folio
   └── persiste orden
            ↓
       MongoDB Atlas
            ↓
       Mis órdenes
            ↓
       Ver detalle
            ↓
       Tickets.API
            ↓
        Ticket PDF
```

---

# 👨‍💻 Autor

Proyecto desarrollado como parte de la evaluación de integración de microservicios.

**Universidad Tecnológica de Tula-Tepeji**

Carrera:

```text
Tecnologías de la Información
```

Proyecto:

```text
Implementación e Integración de Microservicios E-Commerce
```

---

# 📄 Licencia

Proyecto desarrollado con fines académicos y educativos.
