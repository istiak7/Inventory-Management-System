# How to deploy the Inventory Management System

One command starts everything on the server:

| Part       | What it is                        | Who can reach it                       |
|------------|-----------------------------------|----------------------------------------|
| `frontend` | The website (nginx)               | Everyone, on port 80 (or `APP_PORT`)   |
| `backend`  | The API (.NET)                    | Only the website, through `/api/...`   |
| `postgres-db` | The database (PostgreSQL 16)   | The backend, and SQL tools on the server itself (SSH tunnel for remote, see below) |

The browser only talks to the website. The website forwards `/api/...` to the backend.
So you open **one** address, and the database is never open to the network: its port is bound to
`127.0.0.1` of the server and is reached remotely only through an SSH tunnel.

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

The frontend repository is **private**. The automatic deploy (CI/CD) does not need anything on
the server for it: the GitHub job sends its own short-lived, read-only token and the server
downloads over HTTPS with it. The key below is only needed for the **first clone** and for
running `scripts/deploy.sh frontend` **by hand** on the server (one time):

```bash
ssh-keygen -t ed25519 -f ~/.ssh/frontend_deploy_key -N "" -C "vps-frontend-deploy"
cat ~/.ssh/frontend_deploy_key.pub
```

Copy the printed line. On GitHub open the **frontend** repo → **Settings → Deploy keys →
Add deploy key**, paste it, leave "Allow write access" **off**, save. Then tell git to use it:

```bash
cat >> ~/.ssh/config <<'EOF'
Host github-frontend
  HostName github.com
  User git
  IdentityFile ~/.ssh/frontend_deploy_key
  IdentitiesOnly yes
EOF
```

Now download both repositories:

```bash
cd ~

git clone -b Development-Inventory-Management https://github.com/istiak7/Inventory-Management-System.git
git clone -b Development git@github-frontend:walid123780/inventory-management-frontend.git
```

Use exactly these places (your home folder, `~`) and these folder names: the automatic deploy
(CI/CD) expects them.

You should now have:

```
~/                                  (your home folder)
  Inventory-Management-System/
  inventory-management-frontend/
```

## 3. Create the settings file `.env` (one time)

```bash
cd ~/Inventory-Management-System
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
| `POSTGRES_EXTERNAL_PORT` | Optional. Server port for SQL tools (default `5433`). Only open on the server itself. |
| `APP_PORT` | `80`, or another port if 80 is busy (then `APP_URL` must include it, e.g. `http://10.70.34.59:8080`) |

`.env` is ignored by git. **Never commit it and never share it.**

## 4. Start

```bash
cd ~/Inventory-Management-System
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
cd ~/Inventory-Management-System && git pull
cd ~/inventory-management-frontend && git pull
cd ~/Inventory-Management-System && docker compose up -d --build
```

The data is kept (it lives in the Docker volume `postgres_data`, not in the containers).

Once CI/CD (next section) is set up you do not need to do this by hand anymore.

---

## Automatic deploy (CI/CD)

Both repositories have the same workflow file: `.github/workflows/ci-cd.yml`.

| When | What happens |
|---|---|
| Every push or pull request | **CI** checks the code. Backend: build, "is a database migration missing?", Docker image. Frontend: lint, type-check, build, Docker image. A red ✗ means something is broken. |
| A push to the **Development** branch that passes CI | **CD** logs in to the VPS with SSH and runs `scripts/deploy.sh`: download the new code → (backend) back up the database → rebuild with Docker → check `/api/health`. |

Backend branch: `Development-Inventory-Management`. Frontend branch: `Development`.

### One-time setup

1. **Server.** Do steps 1–5 above first (both repos in your home folder `~`, `.env`, first start).
   The SSH user must be allowed to use Docker (`sudo usermod -aG docker <user>`, then log in
   again) and to log in with a password (`PasswordAuthentication yes` in
   `/etc/ssh/sshd_config`, then `sudo systemctl restart ssh`).
2. **Secrets.** In **both** repositories on GitHub open **Settings → Secrets and variables →
   Actions → New repository secret** and add these 3 (same names in both):

   | Name | Value |
   |---|---|
   | `SERVER` | The VPS IP address or host name, e.g. `10.70.34.59` |
   | `USER` | The SSH user name |
   | `PASS` | The SSH password |

   Only a repository admin can add secrets (backend: istiak7, frontend: walid123780).
   Until the secrets exist, the deploy step only prints "Deploy is not configured yet".
3. **Safety.** Use a long, random password: a password login is easier to guess than an SSH
   key. GitHub hides secrets in the logs.

### Every day

- **See a deploy:** GitHub → the repository → **Actions** tab → the newest "CI/CD" run.
- **Deploy again by hand:** Actions → CI/CD → **Run workflow**.
- **Deploy from the server itself:** `bash ~/Inventory-Management-System/scripts/deploy.sh backend`
  (or `frontend`).
- **Undo a bad change:** revert the commit (`git revert <commit>`) and push. CD deploys the
  reverted code. If the database must go back too, restore the backup that the backend deploy
  made in `~/Inventory-Management-System/backups/` (the 10 newest are kept):

  ```bash
  docker exec -i inventory-postgres-db pg_restore -U postgres -d Inventory-Management --clean --if-exists < ~/Inventory-Management-System/backups/<file>.dump
  ```

