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

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "DevOpsDemo.IndexerWorker.dll" ]
