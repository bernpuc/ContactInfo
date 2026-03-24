# ContactInfo

A .NET 9 Blazor Server application that looks up contact information (emails, phone numbers) for a LinkedIn profile by querying multiple providers in parallel. Results are merged and ranked, with contacts found across multiple sources highlighted.

## Features

- Single URL lookup and batch Excel import
- Parallel queries across all enabled providers
- Deduplicates and ranks results — contacts found on multiple sources highlighted in green
- Per-source error reporting inline in results table
- Demo mode for UI testing without consuming API credits
- Per-provider enable/disable toggle in Settings

## Supported Providers

| Provider | Lookup Method | API Docs |
|---|---|---|
| [Apollo.io](https://www.apollo.io) | Direct response | [docs.apollo.io](https://docs.apollo.io/reference/people-enrichment) |
| [RocketReach](https://rocketreach.co) | Polls until complete | [rocketreach.co/api](https://rocketreach.co/api) |
| [SignalHire](https://www.signalhire.com) | Callback via webhook relay | [signalhire.com/profile#api](https://www.signalhire.com/profile#api) |

All providers identify contacts by LinkedIn profile URL, which uniquely identifies a person. Name-based search is not supported.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)

## Getting Started

```bash
dotnet restore ContactInfo/ContactInfo.csproj
dotnet run --project ContactInfo/ContactInfo.csproj
```

Then open `https://localhost:7035` in your browser.

Navigate to **Settings** to enter your API keys, configure which providers are enabled, and set up the webhook relay if using SignalHire.

## Configuration

All settings are stored in `%APPDATA%\ContactInfo\user-settings.json` and never committed to source control. They are managed through the **Settings** page in the app.

To pre-populate API keys via config (optional), add them to `appsettings.json` under `AppSettings`:

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
- **Relay Poll URL** — the URL the app polls to retrieve results; auto-derived when using webhook.site

> **Note:** Free webhook.site URLs expire after 7 days and stop accepting requests after 100 hits. Update the Callback URL in Settings when this happens — the Settings page will show a warning if the relay is unreachable.

## Adding a New Provider

1. Implement `IContactSource` in `Services/YourProviderService.cs`
2. Register in `Program.cs`: `builder.Services.AddHttpClient<IContactSource, YourProviderService>();`
3. Add API key and enabled flag to `AppSettings.cs`, `IUserSettingsService`, and `UserSettingsService`
4. Add the UI section to `Components/Pages/Settings.razor`

If the provider uses a callback pattern, read the shared `WebhookCallbackUrl` / `WebhookRelayPollUrl` from `IUserSettingsService` rather than adding provider-specific URL fields.

See `ApolloService.cs`, `RocketReachService.cs`, or `SignalHireService.cs` as reference implementations.

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
