# Контекст сборки — корень репозитория (нужны Directory.Build.props / Directory.Packages.props):
#   docker build -f src/TaskApi/Dockerfile -t task-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Сначала только манифесты: слой с restore кэшируется, пока не менялись зависимости
COPY Directory.Build.props Directory.Packages.props ./
COPY src/TaskApi/TaskApi.csproj src/TaskApi/
RUN dotnet restore src/TaskApi/TaskApi.csproj

COPY src/TaskApi/ src/TaskApi/
RUN dotnet publish src/TaskApi/TaskApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Не root: пользователь app уже есть в образах .NET 8+
USER $APP_UID

ENTRYPOINT ["dotnet", "TaskApi.dll"]
