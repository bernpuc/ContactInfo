# Getting Started with ContactInfo

ContactInfo looks up email addresses and phone numbers for people by their LinkedIn profile URL. You enter a URL (or upload a list), and the app queries your enabled providers simultaneously and merges the results.

---

## 1. Install the App

Run `ContactInfoSetup.exe` and follow the prompts. The installer places the app in your Program Files folder and creates a Start Menu shortcut. No other software needs to be installed.

When you launch ContactInfo, a small console window opens — this is normal. It shows the app is running and reminds you to press **Ctrl+C** to shut it down. Your browser should open automatically to the app.

---

## 2. Get Your API Keys

ContactInfo relies on third-party services to find contact information. You need an account with at least one of them:

| Provider | Free tier | Where to find your key |
|---|---|---|
| **Apollo.io** | 100 credits/month (10,000 with a business email) | apollo.io → Settings → Integrations → API |
| **RocketReach** | ~5 lookups on trial | rocketreach.co/api → your account |
| **SignalHire** | Varies by plan | signalhire.com/profile#api |

You only need one provider to get results, but enabling multiple gives you more coverage since not every provider has every person in their database.

---

## 3. Enter Your Keys in Settings

Click **Settings** in the left navigation.

- Paste each API key into its field
- Make sure the toggle next to each key is switched **on**
- Click **Save**

If you're not using a provider, switch its toggle off — it won't be queried.

---

## 4. Set Up SignalHire's Webhook Relay (if using SignalHire)

SignalHire works differently from the other providers — instead of returning results directly, it sends them to a callback URL. You need a free relay service to receive them.

1. Go to **https://webhook.site** in your browser
2. Copy the unique URL it gives you (looks like `https://webhook.site/xxxxxxxx-...`)
3. In ContactInfo Settings, scroll to **Webhook Relay**
4. Paste the URL into the **Callback URL** field — the app derives the poll URL it needs automatically
5. Click **Save**

> The free webhook.site URL expires after 7 days. When it does, the Settings page will show a warning. Just get a fresh URL from webhook.site and paste it in again.
>
> **Privacy:** webhook.site's free tier requires no login — anyone holding this URL can view everything relayed to it, including candidate PII. Treat it like an API key; never paste it into chat, tickets, or screenshots.

If you're not using SignalHire, skip this step entirely.

---

## 5. Do Your First Lookup

Click **Lookup** in the left navigation.

Paste a LinkedIn profile URL into the search box — for example:
```
https://www.linkedin.com/in/username
```

Click **Look Up**. The app queries all enabled providers at the same time. Results appear within a few seconds (SignalHire may take up to a minute as it processes the request asynchronously).

The results table shows:
- **Name, title, and company** from the first provider that returned them
- **Emails and phone numbers** deduplicated across all providers
- Contacts found by more than one provider are **highlighted in green** — these are the most reliable

---

## 6. Batch Processing

To look up multiple people at once, click **Lookup** and use the **Import Excel** button to upload a spreadsheet. The file needs a column with LinkedIn URLs — the app automatically finds it by looking for a column header that contains "LinkedIn".

The app processes each row one at a time and updates the table as results come in. When finished you have two download options:

- **Export Excel** — downloads a new file with all results
- **Update Source File** — downloads a copy of the file you uploaded, with the ranked emails written into any "Email Address(es)" column and ranked phones into any "Phone #s" column. All other content in the file is preserved unchanged.

---

## 7. Demo Mode

If you want to explore the app without consuming API credits, go to **Settings**, enable **Demo mode**, and save. The app will return realistic-looking fake data so you can try the lookup and batch features freely. Turn demo mode off when you're ready to run real lookups.

---

## Uninstalling

Uninstall ContactInfo from **Windows Settings → Apps** or **Control Panel → Programs** as you would any other application.

The uninstaller removes the program files but leaves your settings intact. Your API keys and configuration are stored separately in `%APPDATA%\ContactInfo\` and are not touched by the uninstaller. This means if you reinstall later, your keys will still be there.

If you want a clean removal with no trace left behind, delete that folder manually after uninstalling:

1. Press **Win + R**, type `%APPDATA%\ContactInfo`, and press Enter
2. Delete the folder

---

## Troubleshooting

**No results from a provider**
Check that the provider is enabled in Settings and your API key is correct. Each provider shows its own status in the results — hover over an error icon for details.

**SignalHire times out**
Check the Settings page for a webhook relay warning. If the relay URL has expired, get a new one from webhook.site and save it. Also make sure the Callback URL you entered in your SignalHire account matches what's in Settings.

**RocketReach rate limit exceeded**
Free and trial accounts have very limited quotas. The app retries automatically, but if the quota for the billing period is exhausted you'll need to wait for it to reset or upgrade your plan.

**The app doesn't open in the browser**
Navigate manually to **http://localhost:5100** in your browser.

**Shutting down**
Press **Ctrl+C** in the console window, or close the console window directly.
