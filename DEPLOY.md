# How to deploy the Inventory Management System

One command starts everything on the server:

| Part       | What it is                        | Who can reach it                       |
|------------|-----------------------------------|----------------------------------------|
| `frontend` | The website (nginx)               | Everyone, on port 80 (or `APP_PORT`)   |
| `backend`  | The API (.NET)                    | Only the website, through `/api/...`   |
| `postgres-db` | The database (PostgreSQL 16)   | Only the backend                       |

The browser only talks to the website. The website forwards `/api/...` to the backend.
So you open **one** address, and the database is never open to the network.

---

## 1. What the server needs (one time)

- A server with **Docker** and **Docker Compose** (Linux is best, e.g. Ubuntu 22.04/24.04).
  Check with:
  ```bash
  docker --version
  docker compose version
  ```
  If they are missing on Ubuntu: `curl -fsSL https://get.docker.com | sudo sh`
- **git**
- Port **80** free (or choose another port in step 3).

## 2. Get the code (one time)

Both repositories must be **next to each other** in the same folder:

```bash
mkdir -p ~/inventory && cd ~/inventory

git clone -b Development-Inventory-Management https://github.com/istiak7/Inventory-Management-System.git
git clone -b Development https://github.com/walid123780/inventory-management-frontend.git
```

You should now have:

```
~/inventory/
  Inventory-Management-System/
  inventory-management-frontend/
```

## 3. Create the settings file `.env` (one time)

```bash
cd ~/inventory/Inventory-Management-System
cp .env.example .env
nano .env
```

Change **every** value:

| Setting | What to put |
|---|---|
| `POSTGRES_PASSWORD` | A long random password for the database |
| `JWT_SECRET_KEY` | A random secret, at least 32 characters. Make one with `openssl rand -base64 48` |
| `SEED_ADMIN_EMAIL` | Email of the first admin account |
| `SEED_ADMIN_PASSWORD` | Password of the first admin (at least 8 characters) |
| `APP_URL` | The address people open, e.g. `http://10.70.34.59` |
| `APP_PORT` | `80`, or another port if 80 is busy (then `APP_URL` must include it, e.g. `http://10.70.34.59:8080`) |

`.env` is ignored by git. **Never commit it and never share it.**

## 4. Start

```bash
cd ~/inventory/Inventory-Management-System
docker compose up -d --build
```

The first time takes a few minutes (it downloads and builds everything).
On start, the backend updates the database tables by itself, and creates the first admin from `.env`
if there is no admin yet.

## 5. Check it works

```bash
docker compose ps          # all 3 should be "Up" (database: "healthy")
docker compose logs -f backend    # press Ctrl+C to stop watching
```

Open `APP_URL` in the browser (e.g. `http://10.70.34.59`) and log in with
`SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD`.

**Then change the admin password right away:** Settings → Security → Change password.

Create the other users from **Users & Roles**. If someone forgets their password,
an admin opens the user, types a **New password**, and saves.

---

## Update to a new version

```bash
cd ~/inventory/Inventory-Management-System && git pull
cd ~/inventory/inventory-management-frontend && git pull
cd ~/inventory/Inventory-Management-System && docker compose up -d --build
```

The data is kept (it lives in the Docker volume `postgres_data`, not in the containers).

## Backup the database

Make a backup (do this every day, e.g. with cron):

```bash
docker exec inventory-postgres-db pg_dump -U postgres -d Inventory-Management -Fc > backup_$(date +%F).dump
```

Restore a backup:

```bash
docker exec -i inventory-postgres-db pg_restore -U postgres -d Inventory-Management --clean --if-exists < backup_2026-09-23.dump
```

Keep copies of the backups on another machine.

---

## Moving from the old setup (server already has data)

If this server ran the old `docker-compose.yml` before:

1. **Database password.** PostgreSQL sets its password only when the database is created the
   first time. An existing database keeps its old password. Either:
   - put the old password in `.env` as `POSTGRES_PASSWORD`, **or** (better) change it first:
     ```bash
     docker exec -it inventory-postgres-db psql -U postgres -c "ALTER USER postgres PASSWORD 'your-new-password';"
     ```
     and then put the new password in `.env`.
2. **Old admin account.** An existing database already has the admin `admin@inventory.com`
   with the well-known password `Admin@123`. Log in with it once and change the password
   (Settings → Security). The `SEED_ADMIN_*` values are then not used, but they must still be set.
3. **New address.** The website is now on `APP_PORT` (default 80, before it was 3000), and the API
   is at `APP_URL/api` (before it was port 8080). The database port 5432 is no longer open.
4. **Backup first** (see above), then run step 4.

---

## Problems

| You see | What to do |
|---|---|
| `Set POSTGRES_PASSWORD in .env` (or another name) | That value is missing in `.env`. |
| `container name "/inventory-management-frontend" is already in use` | An old container from the old setup is still there. Remove it (data is not inside it): `docker rm -f inventory-management-frontend`, then run step 4 again. |
| `port is already allocated` | Port 80 is used by something else. Set `APP_PORT=8080` (and `APP_URL=http://<ip>:8080`) in `.env`, run step 4 again. |
| `backend` keeps restarting | `docker compose logs backend` shows why. Common: wrong `POSTGRES_PASSWORD` for an existing database (see "Moving from the old setup"), or "There is no admin user yet" (set `SEED_ADMIN_*`). |
| Login page opens but login fails with "Could not reach the server" | `docker compose ps`: is `backend` Up? Then `docker compose logs backend`. |
| Everything is broken after an update | `docker compose logs backend`, and restore the last backup if the data is damaged. |

## HTTPS (recommended when the system is reachable from the internet)

The setup above uses plain `http`, which is fine inside an office network.
If the system will be opened from the internet, put it behind HTTPS
(for example Caddy or nginx with a Let's Encrypt certificate in front of port `APP_PORT`),
and set `APP_URL` to the `https://` address.

## Developers

Nothing changes for local development: `dotnet run` still uses `appsettings.Development.json`
(and creates `admin@inventory.com` / `Admin@123` on an empty local database),
and the frontend still uses `npm run dev`. Swagger is shown only in Development.
