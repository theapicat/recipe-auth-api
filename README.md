# 🔑 recipe-authentication-api

Identitets- og autentiseringstjeneste for **Kjøkkenhylla**-økosystemet, bygget med .NET, ASP.NET Core Identity og **OpenIddict**. Tjenesten eier `recipe_auth_db` (PostgreSQL på port 5432) og håndterer brukerregistrering, profiladministrasjon samt utstedelse og fornyelse av OAuth2/OIDC JWT-tokens.

---

## 🛠️ Arkitektur og Ruting

Auth API-et kjører lokalt på **port 5001**. Klienter kommuniserer med tjenesten gjennom **recipe-gateway-api** (port 5000), som ruter alle `/api/auth/*`-forespørsler videre.

| Endepunkt (Gjennom Gateway) | Metode | Content-Type | Autentisering | Beskrivelse |
| --- | --- | --- | --- | --- |
| `/api/auth/connect/token` | `POST` | `application/x-www-form-urlencoded` | Anonym | Utsteder og fornyer JWT access tokens og refresh tokens via OpenIddict. |
| `/api/auth/account/register` | `POST` | `application/x-www-form-urlencoded` / `application/json` | Anonym | Registrerer ny bruker og returnerer `UserProfileResponse`. |
| `/api/auth/account/me` | `GET` | *Ingen* | Bearer Token | Henter profilinformasjon for den innloggede brukeren (`UserProfileResponse`). |
| `/api/auth/account/profile` | `PUT` | `application/json` | Bearer Token | Oppdaterer fornavn, etternavn og avatar, og returnerer oppdatert `UserProfileResponse`. |
| `/api/auth/account/change-password` | `POST` | `application/json` | Bearer Token | Endrer passord for innlogget bruker. |
| `/api/auth/account/me` | `DELETE` | *Ingen* | Bearer Token | Permanent sletting av brukerens konto. |

---

## 📋 DTO-Spesifikasjoner

### 1. `RegisterRequest`

Brukes ved brukerregistrering på `POST /api/auth/account/register`. Endepunktet godtar både JSON og `application/x-www-form-urlencoded`.

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

**Form Data Parametere (`application/x-www-form-urlencoded`):**

* `Email`: `bruker@example.com`
* `Password`: `DittPassord123!`
* `FirstName`: `Ola`
* `LastName`: `Nordmann`

---

### 2. `UserProfileResponse`

Standard respons som returneres ved fullført registrering (`POST /account/register`), ved profilhenting (`GET /account/me`) og ved profiloppdatering (`PUT /account/profile`).

```csharp
public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

```

---

### 3. `UpdateProfileRequest` & `ChangePasswordRequest`

```csharp
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Fornavn er påkrevd.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Etternavn er påkrevd.")]
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

OpenIddict håndterer token-utstedelse og fornyelse på endepunktet `/connect/token`. Forespørsler **må** sendes som `application/x-www-form-urlencoded`.

Gyldige klient-ID-er:

* `recipe-web-app`
* `recipe-mobile-app`

### A. Innlogging (Password Grant)

* **URL**: `POST /connect/token`
* **Header**: `Content-Type: application/x-www-form-urlencoded`
* **Body**:
* `grant_type`: `password`
* `username`: `bruker@example.com`
* `password`: `DittPassord123!`
* `client_id`: `recipe-web-app`



### B. Fornyelse av Token (Refresh Token Grant)

* **URL**: `POST /connect/token`
* **Header**: `Content-Type: application/x-www-form-urlencoded`
* **Body**:
* `grant_type`: `refresh_token`
* `refresh_token`: `<Mottatt refresh_token>`
* `client_id`: `recipe-web-app`



### C. Vellykket Respons (`200 OK`)

```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "eyJhbGciOiJIUzI1...",
  "scope": "openid profile roles offline_access"
}

```

### D. Feilrespons fra OpenIddict (`400 Bad Request` / `401 Unauthorized`)

Dersom innlogging eller fornyelse mislykkes (ugyldig passord, utløpt token osv.), returnerer OpenIddict en standard OAuth2 JSON-feilrespons:

```json
{
  "error": "invalid_grant",
  "error_description": "Ugyldig e-post eller passord."
}

```

Standard `error`-koder du kan møte på:

* `invalid_grant`: Feil brukernavn/passord, eller utløpt/ugyldig refresh token.
* `unsupported_grant_type`: Sendt ugyldig `grant_type` (må være `password` eller `refresh_token`).
* `invalid_client`: Ugyldig eller manglende `client_id`.

---

## 💻 Frontend Integration (JavaScript / Next.js)

### 1. Registrering med `application/x-www-form-urlencoded`

```typescript
export async function registerUser(formData: { email: string; password: string; firstName: string; lastName: string }) {
  const body = new URLSearchParams();
  body.append('Email', formData.email);
  body.append('Password', formData.password);
  body.append('FirstName', formData.firstName);
  body.append('LastName', formData.lastName);

  const response = await fetch('http://localhost:5000/api/auth/account/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: body.toString(),
  });

  if (!response.ok) {
    const errorData = await response.json();
    throw new Error(errorData.message || 'Registrering mislyktes');
  }

  return await response.json(); // Returnerer UserProfileResponse
}

```

### 2. Innlogging & Feilhåndtering mot `/connect/token`

```typescript
export async function loginUser(email: string, password: string) {
  const body = new URLSearchParams();
  body.append('grant_type', 'password');
  body.append('username', email);
  body.append('password', password);
  body.append('client_id', 'recipe-web-app');

  const response = await fetch('http://localhost:5000/api/auth/connect/token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: body.toString(),
  });

  const data = await response.json();

  if (!response.ok) {
    // Fanger opp OpenIddict sine standard feilmeldingsfelt
    throw new Error(data.error_description || data.error || 'Innlogging mislyktes');
  }

  return data; // Returnerer access_token og refresh_token
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

API-et lytter på **`http://localhost:5001`** og er tilgjengelig via Gateway på **`http://localhost:5000/api/auth/*`**.


---

## 🚧 Work in Progress (Planlagte Funksjoner)

Følgende funksjonalitet og endepunkter er under vurdering og planlagt for fremtidige versjoner:

| Endepunkt (Gjennom Gateway) | Metode | Status | Beskrivelse |
| --- | --- | --- | --- |
| `/api/auth/account/recover` | `POST` | 🚧 Planlagt | Genererer token/magic link for tilbakestilling og sender e-post til bruker. |
| `/api/auth/account/reset-password` | `POST` | 🚧 Planlagt | Validerer token fra e-post og oppdaterer passordet for uinnloggede brukere. |
| `/api/auth/account/confirm-email` | `POST` | 🚧 Planlagt | Bekrefter og verifiserer brukerens e-postadresse via mottatt token. |
| `/api/auth/account/external-login` | `GET` | 🚧 Planlagt | Initiere OAuth2-innloggingsflyt mot Google. |
| `/api/auth/account/external-callback` | `GET` | 🚧 Planlagt | Håndterer retursvar fra Google og utsteder JWT-token via OpenIddict. |