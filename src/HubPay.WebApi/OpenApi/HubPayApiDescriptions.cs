namespace HubPay.WebApi.OpenApi;

/// <summary>English OpenAPI copy for HubPay API documentation (Swagger / ReDoc).</summary>
internal static class HubPayApiDescriptions
{
    public const string ApiTitle = "HubPay API";
    public const string ApiVersion = "v1";

    public const string ApiDescription = """
        # HubPay — Pan-European Payment Hub

        HubPay orchestrates **11 European payment schemes** with inline ONNX anti-fraud scoring,
        PSP integrations (mTLS-ready), idempotent payment creation, and database-backed PSP configuration.

        ## Base URL
        All routes are relative to your deployment host, e.g. `https://localhost:7239`.

        ## Authentication
        Most endpoints require a **JWT Bearer** token.
        1. Call `POST /api/v1/auth/token` with `merchantId` (and optional `role`: `merchant` or `admin`).
        2. Send `Authorization: Bearer {accessToken}` on subsequent requests.

        | Role | Access |
        |------|--------|
        | `merchant` (default) | Payments, transactions, dashboard, refunds |
        | `admin` | PSP configuration admin (`/api/v1/admin/psp-config/*`) |

        ## Idempotency
        `POST /api/v1/payments` **requires** header `X-Idempotency-Key` (unique per logical payment attempt).
        Replays with the same key return the cached response without creating a duplicate transaction.

        ## Supported payment schemes (`paymentScheme`)
        | Value | Provider / notes |
        |-------|------------------|
        | `MBWAY` | SIBS — phone E.164 required |
        | `MULTIBANCO` | SIBS — reference entity returned |
        | `BIZUM` | Bizum — phone E.164 required |
        | `EURO6000` | Euro6000 |
        | `WERO` | Wero instant payments |
        | `CARTESBANCAIRES` | Cartes Bancaires (3DS) |
        | `IDEAL` | iDEAL — QR payload |
        | `BANCONTACT` | Bancontact |
        | `BANCOMATPAY` | Bancomat Pay — phone E.164 required |
        | `SWISH` | Swish |
        | `VIPPSMOBILEPAY` | Vipps MobilePay |

        Currency must be **`EUR`** only.

        ## PSP configuration keys (`scheme` in admin API)
        Provider rows use **provider scheme** identifiers (not payment scheme): `SIBS`, `BIZUM`, `WERO`,
        `CARTESBANCAIRES`, `IDEAL`, `BANCONTACT`, `EURO6000`, `BANCOMATPAY`, `SWISH`, `VIPPSMOBILEPAY`, `WEBHOOKS`.
        `SIBS` settings apply to both `MBWAY` and `MULTIBANCO`.

        ## Webhooks
        PSPs call `POST /api/v1/webhooks/{scheme}` with HMAC signature header (default: `X-HubPay-Signature`).
        `{scheme}` matches the payment scheme (e.g. `MBWAY`, `WERO`).

        ## Real-time updates
        SignalR hub: `/hubs/transactions` — transaction status broadcasts for the Blazor dashboard.

        ## Health
        - `GET /health` — all checks
        - `GET /health/ready` — readiness (PostgreSQL, Redis, ONNX model)
        """;

    public const string TagAuthentication = "Authentication";
    public const string TagAuthenticationDesc =
        "JWT token issuance for merchants and administrators (development and integration testing).";

    public const string TagPayments = "Payments";
    public const string TagPaymentsDesc =
        "Payment initiation, transaction listing, dashboard metrics, refunds, and PSP webhook ingress.";

    public const string TagPspAdmin = "PSP Admin";
    public const string TagPspAdminDesc =
        "CRUD for PSP provider and per-merchant configuration stored in PostgreSQL. Requires `admin` role. " +
        "Use `POST .../reload` to hot-reload settings without restarting the API.";

    public const string TagHealth = "Health";
    public const string TagHealthDesc = "Liveness and readiness probes for orchestrators (Kubernetes, Azure, etc.).";

    public const string AuthTokenSummary = "Issue JWT access token";
    public const string AuthTokenDescription = """
        Generates a signed JWT for the given `merchantId`.
        - Default role: `merchant` (payment API access).
        - Role `admin`: required for PSP configuration endpoints.
        **Production:** replace with your identity provider; this endpoint is intended for development and E2E tests.
        """;

    public const string CreatePaymentSummary = "Create payment";
    public const string CreatePaymentDescription = """
        Runs inline anti-fraud scoring, persists the transaction, and invokes the PSP strategy for `paymentScheme`.
        **Required header:** `X-Idempotency-Key` (string, unique, max 24h TTL in Redis).
        **Phone:** `phoneNumber` in E.164 format (`+351912345678`) when scheme is `MBWAY`, `BIZUM`, or `BANCOMATPAY`.
        **Responses:** `200` with `PaymentResponseDto`; `409` if idempotency key is in flight; `422` if anti-fraud blocks or PSP rejects.
        """;

