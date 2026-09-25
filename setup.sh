#!/usr/bin/env bash
# One-time setup script for Mac/Linux.
# Restores dependencies, restores local dotnet tools, and applies EF Core migrations.
#
# NOTE: LocalDB (the default connection string in appsettings.json) is
# Windows-only. On Mac/Linux, run SQL Server via Docker first, e.g.:
#   docker compose up -d sqlserver
# and set ConnectionStrings__DefaultConnection with `dotnet user-secrets`
# (see README.md) to point at that instance before running this script.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$repo_root"

echo "==> Restoring NuGet packages..."
dotnet restore GiftOfTheGivers.slnx

echo "==> Restoring local dotnet tools (dotnet-ef)..."
dotnet tool restore

echo "==> Applying EF Core migrations..."
dotnet ef database update \
  --project GiftOfTheGivers/GiftOfTheGivers.csproj \
  --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj

cat <<'EOF'

Setup complete!

Next steps:
  1. (Optional) Add your own local secrets - see README.md 'Managing local secrets'.
  2. Run the app:
       dotnet run --project GiftOfTheGivers/GiftOfTheGivers.csproj
  3. Browse to the URL printed in the console (e.g. http://localhost:5106).

Demo login (seeded automatically on first run):
  Employee : employee@giftofthegivers.org / Employee#123
  Donor    : donor@example.com / Donor#123
EOF
