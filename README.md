# Businesstron

Businesstron finds newly registered Australian business names, enriches them from the
**ASIC** business-names registry and the **ABR**, filters out unsuitable leads by
keyword, optionally hunts down each lead's **website and contact email**, and pushes
the good ones into **Ontraport** — all from one web app.

It is a fork of **Asictron** (which does the ASIC/ABR/filter/Ontraport pipeline) with
an added, opt-in **web-enrichment stage** that ports the `ReverseWhois` and `AudaTron`
console tools into the pipeline.

It began as a console tool; it is now a **.NET 10 Clean Architecture** web app
(Jason Taylor layering) deployed as a single container with **.NET Aspire**,
**PostgreSQL**, a **React + Vite + Tailwind** SPA, and **Caddy** for TLS.

## The pipeline

```
New search ─▶ fetch source items ─▶ ASIC + ABR lookup per ABN ─▶ evaluate
suitability (keyword blacklist) ─▶ persist records
  │
  ├─ (opt-in) web enrichment: for suitable records renewing within 12 months,
  │     reverse-whois the ABN ─▶ domains ─▶ auda WHOIS per domain until an
  │     email is found ─▶ (stubbed) contact enrichment for phone / socials
  │
  └─▶ push suitable leads to Ontraport (tag / sequence) ─▶ CSV export
```

- **Source:** newly registered names from `data.gov.au` (by date range) **or** a pasted ABN list.
- **Filter:** a business name containing any active keyword (e.g. `gov`, `legal`) is flagged unsuitable and excluded from the push. Keywords are editable in the UI.
- **Web enrichment (opt-in):** toggle **Find websites & contacts** on a new search (or run it later on an existing run). For each suitable record whose ASIC renewal date is within 12 months, it reverse-whois-es the holder's ABN via **WhoisXML** to find domains, then looks each domain up on **auda** (reusing the 2Captcha solver) until one returns a contact email, trying the next domain when one has none. Off by default — each record spends WhoisXML + CAPTCHA credits. The final phone/socials step is a stubbed `IContactEnricher` seam (Google Places / AI website-scrape) to wire up later.
- **Ontraport:** suitable records are created as contacts and (optionally) tagged / subscribed to a sequence. When web enrichment is enabled, the auto-push is deferred until emails are populated so the discovered email is included. Runs automatically after a search when enabled, or on demand via **Push to Ontraport**.

## Solution layout

```
src/
  Businesstron.Domain           Entities, enums, the pure SuitabilityEvaluator
  Businesstron.Application       CQRS (MediatR), interfaces, DTOs, validators, behaviours
  Businesstron.Infrastructure    EF Core (PostgreSQL) + Identity, Hangfire jobs,
                             external gateways (ASIC / ABR / data.gov / Ontraport /
                             2Captcha / WhoisXML / auda), the WebEnrichmentService
  Businesstron.Web               Minimal-API endpoints + Program.cs; hosts the React SPA
    ClientApp/               React 19 + Vite + Tailwind (built into wwwroot on publish)
  Businesstron.AppHost           .NET Aspire orchestration (Postgres + server + Vite dev)
  Businesstron.ServiceDefaults   OpenTelemetry / health / resilience
tests/
  Businesstron.Domain.UnitTests  Suitability filter tests
```

The three original scrapers (`Asic.Client`, `Business.Client`, `Data.Client`) were
rewritten as clean Infrastructure gateways behind Application interfaces
(`IAsicRegistryClient`, `IAbrClient`, `IDataGovClient`), with an `ICaptchaSolver`
abstraction over 2Captcha. The exact ASIC request/parse flow was preserved.

## Run locally

**Prerequisites:** .NET 10 SDK, Node 22+, Docker Desktop (for the Postgres container).

```bash
# restore the EF tool once
dotnet tool restore

# run everything via Aspire (starts Postgres, the API, and the Vite dev server)
dotnet run --project src/Businesstron.AppHost
```

