# ParkHere – Deploy (Supabase + Render)

**PostgreSQL** (Supabase or local) for the database, **Render** for the backend API. Use `.env` for secrets; `.env` is **not** in `.gitignore` for this setup (adjust if you need to keep secrets out of the repo).

---

## 1. Database: PostgreSQL (Supabase or local)

- **Local:** Use `docker-compose` (Postgres + RabbitMQ + API). Set `DB_MODE=local` and local `DB_*` in `.env`.
- **Supabase:** Create a project, then in **Project Settings → Database** copy:
  - **Host** (Session pooler, e.g. `xxx.pooler.supabase.com`)
  - **Database**, **User**, **Password**
- In `ParkHere.WebAPI/.env` set:
  - `DB_MODE=supabase`
  - `DB_SUPABASE_HOST=...`
  - `DB_SUPABASE_PORT=5432`
  - `DB_SUPABASE_NAME=postgres`
  - `DB_SUPABASE_USER=...`
  - `DB_SUPABASE_PASSWORD=...`

---

## 2. EF migrations (PostgreSQL) – you create them

From the repo root (folder containing `ParkHere.WebAPI`):

```bash
dotnet ef migrations add InitialPostgres --project ParkHere.Services --startup-project ParkHere.WebAPI
```

Ensure `ParkHere.WebAPI/.env` has valid DB settings (local or Supabase) so the startup project can connect. Migrations run automatically on app startup when you deploy or run the API.

---

## 3. Local run (no Docker)

1. Have PostgreSQL running (local or Supabase).
2. Fill in `ParkHere.WebAPI/.env` with DB (and optional RabbitMQ).
3. From repo root:
   ```bash
   dotnet run --project ParkHere.WebAPI
   ```

---

## 4. Local Docker (Postgres + RabbitMQ + API)

From repo root:

```bash
# .env in repo root must have DB_NAME, DB_USER, DB_PASSWORD, RABBITMQ__*
docker-compose up -d
```

API: `http://localhost:5130`. DB: `localhost:5432`, RabbitMQ: `5672` / management `15672`.

---

## 5. Deploy API to Render

1. **New Web Service**, connect this repo; root = folder that contains `Dockerfile` and `ParkHere.WebAPI`.
2. **Build:** Docker (use the repo `Dockerfile`).
3. **Environment (Render dashboard):**
   - `ENVIRONMENT=production`
   - `DB_MODE=supabase`
   - `DB_SUPABASE_HOST=...`
   - `DB_SUPABASE_PORT=5432`
   - `DB_SUPABASE_NAME=postgres`
   - `DB_SUPABASE_USER=...`
   - `DB_SUPABASE_PASSWORD=...`
   - `FRONTEND_URL=https://your-frontend-url.com` (for CORS)
   - RabbitMQ vars if you use a hosted broker.
   - `PORT` is set by Render (do not override).
4. Deploy. The API listens on `PORT` and runs migrations on startup.

---

## 6. Frontend apps – backend URL

- **Mobile** and **Desktop** use `String.fromEnvironment("baseUrl", defaultValue: ...)`.
- **Local:** Default is `http://10.0.2.2:5130/` (mobile) or `http://localhost:5130/` (desktop). No change needed.
- **Production:** When you have the Render API URL, build with:
  ```bash
  flutter build apk --dart-define=baseUrl=https://YOUR_RENDER_SERVICE.onrender.com/
  # or for desktop
  flutter build windows --dart-define=baseUrl=https://YOUR_RENDER_SERVICE.onrender.com/
  ```
  Or set the production URL as the default in each app's `BaseProvider` when you're ready.

---

## 7. .env and .gitignore

- `.env` files are **not** in `.gitignore` in this project (per your preference).
- Repo root `.env` – used by `docker-compose` (DB_NAME, DB_USER, DB_PASSWORD, RABBITMQ__*, etc.).
- `ParkHere.WebAPI/.env` – used by the API at runtime and by `dotnet ef` (DB_MODE, DB_* or DB_SUPABASE_*, ENVIRONMENT, FRONTEND_URL, etc.).
