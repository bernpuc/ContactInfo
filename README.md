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

| Provider | API Docs |
|---|---|
| [Apollo.io](https://www.apollo.io) | [docs.apollo.io](https://docs.apollo.io/reference/people-enrichment) |
| [RocketReach](https://rocketreach.co) | [rocketreach.co/api](https://rocketreach.co/api) |
| [SignalHire](https://www.signalhire.com) | [signalhire.com/profile#api](https://www.signalhire.com/profile#api) |

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)

## Getting Started

```bash
dotnet restore ContactInfo/ContactInfo.csproj
dotnet run --project ContactInfo/ContactInfo.csproj
```

Then open `https://localhost:7035` in your browser.

Navigate to **Settings** to enter your API keys and configure which providers are enabled.

## Configuration

API keys are stored in `%APPDATA%\ContactInfo\user-settings.json` and never committed to source control.

To pre-populate keys via environment/config, add them to `appsettings.json` under `AppSettings`:

```json
{
  "AppSettings": {
    "ApolloApiKey": "",
    "RocketReachApiKey": "",
    "SignalHireApiKey": ""
  }
}
```

## Adding a New Provider

1. Implement `IContactSource` in `Services/YourProviderService.cs`
2. Register in `Program.cs`: `builder.Services.AddHttpClient<IContactSource, YourProviderService>();`
3. Add API key and enabled flag to `AppSettings.cs`, `IUserSettingsService`, and `UserSettingsService`
4. Add the UI section to `Components/Pages/Settings.razor`

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
