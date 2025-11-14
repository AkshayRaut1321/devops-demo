docker compose `
  -f docker-compose.dotnetapi.yml `
  -f docker-compose.mongodb.yml `
  -f docker-compose.elasticsearch.yml `
  up -d --build