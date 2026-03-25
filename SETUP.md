# Development Environment Setup

> **Just want to run the app?** Ask the repository owner for the `ContactInfoSetup.exe` installer. Run it, follow the prompts, and skip to [Step 9](#step-9--configure-api-keys). No programming tools required.

This guide walks you through setting up your Windows computer to **run and modify** the ContactInfo project from scratch. No prior experience is assumed.

---

## What You Will Install

| Tool | Purpose |
|---|---|
| Git | Download the code and track your changes |
| .NET 9 SDK | Build and run the application |
| Visual Studio Code | Edit the code |
| C# Dev Kit (VS Code extension) | C# language support inside VS Code |

---

## Step 1 — Install Git

Git lets you download the project from GitHub and save your changes over time.

1. Go to **https://git-scm.com/download/win**
2. Click the top download link (it will auto-detect 64-bit Windows)
3. Run the installer — accept all default options
4. When finished, open **Command Prompt** (press `Win + R`, type `cmd`, press Enter) and run:
   ```
   git --version
   ```
   You should see something like `git version 2.x.x`. If you do, Git is installed correctly.

---

## Step 2 — Install the .NET 9 SDK

The .NET SDK is what compiles and runs the application.

1. Go to **https://dotnet.microsoft.com/download/dotnet/9.0**
2. Under **.NET 9.0 SDK**, click the **Windows x64** download button
3. Run the installer — accept all defaults
4. Open a **new** Command Prompt window and run:
   ```
   dotnet --version
   ```
   You should see `9.0.x`. If you do, .NET is installed correctly.

> **Important:** Open a new Command Prompt after installing — existing windows won't pick up the new tools.

---

## Step 3 — Install Visual Studio Code

Visual Studio Code (VS Code) is a free code editor.

1. Go to **https://code.visualstudio.com/**
2. Click **Download for Windows**
3. Run the installer
4. On the "Select Additional Tasks" screen, check both:
   - **Add "Open with Code" action to Windows Explorer file context menu**
   - **Add to PATH**
5. Complete the installation and open VS Code

---

## Step 4 — Install the C# Dev Kit Extension

This adds C# language support, code suggestions, and error highlighting to VS Code.

1. Open VS Code
2. Click the **Extensions** icon in the left sidebar (it looks like four squares)
3. In the search box type: `C# Dev Kit`
4. Click **Install** on the result published by **Microsoft**

---

## Step 5 — Set Up GitHub Access

You need a GitHub account and permission to access the repository.

1. If you don't have a GitHub account, create one at **https://github.com**
2. Ask the repository owner to invite you as a collaborator on the **ContactInfo** repo
3. Accept the invitation email that GitHub sends you

---

## Step 6 — Clone the Repository

"Cloning" downloads a copy of the project to your computer.

1. Open Command Prompt
2. Navigate to the folder where you want to store the project. For example, to put it in your Documents folder:
   ```
   cd %USERPROFILE%\Documents
   ```
3. Run:
   ```
   git clone https://github.com/bernpuc/ContactInfo.git
   ```
4. This creates a `ContactInfo` folder. Navigate into it:
   ```
   cd ContactInfo
   ```

---

## Step 7 — Open the Project in VS Code

1. In Command Prompt, while inside the `ContactInfo` folder, run:
   ```
   code .
   ```
   This opens the entire project in VS Code.

2. VS Code may ask *"Do you trust the authors of the files in this folder?"* — click **Yes, I trust the authors**.

3. The C# Dev Kit may take a minute to load the project. You will see a progress indicator at the bottom of the window. Wait for it to finish.

---

## Step 8 — Run the Application

1. In VS Code, open the **Terminal** menu and click **New Terminal**
2. In the terminal that appears at the bottom, run:
   ```
   dotnet run --project ContactInfo/ContactInfo.csproj
   ```
3. Wait until the terminal shows:
   ```
     ContactInfo is running.
     URL: https://localhost:7035

     Press Ctrl+C to shut down.
   ```
4. Open your browser and go to the URL shown (e.g. `https://localhost:7035`)

   > Your browser may show a security warning about the certificate. This is normal for local development — click **Advanced** then **Proceed** (or equivalent in your browser).

5. You should see the ContactInfo application running. To stop it, press `Ctrl + C` in the terminal.

---

## Step 9 — Configure API Keys

The application needs API keys to look up real contact information.

1. In the browser, click **Settings** in the left navigation
2. Enter the API keys you have been provided for Apollo, RocketReach, and/or SignalHire
3. Toggle each provider **on** or **off** as needed
4. Click **Save**

Keys are stored on your computer at `%APPDATA%\ContactInfo\user-settings.json` and are never uploaded to GitHub.

---

## Step 10 — Set Up the Webhook Relay (SignalHire only)

SignalHire does not return results directly — it POSTs them to a callback URL. You need a webhook relay to receive those results.

1. Go to **https://webhook.site** in your browser
2. You will be given a unique URL, e.g. `https://webhook.site/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
3. Copy that URL
4. In the app, click **Settings** and scroll to **Webhook Relay**
5. Paste the URL into the **Callback URL** field — the **Relay Poll URL** will fill in automatically
6. Click **Save**

> **Important:** Free webhook.site URLs expire after **7 days**. When that happens, the Settings page will show a warning. Simply get a new URL from webhook.site, paste it into the Callback URL field, and save again.

If SignalHire is not enabled, you can skip this step.

---

## Making and Saving Changes

When you edit a file and want to save your changes to GitHub:

```bash
# See what files you changed
git status

# Stage all changed files
git add .

# Save a snapshot with a description of what you did
git commit -m "Describe your change here"

# Upload to GitHub
git push
```

To download the latest changes made by someone else:
```bash
git pull
```

---

## Troubleshooting

**`dotnet` is not recognized**
Close your Command Prompt and open a new one. The installer updates the PATH but existing windows don't see the change.

**Port already in use**
Another instance of the app may already be running. Press `Ctrl + C` in the terminal where it is running, then try again.

**Browser shows certificate error**
This is normal for local HTTPS in development. Click Advanced → Proceed to localhost.

**C# errors underlined in VS Code but the app runs fine**
Wait a moment — the C# Dev Kit sometimes takes 30–60 seconds to fully load a project after opening it.

**`git clone` asks for a username and password**
GitHub no longer accepts passwords for Git operations. You may need to set up a Personal Access Token. See **https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token** for instructions.
