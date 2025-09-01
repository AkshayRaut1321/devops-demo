# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "DevOpsDemo.dll" ]

# Docker command to run
# # docker build -t devops-demo:local .
# # docker run --rm -p 8080:80 devops-demo:local
# # In another terminal:
# # curl http://localhost:8080/hello
# # => "Hello World from DevOps Demo"