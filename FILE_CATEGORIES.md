# Repository File Categories

## 1. Project / solution orchestration
- `DevOpsDemo.sln`
- `DevOpsDemo.Worker.sln`
- `DevOpsDemo/DevOpsDemo.csproj`
- `DevOpsDemo.Application/DevOpsDemo.Application.csproj`
- `DevOpsDemo.Domain/DevOpsDemo.Domain.csproj`
- `DevOpsDemo.Infrastructure/DevOpsDemo.Infrastructure.csproj`
- `DevOpsDemo.IndexerWorker/DevOpsDemo.IndexerWorker.csproj`
- `DevOpsDemo.Infrastructure.Tests/DevOpsDemo.Infrastructure.Tests.csproj`
- `DevOpsDemo.MongoPlayground/DevOpsDemo.MongoPlayground.csproj`
- `DevOpsDemo.Tests/DevOpsDemo.Tests.csproj`

## 2. Application-related
- `DevOpsDemo/`
  - `Program.cs`
  - `Controllers/`
  - `appsettings.json`, `appsettings.Development.json`, `appsettings.Docker.json`
  - `Properties/launchSettings.json`
  - `k8s/`
  - `DevOpsDemo.http`
  - `ConfigTemplates/`

- `DevOpsDemo.Application/`
  - `ApplicationAutoMapperProfile.cs`
  - `ApplicationServiceExtensions.cs`
  - `DTOs/`
  - `Interfaces/`
  - `Search/`
  - `Services/`

- `DevOpsDemo.Domain/`
  - `Interfaces/`
  - `Models/`

## 3. Infrastructure-related
- `DevOpsDemo.Infrastructure/`
  - `InfrastructureAutoMapperProfile.cs`
  - `InfrastructureServiceExtensions.cs`
  - `DomainImplementation/`
  - `Entities/`
  - `Implementation/`
  - `Interfaces/`
  - `Seed/`

- `DevOpsDemo.IndexerWorker/`
  - `Program.cs`
  - `appsettings.json`, `appsettings.Development.json`, `appsettings.Docker.json`
  - `Properties/launchSettings.json`
  - `k8s/`
  - `Infrastructure/`
  - `Services/`
  - `Entities/`
  - `Logging/`
  - `IndexerReadiness.cs`
  - `ConfigTemplates/`

## 4. Deployment / environment / ops
- Top-level scripts:
  - `bootstrap-infra.ps1`
  - `delete-docker.ps1`
  - `run-all.ps1`
  - `quick-commands.txt`

- Docker compose / container orchestration:
  - `docker-compose.dotnetapi.yml`
  - `docker-compose.dotnetindexworker.yml`
  - `docker-compose.elasticsearch.yml`
  - `docker-compose.mongodb.yml`

- Docker build artifacts:
  - `DevOpsDemo/Dockerfile`
  - `DevOpsDemo.IndexerWorker/Dockerfile`

- GitHub Actions workflows:
  - `.github/workflows/`
  - `.github/workflows/api-ci-cd-aligned-with-bootstrap-ps1.yaml`
  - `.github/workflows/indexer-ci-cd.yaml`
  - `.github/workflows_backup/`
  - `.github/workflows_backup/ci-cd.yaml`

- CI scripts:
  - `ci/deploy-api.sh`
  - `ci/deploy-indexer.sh`

- Kubernetes manifests:
  - `DevOpsDemo/k8s/`
  - `DevOpsDemo/k8s/deployment.yaml`
  - `DevOpsDemo.IndexerWorker/k8s/`
  - `DevOpsDemo.IndexerWorker/k8s/worker-deployment.yaml`

## 5. Tests and validation
- `DevOpsDemo.Tests/`
  - `HelloTests.cs`
  - `UnitTest1.cs`

- `DevOpsDemo.Infrastructure.Tests/`
  - `MongoTestBase.cs`
  - `Repositories/`

## 6. Playground / experimentation
- `DevOpsDemo.MongoPlayground/`
  - `Program.cs`
  - `Playground/`

## 7. Documentation / diagrams / overview
- `SYSTEM_OVERVIEW.md`
- `SYSTEM_OVERVIEW.html`
- `component-diagram.mmd`

## 8. Tooling / workspace
- `.gitignore`
- `.dockerignore`
- `.github/`
- `.vscode/`
- `.vs/`

## Dependency grouping summary
- `Application related`:
  - `DevOpsDemo/`
  - `DevOpsDemo.Application/`
  - `DevOpsDemo.Domain/`

- `Infrastructure related`:
  - `DevOpsDemo.Infrastructure/`
  - `DevOpsDemo.IndexerWorker/`

- `Deployment / ops`:
  - top-level scripts
  - `docker-compose.*`
  - `ci/`
  - `k8s/`

- `Testing`:
  - `DevOpsDemo.Tests/`
  - `DevOpsDemo.Infrastructure.Tests/`

- `Experiment / playground`:
  - `DevOpsDemo.MongoPlayground/`

- `Docs / architecture`:
  - `SYSTEM_OVERVIEW*`
  - `component-diagram.mmd`
