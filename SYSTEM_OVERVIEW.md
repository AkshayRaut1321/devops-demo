# DevOpsDemo System Overview

## Repository Summary

This repo is a layered .NET solution for a product catalog and search demo. It includes:

- `DevOpsDemo`: ASP.NET Core Web API
- `DevOpsDemo.IndexerWorker`: background worker that syncs MongoDB data into Elasticsearch
- `DevOpsDemo.Application`: business logic and search orchestration
- `DevOpsDemo.Infrastructure`: MongoDB/Elasticsearch integration, repository implementations, seeding
- `DevOpsDemo.Domain`: domain model definitions
- `DevOpsDemo.Infrastructure.Tests`: infrastructure-focused integration tests using Mongo2Go
- `DevOpsDemo.MongoPlayground`: exploratory MongoDB code (sandbox)

---

## Application Entry Points

### `DevOpsDemo/Program.cs`

This is the HTTP API startup.

- Builds the host with `WebApplication.CreateBuilder(args)`
- Loads `appsettings.json` and environment-specific overrides
- Registers controllers, Swagger, application services
- Calls `AddApplicationServices(builder.Configuration, builder.Environment.IsDevelopment())`
- Seeds MongoDB via `DatabaseSeeder` when `Development` or `Docker`
- Maps API controllers and a few diagnostic endpoints

### `DevOpsDemo.IndexerWorker/Program.cs`

This is the background worker startup.

- Builds a minimal host
- Loads worker-specific configuration
- Registers Mongo and Elasticsearch infrastructure
- Registers hosted services:
  - `ElasticBootstrapService`
  - `ChangeStreamWorker`
  - `FullReindexWorker`
- Exposes health endpoint: `/health/ready`

---

## Major Components

### API Layer

- `DevOpsDemo/Controllers/ProductsController.cs`
  - CRUD endpoints for products
  - search endpoints
  - aggregation endpoints
  - combined product-discount endpoint

### Application Layer

- `DevOpsDemo.Application/Services/ProductService.cs`
- `DevOpsDemo.Application/Services/ProductAndDiscountService.cs`
- `DevOpsDemo.Application/Services/SalesService.cs`
- `DevOpsDemo.Application/Search/ProductSearchService.cs`
- `DevOpsDemo.Application/ApplicationServiceExtensions.cs`

### Infrastructure Layer

- `DevOpsDemo.Infrastructure/InfrastructureServiceExtensions.cs`
  - registers `IMongoClient`, `IMongoDatabase`, `ElasticsearchClient`, repositories, and `IElasticIndexService`
- `DevOpsDemo.Infrastructure/Implementation/ElasticIndexService.cs`
- `DevOpsDemo.Infrastructure/DomainImplementation/ProductRepository.cs`
- `DevOpsDemo.Infrastructure/DomainImplementation/ProductAndDiscountRepository.cs`
- `DevOpsDemo.Infrastructure/DomainImplementation/SalesRepository.cs`
- `DevOpsDemo.Infrastructure/Seed/DatabaseSeeder.cs`
- config classes:
  - `MongoDbSettings`
  - `ElasticSearchSettings`

### Worker Components

- `DevOpsDemo.IndexerWorker/Services/ElasticBootstrapService.cs`
  - creates Elasticsearch index and alias
  - marks worker readiness
- `DevOpsDemo.IndexerWorker/Services/FullReindexWorker.cs`
  - bulk reindexes Mongo products into Elasticsearch on startup when enabled
- `DevOpsDemo.IndexerWorker/Services/ChangeStreamWorker.cs`
  - listens for MongoDB change stream events on the `products` collection
  - upserts inserts/updates to Elasticsearch
  - removes deleted product docs from Elasticsearch
  - persists resume tokens into MongoDB checkpoint collection
- `DevOpsDemo.IndexerWorker/Infrastructure/MongoClientFactory.cs`
  - creates a MongoDB client with retry writes and database resolution

---

## Request Flow

1. Client calls the API.
2. `DevOpsDemo/Program.cs` routes requests to controllers.
3. `ProductsController` delegates to application services.
4. `ProductService` uses `ProductRepository` to perform MongoDB CRUD, filtering, and aggregations.
5. `ProductSearchService` executes Elasticsearch queries for the `api/products/elasticsearch` endpoint.
6. Responses are mapped to DTOs and returned to the client.

### Key API paths

- `GET /api/products` - paged product list from MongoDB
- `GET /api/products/GetPagedWithCount` - paged result plus total count from MongoDB
- `GET /api/products/{id}` - get single product from MongoDB
- `POST /api/products` - create product in MongoDB
- `PUT /api/products/{id}` - update product in MongoDB
- `DELETE /api/products/{id}` - delete product from MongoDB
- `GET /api/products/search` - MongoDB text/filter search
- `GET /api/products/elasticsearch` - Elasticsearch search with faceting and highlighting
- `GET /api/products/aggregations` - MongoDB aggregation results
- `GET /api/products/GetProductsAndDiscounts` - product/discount join from MongoDB

---

## Background Processing Flow

### Elasticsearch bootstrap

- `ElasticBootstrapService` runs at worker startup.
- It calls `IElasticIndexService.EnsureIndexAsync()`.
- It creates or verifies the physical index and alias.
- On success, it sets worker readiness.

### Full index sync

