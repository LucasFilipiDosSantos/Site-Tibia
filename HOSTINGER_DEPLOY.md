# Deploy na Hostinger

## Frontend em hospedagem compartilhada

O frontend esta pronto para deploy estatico na Hostinger.

Antes do build, edite:

```bash
FronEnd/.env.production
```

Troque:

```bash
VITE_API_BASE_URL=https://api.seudominio.com.br
VITE_APP_BASE_PATH=/
```

Use `VITE_APP_BASE_PATH=/` quando publicar direto em `public_html`.
Se publicar em uma subpasta, por exemplo `public_html/loja`, use `VITE_APP_BASE_PATH=/loja/` e ajuste tambem `FronEnd/public/.htaccess`:

```apache
RewriteBase /loja/
RewriteRule . /loja/index.html [L]
```

Depois rode:

```bash
cd FronEnd
npm install
npm run build
```

Envie todo o conteudo de `FronEnd/dist/` para `public_html`.

O arquivo `FronEnd/public/.htaccess` ja e copiado automaticamente para `dist/.htaccess` no build. Ele e necessario para abrir rotas internas do React direto pelo navegador sem erro 404.

## Backend/API

A hospedagem compartilhada da Hostinger deve receber apenas o frontend estatico. A API deste repositorio e ASP.NET/.NET com banco e precisa rodar em VPS, Docker, cloud, ou outro host compativel com .NET.

Quando a API estiver publicada, configure o dominio do frontend no CORS do backend:

```json
"Cors": {
  "AllowedOrigins": [
    "https://seudominio.com.br",
    "https://www.seudominio.com.br"
  ]
}
```

Ou por variaveis de ambiente no servidor da API:

```bash
Cors__AllowedOrigins__0=https://seudominio.com.br
Cors__AllowedOrigins__1=https://www.seudominio.com.br
```

