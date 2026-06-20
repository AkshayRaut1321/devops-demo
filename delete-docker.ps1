Write-Host "Stopping containers and deleting volumes..."

docker compose `
  -f docker-compose.dotnetapi.yml `
  -f docker-compose.mongodb.yml `
  -f docker-compose.elasticsearch.yml `
  -f docker-compose.dotnetindexworker.yml `
  down -v --remove-orphans