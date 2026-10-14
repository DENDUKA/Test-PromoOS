# Запуск тестов без локального .NET SDK (нужен только Docker):
#   docker compose --profile tests run --rm tests
# PostgreSQL и RabbitMQ берутся из docker-compose.yml (адреса передаются через TASKAPI_TEST_* переменные),
# поэтому Docker-in-Docker и доступ к docker.sock не нужны.

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /src

# Сначала манифесты: слой с restore кэшируется, пока не менялись зависимости
COPY Directory.Build.props Directory.Packages.props ./
COPY src/TaskApi/TaskApi.csproj src/TaskApi/
COPY tests/TaskApi.Tests/TaskApi.Tests.csproj tests/TaskApi.Tests/
RUN dotnet restore tests/TaskApi.Tests/TaskApi.Tests.csproj

COPY src/ src/
COPY tests/ tests/

ENTRYPOINT ["dotnet", "test", "tests/TaskApi.Tests/TaskApi.Tests.csproj", "--no-restore"]
