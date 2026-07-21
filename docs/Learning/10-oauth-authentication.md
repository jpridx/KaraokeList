# 10 — OAuth Authentication

## Overview

Optional **Google** and **Microsoft** sign-in is hosted on the **API**, not inside the WASM origin. That avoids awkward cookie/CORS issues with external providers. After the identity provider redirects back, the API creates a **one-time exchange code** and sends the browser to the WASM `/auth/callback` page, which trades the code for the same JWT used by password login.

```text
WASM button → GET api/auth/external/{provider}
     → IdP challenge (Google OAuth / Microsoft OIDC)
     → GET api/auth/external/callback
     → one-time code
     → WASM /auth/callback?code=
     → POST api/auth/external/exchange → JWT
```

New OAuth users still pass the **invite / registration gate**. Existing password users can link a provider when email is verified.

## Major aspects

1. **API-hosted challenge** — redirect URIs are on the API (`/signin-google`, `/signin-microsoft`).
2. **Scheme split** — default authenticate/challenge is JwtBearer; external cookie is only for the mid-flow.
3. **Google via `AddGoogle`** — classic OAuth handler.
4. **Microsoft via OpenID Connect** — auth code + PKCE; avoid unnecessary Graph calls.
5. **Bridge code** — short-lived, one-time code replaces trying to set WASM cookies cross-site.
6. **Same JWT afterward** — OAuth is an alternate way to *obtain* the token, not a second auth stack in the UI.
7. **Config-gated UI** — buttons appear only when ClientId/Secret are configured (`GET external/providers`).

## Code samples

### Sample 1 — Register JWT default + Google / Microsoft external schemes

```31:85:KaraokeList.Api/ExternalAuthConfiguration.cs
        var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer()
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "KaraokeList.ExternalAuth";
                // ...
            });

        if (authSettings.Google.IsConfigured)
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authSettings.Google.ClientId;
                options.ClientSecret = authSettings.Google.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                // ...
            });
        }

        if (authSettings.Microsoft.IsConfigured)
        {
            authBuilder.AddOpenIdConnect(ExternalAuthProviders.MicrosoftScheme, options =>
            {
                options.Authority = "https://login.microsoftonline.com/common/v2.0";
                options.CallbackPath = "/signin-microsoft";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                // openid / profile / email scopes
            });
        }
```

### Sample 2 — Challenge, process login, issue one-time code, redirect to WASM

```505:547:KaraokeList.Api/Controllers/AuthController.cs
        var callbackUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", null, Request.Scheme, Request.Host.Value)!;
        var properties = signInManager.ConfigureExternalAuthenticationProperties(scheme, callbackUrl);
        properties.Items[ExternalAuthProviders.ReturnUrlItemKey] = SanitizeReturnUrl(returnUrl);
        // invite + rememberMe stored in auth properties
        return Challenge(properties, scheme);
        // ... in ExternalLoginCallback after ProcessExternalLoginAsync succeeds:
            var code = externalAuthCodeStore.CreateCode(processResult.User.Id, rememberMe);
            return Redirect(BuildExternalSuccessRedirect(code, returnUrl));
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Services/ExternalAuthService.cs` | ~42–71 | Find by login, link verified email, or create user via registration gate |
| `KaraokeList.Web/Pages/AuthCallback.razor` | full | Exchanges `?code=` for JWT and marks user authenticated |
| `docs/wasm-api-local-dev.md` | ~91–124 | Local redirect URI and user-secrets setup for Google/Microsoft |

## Exercises

1. **Multiple choice.** External login challenge is started against which host?
   - A) Only the WASM static host
   - B) The API host (`api/auth/external/{provider}`)
   - C) Azure SQL
   - D) MusicBrainz

2. **Fill in the blank.** After IdP callback success, the API creates a short-lived one-time ________ for the WASM app.

3. **Multiple choice.** Microsoft sign-in is configured with:
   - A) `AddGoogle` only
   - B) `AddOpenIdConnect` (auth code + PKCE)
   - C) Basic auth headers
   - D) Syncfusion license keys

4. **Fill in the blank.** WASM finishes OAuth at the route ________.

5. **Multiple choice.** Why use a one-time code instead of setting the JWT cookie on the API for WASM?
   - A) JWTs cannot be JSON
   - B) Cross-site cookie constraints make a code→exchange flow more reliable for a separate WASM origin
   - C) Google forbids JWTs
   - D) bUnit requires codes

6. **Fill in the blank.** External mid-flow uses Identity’s ________ scheme cookie.

7. **Multiple choice.** New OAuth users are still subject to:
   - A) Nothing — anyone can join
   - B) The registration / invite gate
   - C) MusicBrainz score > 90
   - D) Admin role assignment first

8. **Fill in the blank.** Provider buttons are shown when ________ returns configured providers.

9. **Multiple choice.** The end credential used by WASM after OAuth exchange is:
   - A) A permanent Google refresh token stored in LocalStorage
   - B) The same app JWT as password login
   - C) A SQL connection string
   - D) An unsigned email claim only

10. **Fill in the blank.** Google’s callback path on the API is typically ________.

## Answer key

1. B  
2. code (exchange code)  
3. B  
4. `/auth/callback`  
5. B  
6. External (`IdentityConstants.ExternalScheme`)  
7. B  
8. `GET api/auth/external/providers` (or external/providers)  
9. B  
10. `/signin-google`  
