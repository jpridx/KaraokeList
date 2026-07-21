# 09 — JWT Authentication

## Overview

KaraokeList uses **JSON Web Tokens** as the session credential for Blazor WASM. ASP.NET Core Identity stores users and roles in SQL; after login/register the API issues a signed JWT. The WASM app keeps the token in **localStorage**, reconstructs `AuthenticationState` from its claims, and sends `Authorization: Bearer` on API calls.

```text
Login/Register → JwtTokenService → AuthResponse.Token
       ↓
localStorage["authToken"]
       ↓
JwtAuthenticationStateProvider  +  AuthorizationMessageHandler
       ↓
API validates issuer/audience/lifetime/signature → [Authorize] controllers
```

## Major aspects

1. **Identity is source of truth** — users/roles live in the database; the JWT is a signed snapshot.
2. **HMAC-signed tokens** — symmetric key (`Jwt:Key`), validated issuer and audience.
3. **Claims payload** — user id, name, email, roles, custom `singer_id`.
4. **Remember me** — short default expiry vs extended days.
5. **WASM has no cookie session** — auth state is client-side JWT parsing.
6. **Role changes need re-login** — roles are baked into the token until the next sign-in.
7. **Hardening** — lockout, auth rate limits, invite gate around registration (see security docs).

## Code samples

### Sample 1 — Issue a signed JWT with roles and singer claim

```20:51:KaraokeList.Api/Services/JwtTokenService.cs
    public (string Token, DateTime ExpiresUtc) CreateToken(ApplicationUser user, IEnumerable<string> roles, bool rememberMe = false)
    {
        var expires = rememberMe
            ? DateTime.UtcNow.AddDays(_settings.ExtendedExpirationDays)
            : DateTime.UtcNow.AddHours(_settings.ExpirationHours);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        if (user.SingerId is int singerId)
        {
            claims.Add(new Claim(KaraokeClaimTypes.SingerId, singerId.ToString()));
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
```

### Sample 2 — WASM builds AuthenticationState from localStorage JWT

```15:40:KaraokeList.Web/Services/JwtAuthenticationStateProvider.cs
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        // ...
        var jwt = handler.ReadJwtToken(token.Trim('"'));
        if (jwt.ValidTo < DateTime.UtcNow)
        {
            await localStorage.RemoveItemAsync(TokenKey);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Program.cs` | ~103–116 | `TokenValidationParameters` for JwtBearer |
| `KaraokeList.Web/Services/AuthorizationMessageHandler.cs` | ~8–17 | Attach Bearer header to outbound HttpClient calls |
| `docs/admin-roles.md` | ~61–63 | Admin role is in JWT; grant/revoke applies on next sign-in |

## Exercises

1. **Multiple choice.** After password login, the WASM app stores the JWT in:
   - A) A server session cookie only
   - B) `localStorage` under a key such as `authToken`
   - C) Azure Key Vault
   - D) The service worker cache name

2. **Fill in the blank.** API endpoints protect resources with the ________ attribute (and role checks where needed).

3. **Multiple choice.** Signing algorithm used when creating tokens here is:
   - A) RS256 with a public certificate only
   - B) HMAC SHA-256 with a symmetric key
   - C) MD5
   - D) Plain Base64 without a signature

4. **Fill in the blank.** The custom claim linking a user to their singer row is ________.

5. **Multiple choice.** If an Admin role is granted in the database, the user’s JWT reflects it:
   - A) Instantly in all already-issued tokens
   - B) On the next sign-in (when a new token is issued)
   - C) Never
   - D) Only in Playwright

6. **Fill in the blank.** Expired tokens are cleared when ________ finds `ValidTo` in the past.

7. **Multiple choice.** JwtBearer validation should check:
   - A) Only the email claim
   - B) Issuer, audience, lifetime, and signing key
   - C) Only CORS headers
   - D) Only the MusicBrainz score

8. **Fill in the blank.** Remember-me extends lifetime using ________ExpirationDays (setting concept).

9. **Multiple choice.** Why is JWT a natural fit for Blazor WASM?
   - A) WASM cannot call HTTP
   - B) There is no trusted server-side page session; the client must send a bearer credential
   - C) SQL Server requires JWT
   - D) bUnit cannot mock cookies

10. **Fill in the blank.** Token creation is centralized in ________.

## Answer key

1. B  
2. `[Authorize]`  
3. B  
4. `singer_id` (`KaraokeClaimTypes.SingerId`)  
5. B  
6. `JwtAuthenticationStateProvider`  
7. B  
8. `Extended`  
9. B  
10. `JwtTokenService`  
