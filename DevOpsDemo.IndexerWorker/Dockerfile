# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all dependent projects
COPY DevOpsDemo.Worker.sln ./
COPY DevOpsDemo.Application/*.csproj ./DevOpsDemo.Application/
COPY DevOpsDemo.Domain/*.csproj ./DevOpsDemo.Domain/
COPY DevOpsDemo.Infrastructure/*.csproj ./DevOpsDemo.Infrastructure/
COPY DevOpsDemo.IndexerWorker/*.csproj ./DevOpsDemo.IndexerWorker/

# Restore solution
RUN dotnet restore DevOpsDemo.Worker.sln

# Copy all source code
COPY DevOpsDemo.Application/ ./DevOpsDemo.Application/
COPY DevOpsDemo.Domain/ ./DevOpsDemo.Domain/
COPY DevOpsDemo.Infrastructure/ ./DevOpsDemo.Infrastructure/
COPY DevOpsDemo.IndexerWorker/ ./DevOpsDemo.IndexerWorker/

# Publish IndexerWorker project
WORKDIR /src/DevOpsDemo.IndexerWorker
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "DevOpsDemo.IndexerWorker.dll" ]
