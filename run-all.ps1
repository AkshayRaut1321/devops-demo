docker compose `
  -f docker-compose.dotnetapi.yml `
  -f docker-compose.mongodb.yml `
  -f docker-compose.elasticsearch.yml `
  -f docker-compose.dotnetindexworker.yml `
  up -d --build