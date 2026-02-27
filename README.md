# Automated Multilingual Research Submission Processor (AMRSP)

A full-stack web application for managing multilingual academic research submissions, built with **ASP.NET Core 10** (Web API) and **Angular 21**.

---

## Project Structure

```
automated-multilingual-research-submission-processor/
├── backend/          # ASP.NET Core 10 Web API
│   ├── Program.cs
│   ├── backend.csproj
│   └── appsettings.json
├── frontend/         # Angular 21 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/   # login, admin, user, health-status
│   │   │   ├── services/     # auth.service, api.service
│   │   │   └── auth.guard.ts
│   │   └── environments/
│   └── package.json
└── automated-multilingual-research-submission-processor.sln
```

---

## Prerequisites

| Tool        | Version | Download                              |
| ----------- | ------- | ------------------------------------- |
| .NET SDK    | 10.0+   | https://dotnet.microsoft.com/download |
| Node.js     | 20+ LTS | https://nodejs.org                    |
| Angular CLI | 21+     | `npm install -g @angular/cli`         |

---

## Getting Started

### 1 — Clone the repository

```bash
git clone <repository-url>
cd automated-multilingual-research-submission-processor
```

---

### 2 — Run the Backend (ASP.NET Core Web API)

```bash
cd backend
dotnet run
```

The API starts on:

- **HTTP:** `http://localhost:5091`
- **HTTPS:** `https://localhost:7032`

#### Available Endpoints

| Method | URL                | Description                           |
| ------ | ------------------ | ------------------------------------- |
| `GET`  | `/api/health`      | Returns API version and health status |
| `GET`  | `/openapi/v1.json` | OpenAPI spec (development only)       |

Sample `/api/health` response:

```json
{
  "version": "1.0.0",
  "status": "healthy",
  "timestamp": "2026-02-27T07:00:00Z"
}
```

---

### 3 — Run the Frontend (Angular)

In a **separate terminal**:

```bash
cd frontend
npm install
ng serve
```

The app opens at **`http://localhost:4200`**

---

## Application Credentials

| Username | Password | Role          | Lands on                   |
| -------- | -------- | ------------- | -------------------------- |
| `admin`  | `admin`  | Administrator | `/admin` — Admin Dashboard |
| `user`   | `user`   | User          | `/user` — User Dashboard   |

> Authentication is static / in-memory (no database). Intended for development/demo.

---

## Features

- **Login page** — validates static credentials, redirects by role
- **Admin Dashboard** — dark-blue themed, manage submissions, users, reports
- **User Dashboard** — green themed, submit and track research papers
- **API Health Indicator** — live badge in the header showing API connectivity
  - 🟢 Green = API reachable, displays version
  - 🔴 Red = API unreachable
  - Click the badge to re-check
- **Route Guards** — unauthenticated access redirects to `/login`
- **Logout** — clears session and returns to login

---

## Configuration

### Backend — `backend/appsettings.json`

CORS is configured for `http://localhost:4200` in development. To change the allowed origin, update `Program.cs`:

```csharp
policy.WithOrigins("http://localhost:4200")
```

### Frontend — `frontend/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5091',
};
```

Change `apiUrl` if your backend runs on a different port.

---

## Building for Production

**Backend:**

```bash
cd backend
dotnet publish -c Release -o ./publish
```

**Frontend:**

```bash
cd frontend
ng build --configuration production
# Output: frontend/dist/frontend/
```

---

## Running Tests

**Frontend unit tests:**

```bash
cd frontend
ng test
```

---

## Tech Stack

| Layer    | Technology                                |
| -------- | ----------------------------------------- |
| Frontend | Angular 21, TypeScript, TailwindCSS       |
| Backend  | ASP.NET Core 10, Minimal APIs             |
| Auth     | In-memory static users (Angular signals)  |
| HTTP     | Angular HttpClient → ASP.NET Core REST    |
| Styling  | Component-scoped CSS + global TailwindCSS |