Open the Aspire dashboard (printed in the console), then open the **webfrontend**
URL. Sign in with the seeded admin:

- **Email:** `admin@businesstron.local`
- **Password:** `Businesstron!2026`

(Override via `SeedAdmin:Email` / `SeedAdmin:Password`.) The database is migrated
and seeded automatically on startup.

### Configuration & secrets

Ontraport (App ID / API Key), 2Captcha (API Key) and **WhoisXML** (reverse-WHOIS API
Key) credentials are entered from the **Settings** UI. They are written to a
`settings.overrides.json` layer that the app reloads live (`IOptionsMonitor`), so edits
take effect without a restart. Web enrichment needs both the WhoisXML key (reverse-whois)
and the 2Captcha key (auda uses its public reCAPTCHA site key). In
the deployed container that file lives on the persistent **`businesstron-data`** volume
(`Storage:OverridesPath=/data/settings.overrides.json`), so it survives redeploys.
Other operational settings (keyword blacklist, Ontraport tag/sequence, auto-push)
are edited in the UI too.

Only the Postgres password is still a deploy-time secret. For local dev, set it (and
optionally seed the integration keys / public host) via user-secrets on the **AppHost**:

```bash
cd src/Businesstron.AppHost
dotnet user-secrets set "Parameters:postgres-password" "<dev-password>"
dotnet user-secrets set "PublicHost"                   "businesstron.example.com"
```

Integration keys can be seeded the same way for local dev (`Ontraport__AppId`,
`Ontraport__ApiKey`, `TwoCaptcha__ApiKey` via `dotnet user-secrets` on **Businesstron.Web**),
but the UI is the source of truth once saved. Without a 2Captcha key, ASIC searches
that hit the CAPTCHA gate cannot complete; without Ontraport credentials, pushes are
skipped and reported as not-configured.

### Tests & migrations

```bash
dotnet test                                              # domain unit tests
dotnet ef migrations add <Name> --project src/Businesstron.Infrastructure \
  --startup-project src/Businesstron.Infrastructure --output-dir Data/Migrations
```

## Deploy

`.github/workflows/deploy.yml` (manual `workflow_dispatch`) does:

1. **Build & push** the single `businesstron-server` image to GHCR (the SPA is baked
   into `wwwroot` during publish).
2. **Generate** `docker-compose.yaml` via `aspire publish`.
3. **Deploy** over SSH: `docker compose pull && up -d`.

The server joins the shared external **`caddy`** Docker network and carries
`caddy` labels, so `caddy-docker-proxy` on the host routes
`https://<PublicHost>` → `businesstron-server:8080` with automatic TLS. If the host
doesn't already run it (Renewtron does), bootstrap once with
`deploy/caddy/docker-compose.yml`.

### Required GitHub settings

**Variables:** `SERVER_HOST`, `SERVER_USER`, `BUSINESSTRON_REMOTE_DIR`, `BUSINESSTRON_PUBLIC_HOST`
**Secrets:** `DEPLOY_SSH_KEY`, `POSTGRES_PASSWORD`

Ontraport and 2Captcha keys are **not** deploy secrets — enter them in the Settings
UI after the first deploy; they persist on the `businesstron-data` volume.

## Open items to confirm with the client

- **Ontraport field mapping.** ASIC records carry **no email** on their own; when web
  enrichment runs, the auda-discovered email is attached to the contact, otherwise
  contacts are created with company/name/address and ABN in a placeholder field.
  Confirm the real field mapping + target tag/sequence, and whether outreach is email
  or postal.
- **Contact enrichment provider.** The phone/socials step is a `NoOpContactEnricher`
  stub. Decide on Google Places / My Business vs an AI website-scrape (or both) and
  implement `IContactEnricher` — no pipeline changes needed, just swap the DI line.
- **Default keywords.** Seeded with `gov, government, council, legal, law, …` —
  adjust in **Keywords**.
- **Deploy host / domain.** Set `BUSINESSTRON_PUBLIC_HOST` and the SSH/deploy vars.
