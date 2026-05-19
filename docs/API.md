# HubPay API documentation

Interactive API reference is generated from the OpenAPI specification at runtime.

| UI | URL (local dev) |
|----|-----------------|
| **ReDoc** (recommended) | https://localhost:7239/redoc |
| Short link | https://localhost:7239/docs |
| Swagger UI | https://localhost:7239/swagger |
| OpenAPI JSON | https://localhost:7239/openapi/v1/swagger.json |

## Enable in other environments

Set in configuration:

```json
"HubPay": {
  "EnableSwagger": true
}
```

By default, documentation is enabled in **Development** only.

## Authentication flow

1. `POST /api/v1/auth/token` with `{ "merchantId": "...", "role": "merchant" }` or `"admin"`.
2. Use `Authorization: Bearer {accessToken}` on protected routes.
3. For `POST /api/v1/payments`, also send header `X-Idempotency-Key`.

See ReDoc for full request/response schemas, examples, and error codes.
