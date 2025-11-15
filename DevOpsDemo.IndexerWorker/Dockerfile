# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only Worker project files
COPY DevOpsDemo.IndexerWorker/*.csproj ./DevOpsDemo.IndexerWorker/
COPY DevOpsDemo.IndexerWorker/*.json ./DevOpsDemo.IndexerWorker/
COPY DevOpsDemo.IndexerWorker/ ./DevOpsDemo.IndexerWorker/

WORKDIR /src/DevOpsDemo.IndexerWorker
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copy published Worker project
COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "DevOpsDemo.IndexerWorker.dll" ]
