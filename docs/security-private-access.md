# Keeping KaraokeList private (friends only)

This app is not meant to be a public internet service. These layers reduce bots, scanners, and drive-by registration without changing how your friends use it day to day.

## What the app does now

| Control | Purpose |
|---------|---------|
| **Invite code** | Only people with the secret can register |
| **Registration can be closed** | After your group has accounts, shut the door completely |
| **Sign-in required** | All karaoke data pages need login |
| **Account lockout** | 5 failed passwords → 15 minute lockout |
| **Rate limits** | Caps login/register attempts per IP |
| **Honeypot field** | Hidden field bots often fill; humans never see it |
| **Strong passwords** | 12+ chars with mixed character types |
| **No public password reset** | Removes a common bot target (friends use Manage → Password when logged in) |
| **`robots.txt`** | Discourages polite crawlers from indexing |
| **Security headers** | HSTS, `X-Frame-Options`, `nosniff`, etc. |

## Recommended workflow

### 1. Before you deploy

Generate a long random invite code (password manager or `openssl rand -base64 32`).

### 2. Azure App Service settings (never commit the code)

| Setting | Value |
|---------|--------|
| `Security__Registration__InviteCode` | Your secret (32+ random characters) |
| `Security__Registration__RequireInviteCode` | `true` |
| `Security__Registration__AllowRegistration` | `true` until everyone has joined |
| `Security__Registration__AllowPasswordRecovery` | `false` |

Optional: restrict email domains if your friends all use the same provider:

```
Security__Registration__AllowedEmailDomains__0=gmail.com
Security__Registration__AllowedEmailDomains__1=outlook.com
```

### 3. Share with friends (private channel)

Send the site URL and invite code over text/DM — not in a public post.

Optional link that pre-fills the code:

```
https://<your-app>.azurewebsites.net/Account/Register?invite=<your-code>
```

### 4. After the last friend registers

In Azure → Configuration:

```
Security__Registration__AllowRegistration = false
```

Registration and the Register nav link disappear. Existing users keep signing in normally.

## Extra hardening (optional, Azure)

| Measure | Effort | Effect |
|---------|--------|--------|
| **Do not link the URL publicly** | Trivial | Security through obscurity helps against casual discovery |
| **App Service access restrictions** | Low | Allow only known IP ranges (works if friends are mostly on home Wi‑Fi; poor for mobile) |
| **Azure Front Door + WAF** | Higher cost | Bot rules, geo block, rate limits at the edge |
| **Custom domain + don’t publish DNS** | Low | Harder to guess than `*.azurewebsites.net` |
| **Microsoft social login only** | Medium | No password spray on local accounts (still need invite if registration stays open) |

## What this does *not* guarantee

- A determined attacker who obtains the URL can still try login spraying; lockout and rate limits slow that down.
- Invite codes in SMS/email can leak; rotate the code if you suspect that.
- There is no classified data in the catalog, but weak passwords on any internet-facing app are still worth avoiding — hence the 12-character policy.

## Local development

`appsettings.Development.json` disables the invite requirement so you can register without Azure secrets. Production always requires the invite code when `RequireInviteCode` is true.
