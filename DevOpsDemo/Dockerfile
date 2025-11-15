# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files and solution file
COPY DevOpsDemo.sln ./
COPY DevOpsDemo/*.csproj ./DevOpsDemo/
COPY DevOpsDemo.Tests/*.csproj ./DevOpsDemo.Tests/
COPY DevOpsDemo.Application/*.csproj ./DevOpsDemo.Application/
COPY DevOpsDemo.Domain/*.csproj ./DevOpsDemo.Domain/
COPY DevOpsDemo.Infrastructure/*.csproj ./DevOpsDemo.Infrastructure/
COPY DevOpsDemo.Infrastructure.Tests/*.csproj ./DevOpsDemo.Infrastructure.Tests/
COPY DevOpsDemo.MongoPlayground/*.csproj ./DevOpsDemo.MongoPlayground/

# Restore solution
RUN dotnet restore

# Copy all source code
COPY DevOpsDemo/ ./DevOpsDemo/
COPY DevOpsDemo.Application/ ./DevOpsDemo.Application/
COPY DevOpsDemo.Domain/ ./DevOpsDemo.Domain/
COPY DevOpsDemo.Infrastructure/ ./DevOpsDemo.Infrastructure/
COPY DevOpsDemo.IndexerWorker/ ./DevOpsDemo.IndexerWorker/

# Publish API project
WORKDIR /src/DevOpsDemo
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "DevOpsDemo.dll" ]
