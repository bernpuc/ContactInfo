# ContactInfo — Application Overview

2026-10-29 · Bernard Puc

## What ContactInfo Is

ContactInfo is an internal tool that finds a person's work email address and phone number starting from just their LinkedIn profile page. A staff member pastes in a LinkedIn URL — or uploads a spreadsheet of many at once — and the app checks two paid "people data" services (RocketReach and SignalHire) at the same time, then combines whatever each one finds into a single answer.

When two or more of those services independently agree on the same email or phone number, ContactInfo flags it in green as higher-confidence. This replaces the manual work of logging into each provider's website separately and cross-checking results by hand.

## How It Works

1. **Single lookup.** A user pastes one LinkedIn URL and clicks Look Up. All enabled providers are queried at once; results usually appear within a few seconds (SignalHire can take up to a minute, since it works asynchronously).
2. **Batch lookup.** A user uploads an Excel spreadsheet of LinkedIn URLs. The app works through the list one row at a time, updating the results table live, then offers two downloads: a new results file, or the original spreadsheet with the found emails and phone numbers written back into it.
3. **Confidence ranking.** Results found by only one provider are shown as-is. Results confirmed by two or more providers are highlighted in green as the most reliable.
4. **Demo mode.** Settings includes a toggle that returns realistic sample data instead of real lookups, so the app can be tried or demonstrated without spending paid provider credits.

## Current Deployment Model

ContactInfo is running as a **single-install-per-user pilot**: every participant installs their own private copy on their own Windows PC. There is no shared company server and nothing for IT to host or maintain centrally.

For IT, the practical implications are:

- The installer behaves like any ordinary desktop application — no administrator rights are required to install or run it.
- Each install is self-contained. One user's copy, settings, and API keys have no effect on anyone else's.
- Each user supplies their own paid API keys for the providers they want to use (RocketReach, SignalHire), so each person needs their own account with at least one of those services.
- Settings and keys are stored locally on that PC, under that Windows user's own profile, encrypted so they can't be read by other users of the same machine.

## Data, Privacy & Security

- **API keys** are encrypted at rest using Windows's built-in per-user encryption, so they can only be decrypted by the same Windows login that saved them.
- **Contact data itself** (the emails and phone numbers returned) comes from two external commercial data brokers — RocketReach and SignalHire. Each operates under its own privacy policy and data-sourcing practices, which are outside ContactInfo's control.
- **SignalHire's relay — a known, accepted limitation of the pilot.** SignalHire doesn't return results directly; it sends them to a callback address. Because each pilot install runs on an individual PC with no public internet address of its own, ContactInfo relays those callbacks through a free third-party service, webhook.site. ContactInfo users are instructed to treat that link like a password (never paste it into chat, a ticket, or a screenshot). This workaround would not be needed under a centrally hosted deployment, which could receive the callback directly and keep that data inside company infrastructure.
- ContactInfo does not retain candidate data on any central system — results exist only on the individual user's PC and in any file they choose to export.

## Costs & Licensing

The ContactInfo application itself is an internally built tool with no license cost. The real cost is the two underlying data providers, each sold on its own paid or credit-based plan.

Under the current pilot model, each participant holds their own account and key for the providers they use, so cost scales with the number of pilot users. If the tool is adopted more broadly, cost will scale differently depending on which deployment path is chosen — one key per user versus shared, centrally managed provider accounts.

## IT Support Notes

- **Install:** runs like a normal Windows application installer. No administrator rights needed; it installs to the user's own profile and adds a Start Menu shortcut.
- **First launch:** a small console (black text) window opens alongside the user's browser — this is expected. The app is a local web server; the console window is how it runs and is closed with Ctrl+C to shut the app down.
- **Where it runs:** the app opens in the user's default browser at `http://localhost:5100`, i.e. only on that machine, never over the network.
- **Uninstall:** standard removal via Windows Settings → Apps, or Control Panel → Programs. The uninstaller removes program files only; it leaves the user's settings and API keys in place (under `%APPDATA%\ContactInfo\`) in case they reinstall later. A fully clean removal means also deleting that folder by hand.
- **Common support issues:** a missing or incorrect API key for a provider; SignalHire's free relay link expiring roughly every 7 days (the app shows a warning in Settings when this happens — the user just needs a fresh link from webhook.site); RocketReach's trial/free tier has a very low lookup quota and can rate-limit.
- **Ongoing IT burden:** minimal during the pilot. Each install is independent, self-contained, and self-updating is not in place — there is no shared server or infrastructure to patch or monitor.