| Deploy problem | What to do |
|---|---|
| `Permission denied` | Wrong `USER` / `PASS`, or password login is off on the server (step 1). |
| `git@github.com: Permission denied (publickey)` while getting the frontend code | The server's `scripts/deploy.sh` is older than the HTTPS-token deploy. Push the backend first (or `git pull` in `~/Inventory-Management-System` on the server), then re-run the frontend job. For a deploy **by hand**, the server needs the deploy key from step 2 and the frontend remote must be `git@github-frontend:...` (`git remote -v`). |
| `Not possible to fast-forward` | Someone changed files on the server inside the repo. Look with `git status` there and undo those changes. |
| `NOT healthy after 2 minutes` | The job prints the backend logs. Fix the cause and push again. |

---

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

## Connect to the database (DBeaver, pgAdmin, any SQL tool)

The database port is published **only on the server's own `127.0.0.1`** (port
`POSTGRES_EXTERNAL_PORT`, default `5433`). Nobody on the network or the internet can reach it
directly. You get in through an **SSH tunnel**: your SSH login to the server carries the database
connection. The backend is not affected: it still uses `Host=postgres-db;Port=5432` inside Docker.

### DBeaver (recommended)

New Connection → **PostgreSQL**.

**Main tab**

| Setting | Value |
|---|---|
| Host | `localhost` (this is the address *as seen from the server*, through the tunnel) |
| Port | `5433` (your `POSTGRES_EXTERNAL_PORT`) |
| Database | `Inventory-Management` |
| Username | `postgres` |
| Password | the `POSTGRES_PASSWORD` value from the server's `.env` |

**SSH tab** → tick **Use SSH Tunnel**

| Setting | Value |
|---|---|
| Host/IP | the server address, e.g. `10.70.34.59` |
| Port | `22` (your SSH port) |
| User Name | the server's SSH user (the same one you log in with) |
| Authentication Method | **Public Key** (best: pick your private key file, plus its passphrase if any). *Password* also works if the server allows password login. |

Press **Test Connection**, then **Finish**. pgAdmin works the same way (Connection tab: host
`localhost`, port `5433`; SSH Tunnel tab: same SSH values).

No SSH tool? Open the tunnel yourself and point DBeaver at `localhost:5433` without the SSH tab:

```bash
ssh -N -L 5433:127.0.0.1:5433 <ssh-user>@<server-ip>
```

(If your own PC already uses 5433, use another local port: `-L 6543:127.0.0.1:5433` and connect to 6543.)

### Security rules

- Keep `POSTGRES_BIND_ADDRESS` unset (= `127.0.0.1`). Then the port cannot be reached from outside,
  and no firewall rule is needed for it. Do **not** open `5433` in the firewall.
- Use a long random `POSTGRES_PASSWORD`; never put it in git, chat or screenshots.
- Prefer SSH key login, and give the SSH login only to people who may see production data.
- A database tool can change or delete data: use a read-only database user for people who only look.
  ```bash
  docker exec -it inventory-postgres-db psql -U postgres -d Inventory-Management -c "CREATE ROLE readonly LOGIN PASSWORD 'another-long-random-password'; GRANT CONNECT ON DATABASE \"Inventory-Management\" TO readonly; GRANT USAGE ON SCHEMA public TO readonly; GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly; ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO readonly;"
  ```

### Direct remote access (only if a tunnel is truly impossible)

1. In `.env` set `POSTGRES_BIND_ADDRESS=0.0.0.0` (and apply, below).
2. **Firewall first, allowlist only your IPs.** Docker bypasses `ufw` rules for published ports, so
   put the rule in the `DOCKER-USER` chain, or in your cloud/provider firewall:
   ```bash
   sudo iptables -I DOCKER-USER -p tcp --dport 5433 -j DROP
   sudo iptables -I DOCKER-USER -p tcp --dport 5433 -s <your-office-ip> -j ACCEPT
   ```
   (the ACCEPT is inserted last, so it is checked first; repeat it for each allowed IP, and make the
   rules permanent with `iptables-persistent`). Check from a non-allowed network that the port is closed.
3. Use a strong password, and prefer the read-only user above. For encrypted traffic use an SSH
   tunnel or configure PostgreSQL SSL: plain PostgreSQL over the internet is not encrypted.

### Apply the change on the server (data is kept)

The database data lives in the Docker volume `postgres_data`. These commands only recreate the
containers; they never touch the volume. **Never use `docker compose down -v`** (it deletes it).

```bash
cd ~/Inventory-Management-System && git pull
# optional: add POSTGRES_EXTERNAL_PORT=5433 to .env (5433 is the default anyway)

docker compose exec postgres-db pg_dump -U postgres -d Inventory-Management -Fc > ~/backup_before_port_$(date +%F).dump   # safety backup
docker compose config >/dev/null && echo "compose file OK"
docker compose up -d postgres-db     # recreates only the database container (a few seconds), same volume
docker compose up -d                 # backend/frontend start or stay as they are
docker compose ps                    # postgres-db should show 127.0.0.1:5433->5432/tcp and be healthy
docker volume ls | grep postgres_data   # the data volume is still there
```

On the server itself you can test with: `psql -h 127.0.0.1 -p 5433 -U postgres -d Inventory-Management`.

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
   is at `APP_URL/api` (before it was port 8080). The database port 5432 is no longer open to the
   network; use the SSH tunnel in "Connect to the database" below.
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
