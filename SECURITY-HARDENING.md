# ContactInfo — Security Hardening Plan

Steps required before deploying to an internal corporate server.

---

## Critical

### 1. Add Authentication
The app has zero auth — anyone on the network can access it and modify API keys.

- Add Windows Authentication (simplest for internal corporate):
  ```csharp
  services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
  services.AddAuthorization();
  ```
- Or add ASP.NET Core Identity with an internal user store.
- Apply `[Authorize]` to all pages; protect `/settings` most aggressively (only admins should touch API keys).

### 2. Secure API Key Storage
Keys are stored as plaintext in `%APPDATA%\ContactInfo\user-settings.json`.

- For server deployment: move keys to environment variables or a secrets manager (Windows Credential Manager, Azure Key Vault, or ASP.NET Core User Secrets → production env vars).
- Remove API keys from `appsettings.json` entirely; read via `Environment.GetEnvironmentVariable()` at startup.
- If file storage must remain, encrypt with DPAPI (`ProtectedData.Protect()`).

### 3. Enforce HTTPS
`Program.cs` has no `UseHttpsRedirection()` or HSTS. The HTTP launch profile is a live risk.

```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

---

## High

### 4. Server-Side Input Validation
Only browser-side `type="url"` validation exists on the LinkedIn field.

- Add a regex check in `RunSingleLookup()` and `ExcelService.ReadLinkedInUrls()` enforcing a `linkedin.com/in/` prefix.
- Validate uploaded file MIME type server-side (not just the HTML `accept` attribute).
- Add a server-side file size cap in addition to the browser-side 10 MB limit.

### 5. Sanitize Error Messages
`ApolloService.cs` returns raw `ex.Message` to the UI, which can leak internal API details or stack info.

- Log the full exception server-side with `ILogger`.
- Return only a generic message to the Blazor component.

### 6. Add Rate Limiting
No throttling exists; a single user could exhaust all API credits via batch upload.

- Add ASP.NET Core rate limiting middleware (`AddRateLimiter`) — fixed window or token bucket per IP/user.
- Consider a per-session cap on the number of lookups.

---

## Medium

### 7. Add Security Headers

- Content-Security-Policy (restrict scripts/frames)
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY

Can be added via middleware or IIS/reverse proxy config.

### 8. Structured Logging with Sensitive Data Filtering

- Use Serilog or the built-in logger with a destructuring policy that redacts API keys and full LinkedIn URLs from logs.
- Ship logs to a central sink (Event Log, file with rotation, or corporate SIEM).

---

## Deployment Steps

### 9. Publish Profile and IIS/Kestrel Config

- Create `Properties/PublishProfiles/Production.pubxml` targeting the server.
- If behind IIS: install the ASP.NET Core Hosting Bundle, configure the IIS site with an HTTPS binding and a valid cert (internal CA is fine).
- Set `ASPNETCORE_ENVIRONMENT=Production` so the dev exception page is disabled.
- Set API keys as environment variables on the server (IIS Application Pool environment variables or Windows Service env).

### 10. Principle of Least Privilege

- Run the app under a dedicated service account with no admin rights.
- Restrict `%APPDATA%` write access to that account only.
- Firewall the server to the internal network only.

---

## Low / Hardening

- Remove `"AllowedHosts": "*"` from `appsettings.json` and set it to your internal domain.
- Add a `.gitignore` entry for any file that could ever contain keys.
- Consider a Content Security Policy nonce for the inline scripts in `App.razor`.

---

## Implementation Order

| Priority | Step | Effort |
|---|---|---|
| 1 | Windows Auth + `[Authorize]` on all pages | ~2h |
| 2 | Move API keys to env vars, remove from file | ~1h |
| 3 | `UseHttpsRedirection` + HSTS + IIS HTTPS cert | ~1h |
| 4 | Server-side URL validation + file MIME check | ~1h |
| 5 | Sanitize exception messages, add `ILogger` | ~1h |
| 6 | Rate limiting middleware | ~1h |
| 7 | Security headers middleware | ~30m |
| 8 | Publish profile + production deployment doc | ~1h |
