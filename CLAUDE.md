# ContactInfo — CLAUDE.md

A .NET 9 Blazor Server application that looks up contact information (emails, phone numbers) for a LinkedIn profile by querying Apollo, RocketReach, and SignalHire in parallel. Results are merged and ranked, with contacts found on multiple sources highlighted in green.

---

## Commands

```bash
# From repo root
dotnet restore ContactInfo/ContactInfo.csproj
dotnet build   ContactInfo/ContactInfo.csproj
dotnet run --project ContactInfo/ContactInfo.csproj
```

---

## Architecture

Single-project Blazor Server app. All code lives in `ContactInfo/`.

```
Models/
  AppSettings.cs          — API keys config (bound from appsettings.json; fallback only)
  BatchJob.cs             — Batch state: list of BatchJobRow + ordered SourceNames
  BatchJobRow.cs          — One row: LinkedIn URL, profile fields, per-source results, ranked contacts
  RankedContact.cs        — Deduplicated contact: Value, Label, Score (# sources), FoundOnSources
  SourceResult.cs         — Raw result from one provider: Emails/Phones as List<LabeledValue>
                            Also defines: LabeledValue(string Value, string? Label)

Services/Interfaces/
  IContactSource.cs       — Provider interface: string Name + LookupAsync(linkedInUrl)
  IUserSettingsService.cs — API key + settings access; Save(keys, flags, webhook urls)
  IExcelService.cs        — ReadLinkedInUrls, ExportBatch, UpdateSourceFile, GenerateSampleImport

Services/
  ApolloService.cs        — Apollo.io People Enrichment (POST /api/v1/people/match)
  RocketReachService.cs   — RocketReach v2 (GET /api/v2/person/lookup; polls until complete;
                            retries on 429 with 10s/20s backoff)
  SignalHireService.cs    — SignalHire v1 (POST submit + poll requestId; callback via webhook relay;
                            scheme-insensitive URL matching)
  DemoContactSource.cs    — Deterministic fake data; two instances (DemoA/DemoB); 8 scenarios
                            covering every result combination (see file docstring)
  ContactRankerService.cs — Groups LabeledValues by Value (case-insensitive, label ignored),
                            counts distinct sources → Score; picks first non-null Label
  BatchProcessorService.cs— Processes URLs one at a time; all sources in parallel per row
  UserSettingsService.cs  — Singleton; DPAPI-encrypts API keys at rest;
                            reads/writes %APPDATA%\ContactInfo\user-settings.json
  ExcelService.cs         — ClosedXML import/export; finds LinkedIn column by header name;
                            UpdateSourceFile writes ranked results back into a copy of the source file

Components/Pages/
  Lookup.razor            — Single URL lookup + batch import/export UI
  Settings.razor          — API key management, provider toggles, webhook relay config + expiry warning

Components/Layout/
  NavMenu.razor           — Lookup + Settings nav links
```

---

## Key Design Points

**Lookup flow:**
1. User enters LinkedIn URL → all enabled `IContactSource` instances called in parallel
2. Each returns a `SourceResult` with `List<LabeledValue>` for emails and phones
3. `ContactRankerService.Rank()` groups by value (case-insensitive), counts distinct sources
4. `RankedContact.Score > 1` → `IsMultiSource = true` → green highlight in UI and Excel

**Provider: RocketReach (v2)**
- GET `https://api.rocketreach.co/api/v2/person/lookup?linkedin_url=...`
- Header: `Api-Key: YOUR_KEY`
- May return `status: "searching"` — polls `?id={id}` until "complete" or "failed"
- Phone `type` field used as label (e.g. "direct", "mobile")
- Retries 429 up to 3 times with 10s/20s delays

**Provider: SignalHire (v1)**
- Step 1 — POST `https://www.signalhire.com/api/v1/candidate/search`
  - Header: `apikey: YOUR_KEY`; Body: `{ "items": ["linkedin_url"] }`
  - Response 202: `{ "requestId": "..." }`
- Step 2 — Poll webhook relay for the result (not the SignalHire API)
- Contact `subType` field used as label (e.g. "work", "personal", "mobile")
- URL matching strips scheme before comparison to avoid `https://` vs bare-URL mismatches

**Provider: Apollo.io**
- POST `https://api.apollo.io/api/v1/people/match?linkedin_url=...`
- Header: `x-api-key: YOUR_KEY`
- Returns emails (contact_emails array + person.email fallback); no label on phones

**LabeledValue and labels**
- `LabeledValue(string Value, string? Label)` — record in `SourceResult.cs`
- Providers pass their type/subType field as `Label`; null if provider doesn't supply one
- `ContactRankerService` picks the first non-null label when merging duplicate values
- Matching/deduplication uses `Value` only — label is never part of the comparison key
- UI shows label as a badge; Excel formats as `"value (label)"`

**Demo mode (DemoContactSource)**
- Two registered instances: DemoA (variant 1) and DemoB (variant 2)
- 8 scenarios selected by `hash % 8`; append `-sN` to username to force scenario N
- Scenarios cover: full multi-source, email-only multi-source, phone-only multi-source,
  all single-source, email-only, phone-only, no results, multiple contacts single source
- Sample import file uses `-s0` through `-s7` suffixes to guarantee all scenarios are shown

**Excel: Import**
- Scans row 1 for a header containing "LinkedIn" (case-insensitive); falls back to column A

**Excel: Update Source File**
- Opens a copy of the originally uploaded file (bytes kept in memory)
- Finds "Email Address(es)" and "Phone #s" columns by header; updates matching rows by LinkedIn URL
- Downloads the modified copy — original file is never touched

**Webhook relay (shared)**
- `WebhookCallbackUrl` / `WebhookRelayPollUrl` in `IUserSettingsService` — not provider-specific
- Settings page auto-derives poll URL from webhook.site callback URL
- Settings page checks relay on load and shows an expiry warning on 404/unreachable
- Service fast-fails on 404 rather than waiting for timeout

**Security**
- API keys encrypted at rest with Windows DPAPI (`DataProtectionScope.CurrentUser`)
- `Unprotect()` catches exceptions and returns value as-is → silent migration from plaintext
- `[assembly: SupportedOSPlatform("windows")]` in `Program.cs` suppresses CA1416 warnings

**Settings persistence**
- `%APPDATA%\ContactInfo\user-settings.json` — API keys (DPAPI-encrypted), toggles, webhook URLs
- `appsettings.json` — fallback only (empty keys by default)
- `appsettings.Production.json` — fixes Kestrel on HTTP port 5100

## Adding a New Provider

1. Implement `IContactSource` in `Services/YourProviderService.cs`
2. Register in `Program.cs` with `AddHttpClient<IContactSource, YourProviderService>()`
3. Add API key + enabled flag to `AppSettings.cs`, `IUserSettingsService`, `UserSettingsService`
4. Add Settings UI section in `Components/Pages/Settings.razor`

If the provider uses a callback, read `WebhookCallbackUrl`/`WebhookRelayPollUrl` from `IUserSettingsService` — do not add new URL fields.