- `FullReindexWorker` runs if `WorkerSettings.FullReindexOnStartup` is true.
- It pages over the MongoDB `products` collection and sends idempotent bulk upserts to Elasticsearch.
- It uses `_id` ordering for paging and a simple retry policy.

### Change stream synchronization

- `ChangeStreamWorker` watches MongoDB change streams on the configured `products` collection.
- For insert/update/replace events it upserts the full document to Elasticsearch.
- For delete events it deletes the corresponding document from Elasticsearch.
- It persists resume tokens into a checkpoint collection, enabling restart/resume.

---

## MongoDB Usage

### Collections used

- `products` - product documents
- `sales` - sales transaction documents seeded for reporting
- `discounts` - discount documents used by the product-discount join
- `search_sync_checkpoints` - change stream resume checkpoint storage

### MongoDB responsibilities

- Primary source of truth for product and sales data
- Payload storage for `Create`, `Read`, `Update`, `Delete`
- Filtered queries and aggregations
- Seed data generation in `DatabaseSeeder`
- Change stream source for Elasticsearch synchronization

### MongoDB access patterns

- `ProductRepository` uses `IMongoCollection<ProductEntity>` and text indexes.
- `ProductAndDiscountRepository` uses aggregation pipelines with `$lookup` and `$unionWith`.
- `SalesRepository` runs analytic pipelines across `sales` and `products`.

---

## Elasticsearch Usage

### Purpose

- Search,
- autocomplete,
- filtering,
- faceting,
- relevance-based query.

### Elasticsearch responsibilities

- Index product documents for search
- Support alias-based queries using `products_current`
- Provide aggregations and highlights for search responses

### Core implementation

- `ElasticIndexService` builds index settings and mappings
- `ProductSearchService` performs search queries against the alias
- `FullReindexWorker` populates Elasticsearch from MongoDB
- `ChangeStreamWorker` keeps Elasticsearch in sync with MongoDB updates

### Index details

- Default index name: `products_v1`
- Alias: `products_current`
- Fields mapped:
  - `Name` with standard text, keyword, autocomplete
  - `Description` as full text
  - `Category` as keyword
  - `Price` as number
  - `CreatedAt` as date

---

## Configuration Files

### Source configuration

- `DevOpsDemo/appsettings.json`
- `DevOpsDemo/appsettings.Development.json` (not committed, template exists)
- `DevOpsDemo/appsettings.Docker.json` (not committed, template exists)
- `DevOpsDemo.IndexerWorker/appsettings.json`
- `DevOpsDemo.IndexerWorker/appsettings.Docker.json` (not committed, template exists)

### Templates

- `DevOpsDemo/ConfigTemplates/appsettings.Development.json.template`
- `DevOpsDemo/ConfigTemplates/appsettings.Docker.json.template`
- `DevOpsDemo.IndexerWorker/ConfigTemplates/appsettings.Docker.json.template`

### Configuration sections

- `MongoDbWebApi` - API MongoDB connection for the `DevOpsDemo` project
- `ElasticSearchWebApi` - API Elasticsearch connection for the `DevOpsDemo` project
- `MongoDbIndexer` - worker MongoDB connection
- `ElasticSearchIndexer` - worker Elasticsearch connection
- `WorkerIndexer` - worker runtime options and checkpoint settings

### Environment variables

The docker-compose files and Kubernetes manifest wire configuration through environment variables, including:

- `MongoDbWebApi__ConnectionString`
- `MongoDbWebApi__DatabaseName`
- `MongoDbWebApi__CollectionName`
- `ElasticSearchWebApi__NodeUrl`
- `ElasticSearchWebApi__Username`
- `ElasticSearchWebApi__Password`
- `ElasticSearchWebApi__IndexName`
- `ElasticSearchWebApi__IndexAlias`

Worker-specific values are similarly configured for `MongoDbIndexer` and `ElasticSearchIndexer`.

---

## Deployment Artifacts

### Docker

- `DevOpsDemo/Dockerfile`
- `DevOpsDemo.IndexerWorker/Dockerfile`
- `docker-compose.dotnetapi.yml`
- `docker-compose.dotnetindexworker.yml`
- `docker-compose.mongodb.yml`
- `docker-compose.elasticsearch.yml`

### Kubernetes

- `DevOpsDemo/k8s/deployment.yaml`

### Scripts

- `bootstrap-infra.ps1`
- `delete-docker.ps1`
- `run-all.ps1`
- `ci/deploy-api.sh`
- `ci/deploy-indexer.sh`

---

## Dependency Diagram

The repository follows a cleaned layered dependency structure:

- `DevOpsDemo` depends on `DevOpsDemo.Application` and `DevOpsDemo.Infrastructure`
- `DevOpsDemo.Application` depends on `DevOpsDemo.Infrastructure`, `DevOpsDemo.Domain`, and `DevOpsDemo.Interfaces`
- `DevOpsDemo.Infrastructure` depends on MongoDB client, Elasticsearch client, and the domain model layer
- `DevOpsDemo.IndexerWorker` depends on `DevOpsDemo.Infrastructure` and the worker-specific hosted services

See `component-diagram.mmd` for the rendered architecture diagram.

---

## View the Diagram

Open `SYSTEM_OVERVIEW.html` in your browser to view the component diagram as a webpage.
