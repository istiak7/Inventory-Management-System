#!/usr/bin/env bash
# Deploys the newest code of one repo on the server. Run by GitHub Actions (CI/CD) over SSH,
# or by hand:
#
#   bash ~/inventory/Inventory-Management-System/scripts/deploy.sh backend
#   bash ~/inventory/Inventory-Management-System/scripts/deploy.sh frontend
#
# Expects both repos side by side (see DEPLOY.md):
#   <folder>/Inventory-Management-System/      <- this repo (has docker-compose.yml and .env)
#   <folder>/inventory-management-frontend/
#
# Steps: get the newest code -> (backend) back up the database -> rebuild that part with Docker
# -> wait until /api/health says the system is healthy. Any failure stops with exit code 1,
# so the GitHub job turns red.

# Everything is inside main(), which bash reads completely before running it. This matters
# because "git merge" below can replace this very file while it is running.
main() {
  set -euo pipefail

  local service="${1:-}"
  local script_dir app_dir backend_dir frontend_dir repo_dir branch
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  backend_dir="$(cd "$script_dir/.." && pwd)"
  app_dir="$(cd "$backend_dir/.." && pwd)"
  frontend_dir="$app_dir/inventory-management-frontend"

  case "$service" in
    backend)  repo_dir="$backend_dir";  branch="Development-Inventory-Management" ;;
    frontend) repo_dir="$frontend_dir"; branch="Development" ;;
    *) echo "Usage: deploy.sh backend|frontend" >&2; exit 2 ;;
  esac

  # Only one deploy at a time: when both repos are pushed together, the second one waits here.
  exec 9>/tmp/inventory-deploy.lock
  echo "==> Waiting for any other deploy to finish..."
  flock 9

  echo "==> [$service] Getting the newest code ($branch)"
  git -C "$repo_dir" fetch --quiet origin "$branch"
  git -C "$repo_dir" checkout --quiet "$branch"
  # Only a fast-forward: a server copy with its own changes is never overwritten.
  git -C "$repo_dir" merge --ff-only "origin/$branch"
  git -C "$repo_dir" log --oneline -1

  cd "$backend_dir"

  if [[ "$service" == "backend" ]]; then
    backup_database "$backend_dir"
  fi

  echo "==> [$service] Rebuilding and restarting"
  docker compose up -d --build "$service"
  docker image prune -f >/dev/null

  wait_until_healthy "$backend_dir"
  echo "==> [$service] Deployed successfully"
}

# The backend applies database changes (migrations) when it starts, so keep a copy first.
# Restore one with:  docker exec -i inventory-postgres-db pg_restore -U postgres -d Inventory-Management --clean --if-exists < backups/<file>.dump
backup_database() {
  local backend_dir="$1"
  if ! docker ps --format '{{.Names}}' | grep -qx inventory-postgres-db; then
    echo "==> Database is not running yet, no backup needed"
    return
  fi

  mkdir -p "$backend_dir/backups"
  local file
  file="$backend_dir/backups/before-deploy-$(date +%Y%m%d-%H%M%S).dump"
  echo "==> Backing up the database to backups/$(basename "$file")"
  docker exec inventory-postgres-db pg_dump -U postgres -d Inventory-Management -Fc > "$file"

  # Keep the 10 newest backups. (ls is safe here: this script names the files itself.)
  # shellcheck disable=SC2012
  ls -1t "$backend_dir"/backups/before-deploy-*.dump | tail -n +11 | xargs -r rm -f
}

# Polls /api/health (through nginx, like a real user) for up to 2 minutes.
wait_until_healthy() {
  local backend_dir="$1"
  local port
  port="$(grep -E '^APP_PORT=' "$backend_dir/.env" 2>/dev/null | tail -n 1 | cut -d= -f2 | tr -d '[:space:]"' || true)"
  port="${port:-80}"
  local url="http://localhost:${port}/api/health"

  echo "==> Waiting for $url"
  for _ in $(seq 1 40); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo "==> Healthy: $(curl -fsS "$url")"
      return
    fi
    sleep 3
  done

  echo "==> NOT healthy after 2 minutes. Last backend logs:" >&2
  docker compose logs --tail 100 backend >&2 || true
  exit 1
}

main "$@"