    public const string GetTransactionsSummary = "List transactions";
    public const string GetTransactionsDescription =
        "Returns a paginated list of transactions ordered by creation time (newest first).";

    public const string DashboardStatsSummary = "Dashboard statistics";
    public const string DashboardStatsDescription =
        "Aggregated KPIs: volume, conversion, fraud blocks, TRA exemptions, Wero A2A vs card counts.";

    public const string AntiFraudDetailSummary = "Anti-fraud decision detail";
    public const string AntiFraudDetailDescription =
        "Returns feature vector inputs and ONNX score for a transaction. `404` if the transaction does not exist.";

    public const string RefundSummary = "Refund transaction";
    public const string RefundDescription = """
        Initiates a full or partial refund via the PSP strategy bound to the transaction.
        Query `amount` (optional): partial refund in EUR; omit for full refund.
        """;

    public const string WebhookSummary = "PSP webhook callback";
    public const string WebhookDescription = """
        Anonymous endpoint for PSP status notifications.
        Validates HMAC signature using scheme-specific secrets from `WEBHOOKS` configuration.
        **Path parameter `scheme`:** payment scheme (`MBWAY`, `WERO`, …) used to resolve the handler.
        """;

    public const string ListProvidersSummary = "List PSP providers";
    public const string ListProvidersDescription =
        "Returns all rows from `psp_provider_configurations` including `id` (GUID) and `scheme` (logical key).";

    public const string GetProviderSummary = "Get PSP provider by scheme";
    public const string GetProviderDescription =
        "Returns a single provider configuration. `scheme` examples: `SIBS`, `BIZUM`, `WEBHOOKS`.";

    public const string CreateProviderSummary = "Create PSP provider";
    public const string CreateProviderDescription =
        "Inserts a new provider. `scheme` must be unique. `settingsJson` is provider-specific JSON (camelCase).";

    public const string UpdateProviderSummary = "Update PSP provider";
    public const string UpdateProviderDescription =
        "Updates `isEnabled` and/or `settingsJson` for an existing provider identified by `scheme`.";

    public const string DeleteProviderSummary = "Delete PSP provider";
    public const string DeleteProviderDescription = "Removes the provider row. Returns `204 No Content`.";

    public const string ListMerchantsSummary = "List merchant overrides";
    public const string ListMerchantsDescription =
        "Lists per-merchant PSP settings. Optional query `scheme` filters by provider scheme (e.g. `SIBS`, `WERO`).";

    public const string GetMerchantSummary = "Get merchant override";
    public const string GetMerchantDescription =
        "Returns merchant-specific JSON (e.g. Multibanco entity for SIBS, Wero IBANs for WERO).";

    public const string UpsertMerchantSummary = "Create merchant override";
    public const string UpsertMerchantDescription =
        "Creates or replaces merchant configuration for `(scheme, merchantId)`.";

    public const string UpdateMerchantSummary = "Update merchant override";
    public const string UpdateMerchantDescription =
        "Same as create; `scheme` and `merchantId` in the body must match the route.";

    public const string DeleteMerchantSummary = "Delete merchant override";
    public const string DeleteMerchantDescription = "Removes the merchant configuration row.";

    public const string ReloadSummary = "Hot-reload PSP configuration";
    public const string ReloadDescription = """
        Reloads `HubPaySettings` from PostgreSQL into memory (all PSP strategies and webhook secrets).
        Does not restart the process. Returns counts of providers and merchants loaded.
        """;

    public const string HealthSummary = "Health check (all)";
    public const string HealthDescription = "Runs all registered health checks (PostgreSQL, Redis, ONNX model path).";

    public const string HealthReadySummary = "Readiness check";
    public const string HealthReadyDescription =
        "Checks tagged `ready` only — use for Kubernetes readiness probes before accepting traffic.";

    public static readonly string[] PaymentSchemes =
    [
        "MBWAY", "MULTIBANCO", "BIZUM", "EURO6000", "WERO", "CARTESBANCAIRES",
        "IDEAL", "BANCONTACT", "BANCOMATPAY", "SWISH", "VIPPSMOBILEPAY"
    ];

    public static readonly string[] ProviderSchemes =
    [
        "SIBS", "BIZUM", "WERO", "CARTESBANCAIRES", "IDEAL", "BANCONTACT",
        "EURO6000", "BANCOMATPAY", "SWISH", "VIPPSMOBILEPAY", "WEBHOOKS"
    ];
}
