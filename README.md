# 🔑 recipe-authentication-api

Identitets- og autentiseringstjeneste for **Kjøkkenhylla**-økosystemet, bygget med .NET, ASP.NET Core Identity og **OpenIddict**. Tjenesten eier `recipe_auth_db` (PostgreSQL på port 5432) og håndterer brukerregistrering, profiladministrasjon samt utstedelse og fornyelse av OAuth2/OIDC JWT-tokens.

---

## 🛠️ Arkitektur og Ruting

Auth API-et kjører lokalt på **port 5001**. Klienter kommuniserer normalt med tjenesten gjennom **recipe-gateway-api** (port 5000), som ruter alle `/api/auth/*`-forespørsler videre.

| Endepunkt (Gjennom Gateway) | Metode | Content-Type | Autentisering | Beskrivelse |
| --- | --- | --- | --- | --- |
| `/api/auth/connect/token` | `POST` | `application/x-www-form-urlencoded` | Anonym | Henter ut Access Token / Refresh Token ved innlogging eller fornyelse. |
| `/api/auth/api/account/register` | `POST` | `application/json` | Anonym | Registrerer en ny bruker. |
| `/api/auth/api/account/me` | `GET` | *Ingen* | Bearer Token | Henter den innloggede brukerens profil. |
| `/api/auth/api/account/profile` | `PUT` | `application/json` | Bearer Token | Oppdaterer fornavn, etternavn og avatar. |
| `/api/auth/api/account/change-password` | `POST` | `application/json` | Bearer Token | Endrer passord for innlogget bruker. |
| `/api/auth/api/account/me` | `DELETE` | *Ingen* | Bearer Token | Permanent sletting av brukerens konto. |

---

## 📋 DTO-Spesifikasjoner

### 1. `RegisterRequest`

```csharp
public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

```

### 2. `UserCreatedResponse` & `LoginResponse`

```csharp
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class UserCreatedResponse : LoginResponse
{
    public string Message { get; set; } = "Bruker opprettet med hell.";
}

```

### 3. `UpdateProfileRequest` & `ChangePasswordRequest`

```csharp
public class UpdateProfileRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

```

---

## 🔑 OAuth2 Token Exchange (`/connect/token`)

OpenIddict krever at forespørsler mot `/connect/token` er formatert i henhold til OAuth2-standarden ved å bruke **`application/x-www-form-urlencoded`** i stedet for JSON.

### A. Innlogging (Password Grant)

* **URL**: `POST /connect/token`
* **Header**: `Content-Type: application/x-www-form-urlencoded`
* **Body Form Parameters**:
* `grant_type`: `password`
* `username`: `bruker@example.com`
* `password`: `DittPassord123!`



### B. Fornyelse av Token (Refresh Token Grant)

* **URL**: `POST /connect/token`
* **Header**: `Content-Type: application/x-www-form-urlencoded`
* **Body Form Parameters**:
* `grant_type`: `refresh_token`
* `refresh_token`: `<Mottatt refresh_token>`



### C. Respons-format fra `/connect/token`

```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "f83a4b98-12cd-4e56-8a90-..."
}

```

---

## 💻 Frontend Integration (JavaScript / Next.js)

Siden `/connect/token` forventer URL-enkodede skjermadata, benyttes `URLSearchParams` i `fetch` fra **recipe-web-app**:

```typescript
// Eksempel på innlogging fra Next.js / React
export async function loginUser(email: string, password: string) {
  const body = new URLSearchParams();
  body.append('grant_type', 'password');
  body.append('username', email);
  body.append('password', password);

  const response = await fetch('http://localhost:5000/api/auth/connect/token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: body.toString(),
  });

  if (!response.ok) {
    throw new Error('Innlogging mislyktes');
  }

  const data = await response.json();
  // data.access_token lagres og sendes som "Bearer <token>" mot Gateway
  return data;
}

```

---

## ⚙️ Utvikleroppsett & Kjøring

1. **Infrastruktur**: Sørg for at PostgreSQL kjører i Docker via `recipe-infrastructure` (`recipe-auth-db` på port 5432).


2. **Databasemigrasjoner**:
```bash
dotnet ef database update

```


3. **Kjør applikasjonen**:
```bash
dotnet watch

```


API-et vil lytte på **`http://localhost:5001`**.