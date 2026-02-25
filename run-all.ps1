docker compose `
  -f docker-compose.elasticsearch.yml `
  -f docker-compose.mongodb.yml `
  -f docker-compose.dotnetindexworker.yml `
  -f docker-compose.dotnetapi.yml `
  up -d --build