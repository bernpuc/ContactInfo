# ContactInfo — CLAUDE.md

A .NET 9 Blazor Server application that looks up contact information (emails, phone numbers) for a LinkedIn profile by querying RocketReach and SignalHire in parallel. Results are merged and ranked, with contacts found on both sources highlighted.

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
  ContactEntry.cs         — Single email or phone with source flags (RocketReach | SignalHire)
  LookupResult.cs         — Merged result: profile info + deduplicated emails + phones

Services/Interfaces/
  IRocketReachService.cs  — LookupAsync(linkedInUrl)
  ISignalHireService.cs   — LookupAsync(linkedInUrl)
  IUserSettingsService.cs — API key storage

Services/
  RocketReachService.cs   — HTTP client for RocketReach API v2; polls until "complete"
  SignalHireService.cs    — HTTP client for SignalHire API v1; submits then polls requestId
  UserSettingsService.cs  — Singleton; reads/writes user-settings.json in %APPDATA%\ContactInfo\
  ContactMergerService.cs — Static merge helper: deduplicates by value, flags FoundOnBoth, sorts

Components/Pages/
  Lookup.razor            — Main UI: LinkedIn URL input, results tables (emails + phones)
  Settings.razor          — API key editor for both services

Components/Layout/
  NavMenu.razor           — Lookup + Settings nav links
```

---

## Key Design Points

**Lookup flow:**
1. User enters LinkedIn profile URL → clicks Look Up
2. `RocketReachService` and `SignalHireService` are called in parallel via `Task.WhenAll`
3. `ContactMergerService.Merge()` deduplicates by normalised value, merges source flags
4. Results sorted: found-on-both first (green rows), then by source

**RocketReach API (v2):**
- POST `https://api.rocketreach.co/v2/api/lookupProfile`
- Header: `Api-Key: YOUR_KEY`
- Body: `{ "linkedin_url": "..." }`
- May return `status: "searching"` — polls with `{ "id": result.id }` until "complete" or "failed"
- Returns: emails (with smtp_valid + type), phones (with type), name, title, employer

**SignalHire API (v1):**
- Step 1 — POST `https://www.signalhire.com/api/v1/candidate/search`
  - Header: `apikey: YOUR_KEY`
  - Body: `{ "items": ["linkedin_url"] }`
  - Response 202: `{ "requestId": "..." }`
- Step 2 — Poll GET `https://www.signalhire.com/api/v1/request/{requestId}`
  - Until status is "done", "notFound", or "error"
  - Returns: candidates[].contacts[] with type ("email"/"phone"), value, label

**ContactEntry.Sources** is a `[Flags]` enum — a single entry can have `RocketReach | SignalHire`.
`FoundOnBoth` is `true` when both flags are set → triggers green row highlight.

**Settings persistence:**
- `user-settings.json` — API keys; stored in `%APPDATA%\ContactInfo\`
- `appsettings.json` — fallback only (empty keys by default)
