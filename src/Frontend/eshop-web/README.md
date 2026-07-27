# eShop Web

Frontend React + Vite para eShop.

## Desarrollo Local

Crear `.env` local con:

```env
VITE_CATALOG_API_URL=http://localhost:6002
VITE_BASKET_API_URL=http://localhost:6001
VITE_AUTH_API_URL=http://localhost:6003
```

Comandos:

```powershell
npm run dev
npm run lint
npm run build
```

## Netlify

La configuracion esta en `../../../netlify.toml` desde esta carpeta, ubicado en la raiz del repositorio.

- Base directory: `src/Frontend/eshop-web`
- Build command: `npm run build`
- Publish directory: `dist`
- SPA fallback: `/index.html`

Variables requeridas en Netlify:

```text
VITE_AUTH_API_URL=https://auth-api.example.com
VITE_CATALOG_API_URL=https://catalog-api.example.com
VITE_BASKET_API_URL=https://basket-api.example.com
```

No guardar URLs internas de Railway, `REDIS_PUBLIC_URL` ni credenciales en el frontend.
