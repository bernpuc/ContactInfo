# ContactInfo

A .NET 9 Blazor Server application that looks up contact information (emails, phone numbers) for a LinkedIn profile by querying multiple providers in parallel. Results are merged and ranked, with contacts found across multiple sources highlighted.

## Features

- Single URL lookup and batch Excel import
- Parallel queries across all enabled providers
- Deduplicates and ranks results — contacts found on multiple sources highlighted in green
- Contact type labels (work, personal, mobile, direct) shown as badges where providers supply them
- Export results to a new Excel file, or write them back into a copy of the source file
- Per-source error reporting inline in results table
- Demo mode covering all result combinations (multi-source match, email-only, phone-only, no results, etc.)
- Per-provider enable/disable toggle in Settings

## Supported Providers

| Provider | Lookup Method | API Docs |
|---|---|---|
| [Apollo.io](https://www.apollo.io) | Direct response | [docs.apollo.io](https://docs.apollo.io/reference/people-enrichment) |
| [RocketReach](https://rocketreach.co) | GET lookup, polls until complete | [docs.rocketreach.co](https://docs.rocketreach.co/reference/people-lookup-api) |
| [SignalHire](https://www.signalhire.com) | Callback via webhook relay | [signalhire.com/profile#api](https://www.signalhire.com/profile#api) |

All providers identify contacts by LinkedIn profile URL, which uniquely identifies a person. Name-based search is not supported.

> **Free tier note:** Trial and free accounts typically have low quotas (e.g. RocketReach free gives ~5 lookups). The app retries automatically on rate limit errors with backoff, but a depleted quota requires waiting for the billing period to reset.

## Installation

### Windows Installer (recommended for end users)

A self-contained Windows installer is provided — no .NET installation required on the target machine.

**[⬇ Download the latest installer](https://github.com/bernpuc/ContactInfo/releases/latest)** from the Releases page.

**To build the installer yourself instead:**
1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php) (free)
2. Run `.\installer\build.ps1` from the repo root in PowerShell
3. Distribute `installer\Output\ContactInfoSetup.exe`

The installer creates Start Menu and optional desktop shortcuts. When launched, the app opens automatically in the default browser at `http://localhost:5100`. A console window shows the URL and instructions — press **Ctrl+C** there to shut the app down.

### Cutting a release (maintainers)

1. Bump `<Version>` in `ContactInfo/ContactInfo.csproj`, commit and push
2. Run `.\installer\release.ps1 -Notes "..."` from the repo root in PowerShell — builds the installer, tags `vX.Y.Z`, pushes the tag, and publishes a GitHub release with both `ContactInfoSetup.exe` and `GETTING-STARTED.pdf` attached

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) and an authenticated [GitHub CLI](https://cli.github.com/) (`gh`).

### Running from source (developers)

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9).

```bash
dotnet restore ContactInfo/ContactInfo.csproj
dotnet run --project ContactInfo/ContactInfo.csproj
```

The console will print the URL once the server is ready. Open it in your browser.

Navigate to **Settings** to enter your API keys, configure which providers are enabled, and set up the webhook relay if using SignalHire.

## Configuration

All settings are stored in `%APPDATA%\ContactInfo\user-settings.json` and managed through the **Settings** page in the app. This file is never committed to source control.

**API keys are encrypted at rest** using Windows DPAPI (user-scoped). The encrypted values are only readable by the Windows user account that saved them. Existing plaintext settings files from earlier versions are migrated automatically on next save — no manual action required.

To pre-populate API keys via config (optional), add them to `appsettings.json` under `AppSettings`. Keys entered this way are used as a fallback only and are not encrypted — prefer entering them through the Settings page:

```json
{
  "AppSettings": {
    "ApolloApiKey": "",
    "RocketReachApiKey": "",
    "SignalHireApiKey": ""
  }
}
```

### SignalHire Webhook Relay

SignalHire delivers results by POSTing to a callback URL rather than returning them in the API response. The app polls a webhook relay to retrieve those results. Configure this in **Settings → Webhook Relay**:

- **Callback URL** — the URL SignalHire POSTs results to (e.g. a [webhook.site](https://webhook.site) URL)
- **Relay Poll URL** — only shown if Callback URL isn't a webhook.site URL; the URL the app polls to retrieve results from your own relay. Auto-derived and hidden when using webhook.site.

> **Note:** Free webhook.site URLs expire after 7 days and stop accepting requests after 100 hits. Update the Callback URL in Settings when this happens — the Settings page will show a warning if the relay is unreachable.
>
> **Privacy:** webhook.site's free tier requires no login — anyone holding the Callback URL can view everything relayed to it, including candidate PII from SignalHire callbacks. Treat it like an API key; never paste it into chat, tickets, or screenshots.

## Adding a New Provider

1. Implement `IContactSource` in `Services/YourProviderService.cs`
2. Register in `Program.cs`: `builder.Services.AddHttpClient<IContactSource, YourProviderService>();`
3. Add API key and enabled flag to `AppSettings.cs`, `IUserSettingsService`, and `UserSettingsService`
4. Add the UI section to `Components/Pages/Settings.razor`

If the provider uses a callback pattern, read the shared `WebhookCallbackUrl` / `WebhookRelayPollUrl` from `IUserSettingsService` rather than adding provider-specific URL fields.

See `ApolloService.cs`, `RocketReachService.cs`, or `SignalHireService.cs` as reference implementations.

## Changelog

### v1.3.0
- Sidebar: removed the default "About" link (pointed to ASP.NET Core docs, confusing for end users); added a client logo pinned to the bottom of the sidebar
- Settings: Relay Poll URL field now only shown when the Callback URL isn't webhook.site, instead of always displaying a disabled, redundant field
- Documented webhook.site's free-tier privacy limitations (no login, no access control beyond the URL itself, no audit trail)

### v1.2.0
- Contact type labels (work, personal, mobile, direct) shown as badges on emails and phones
  - RocketReach: phone `type` field; SignalHire: contact `subType` field
  - Labels are display-only — deduplication and multi-source matching use the value only
- Demo mode expanded to 8 scenarios covering every result combination; sample import file exercises all 8
- Excel import auto-detects LinkedIn column by header name (previously assumed column A)
- "Update Source File" button writes ranked results back into a copy of the uploaded source file

### v1.1.0
- API keys encrypted at rest using Windows DPAPI (user-scoped; existing plaintext files migrate automatically on next save)
- RocketReach API updated to current GET endpoint (`/api/v2/person/lookup`)
- Rate limit retry with backoff for RocketReach (10s / 20s)
- Webhook relay refactored to shared settings — any callback-based provider can reuse it
- Settings page warns when webhook relay URL has expired or is unreachable
- SignalHire URL matching fixed (scheme-insensitive comparison)
- Windows installer (`ContactInfoSetup.exe`) — self-contained, no .NET required on target machine
- Console window shows URL and Ctrl+C shutdown instructions on startup

### v1.0.0
- Initial release: Apollo.io, RocketReach, SignalHire lookup in parallel
- Single URL and batch Excel import/export
- Per-provider enable/disable, demo mode
- SignalHire webhook relay via webhook.site

## Project Structure

```
Models/
  AppSettings.cs            — API keys config (appsettings.json fallback)
  BatchJob.cs / BatchRow.cs — Batch processing state
  RankedContact.cs          — Merged contact with source flags
  SourceResult.cs           — Raw result from a single provider

Services/Interfaces/
  IContactSource.cs         — Provider interface (Name + LookupAsync)
  IUserSettingsService.cs   — Settings access interface
  IExcelService.cs          — Excel import/export interface

Services/
  ApolloService.cs          — Apollo.io People Enrichment API
  RocketReachService.cs     — RocketReach v2 API (polls until complete)
  SignalHireService.cs      — SignalHire v1 API (submit + poll)
  DemoContactSource.cs      — Deterministic fake data for UI testing
  ContactRankerService.cs   — Deduplicates and ranks merged results
  BatchProcessorService.cs  — Processes rows one at a time, all sources in parallel
  UserSettingsService.cs    — Persists settings to %APPDATA%\ContactInfo\
  ExcelService.cs           — ClosedXML import/export

Components/Pages/
  Lookup.razor              — Single lookup + batch import UI
  Settings.razor            — API key management and provider toggles
```
