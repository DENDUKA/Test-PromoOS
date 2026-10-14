# TaskApi

Сервис задач: .NET 9 Minimal API + PostgreSQL (EF Core) + RabbitMQ (только publish).
При завершении задачи в exchange `task.events` уходит событие `task.completed`.

## Запуск

Нужен только Docker (с Compose v2). .NET SDK на машине не требуется.

```bash
docker compose up --build
```

| Что | Где |
|---|---|
| API | http://localhost:8080 |
| Health | http://localhost:8080/health |
| RabbitMQ Management | http://localhost:15672 (taskapi / taskapi) |

PostgreSQL и AMQP-порт RabbitMQ наружу **не публикуются** (доступны только внутри сети compose), поэтому уже занятые на машине
5432/5672 запуску не мешают. Если заняты 8080 или 15672 — поменяйте порты, не трогая файлы:

```bash
API_PORT=8081 RABBITMQ_MANAGEMENT_PORT=15673 docker compose up --build
```

Логины/пароли в `docker-compose.yml` — локальные значения по умолчанию для демо; переопределяются через
`.env` (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`).

### Без Docker для приложения

Нужны .NET 9 SDK и локальные PostgreSQL и RabbitMQ:

```bash
dotnet run --project src/TaskApi
```

Настройки берутся из `appsettings.Development.json`: PostgreSQL `postgres/postgres` на `localhost:5432`, RabbitMQ `guest/guest`.
Важно: RabbitMQ пускает `guest` только с localhost, поэтому к брокеру из `docker compose` с этими данными подключиться **нельзя**
(`ACCESS_REFUSED`). Для такого брокера задайте пользователя из compose:

```bash
RabbitMq__UserName=taskapi RabbitMq__Password=taskapi dotnet run --project src/TaskApi
```

## API

| Метод | Путь | Ответы |
|---|---|---|
| POST | `/tasks` | `201`, `400` (пустой/пробельный/длиннее 200 Title, неизвестный Priority, битый JSON) |
| GET | `/tasks` | `200` — **все** задачи. Бонус: `?page&pageSize`. `400`, если `page` < 1 или `pageSize` вне 1..200 |
| PUT | `/tasks/{id}/complete` | `200`, `404`, `409` (уже завершена или завершается параллельным запросом) |
| DELETE | `/tasks/{id}` | `204`, `404` (удаление жёсткое) |

Все ошибки — RFC 7807 `application/problem+json`.

```bash
curl -X POST localhost:8080/tasks -H 'Content-Type: application/json' -d '{"title":"Buy milk","priority":"High"}'
curl -X PUT  localhost:8080/tasks/<id>/complete
```

Сообщение в RabbitMQ (`task.events`, routing key `task.completed`):

```json
{ "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "title": "Buy milk", "completedAt": "2026-05-07T12:00:00Z", "priority": "High" }
```

## Структура (Vertical Slice Architecture)

```
src/TaskApi/
├── Program.cs                     точка входа
├── Startup.cs                     ConfigureServices (DI) и Configure (конвейер запросов)
├── Features/                      вертикальные срезы: папка на операцию, один тип на файл
│   ├── DependencyInjection.cs     регистрация валидаторов и хендлеров
│   └── Tasks/
│       ├── CreateTask/            CreateTaskCommand, CreateTaskCommandValidator, CreateTaskHandler
│       ├── GetTasks/              GetTasksQuery (пагинация), GetTasksQueryValidator, GetTasksHandler
│       ├── CompleteTask/          CompleteTaskHandler, TaskCompletedIntegrationEvent (событие RabbitMQ)
│       ├── DeleteTask/            DeleteTaskHandler
│       ├── TaskDto.cs             модель представления
│       └── TaskEndpoints.cs       маршруты группы /tasks
├── Domain/                        TaskItem, Priority, TaskService (доменная логика), TaskAlreadyCompletedException
├── Infrastructure/
│   ├── Database/                  ITaskRepository
│   ├── Persistence/EntityFramework/ EF Core: DbContext, TaskEntity, репозиторий, миграции
│   ├── RabbitMq/                  очередь в памяти, публикатор RabbitMQ, graceful shutdown
│   └── DependencyInjection.cs     регистрация инфраструктуры
├── Common/                        Error, ProblemDetails, GlobalExceptionHandler, валидация
└── tests/TaskApi.Tests/           интеграционные тесты на реальных PostgreSQL и RabbitMQ
```

## Ключевые решения

- **`CompletedAt` только при завершении.** `TaskItem.Create` его не трогает; `Complete()` ставит время из `TimeProvider` и
  кидает исключение, если задача уже завершена.
- **Валидация запросов — FluentValidation.** `CreateTaskCommandValidator` и `GetTasksQueryValidator` лежат в `Features/Tasks/` рядом с
  хендлерами, регистрируются через `AddValidatorsFromAssemblyContaining`. Хендлер вызывает валидатор первым делом; нарушения по всем
  полям собираются разом и уходят как `400 application/problem+json` (`code: validation.failed`, `errors: { title: [...] }`).
- **Домен отделён от EF.** `TaskItem` — доменный объект без следов хранилища; таблицу описывает отдельная `TaskEntity`. Репозиторий
  преобразует одно в другое и перед сохранением переносит изменения домена в отслеживаемую запись, а её исходный `xmin`
  даёт оптимистичную блокировку (в домене `Version` нет).
- **Title.** Валидируется в `CreateTaskCommandValidator`: проверяется, что строка не пустая, не состоит из одних пробелов и после trim
  не превышает 200 символов (константа `TaskItem.MaxTitleLength = 200`). В БД сохраняется уже обрезанное (trim) строковое значение.
- **Конкурентность (409).** `Version` = системная колонка PostgreSQL `xmin` (`IsRowVersion()`), отдельная колонка и блокировки не нужны.
  Два одновременных `complete`: либо второй видит уже завершённую задачу, либо `UPDATE ... WHERE xmin = @old` не находит строку →
  `DbUpdateConcurrencyException` → 409. Покрыто тестом на 5 параллельных запросов (ровно один 200, остальные 409, одно сообщение).
- **Сообщение уходит строго после коммита.** Handler: загрузить → `Complete` → `SaveChanges` → только потом `Publish`.
- **Публикация не блокирует запрос.** `Publish` — `TryWrite` в ограниченный `Channel` (1000), отправляет фоновый сервис. Без publisher confirms.
- **RabbitMQ недоступен → задача всё равно завершена**, ошибка и полный payload сообщения пишутся в лог (`Failed to publish ... Payload: {...}`).
  После неудачного подключения новые попытки не делаются `ReconnectCooldownSeconds` (5 с), чтобы не тормозить очередь таймаутами.
- **Graceful shutdown.** `StopAsync` публикатора: закрыть очередь → дать `ShutdownDrainTimeoutSeconds` (5 с) вычитать и отправить
  уже поставленные события → оборвать остаток (он логируется) → закрыть канал и соединение. `HostOptions.ShutdownTimeout` = 10 с,
  `stop_grace_period` в compose = 15 с. Kestrel останавливается раньше публикатора (веб-сервер регистрируется
  последним, а хост стопает сервисы в обратном порядке), так что новые запросы уже не поступают. Проверено: в логе запуска
  `Database migrations applied` идёт раньше `Now listening`, а при `docker compose stop api` остановка занимает ~2 с с записью
  `RabbitMQ connection closed`.
- **Формат времени.** `completedAt` в сообщении всегда UTC с суффиксом `Z`, как в задании (`System.Text.Json` по умолчанию
  писал бы `+00:00`; для этого в `MessagingJson` есть отдельный конвертер).
- **Priority** хранится в БД числом, в JSON — строкой; числа в запросе отклоняются.
- **`/health`**: недоступная БД → `503`, недоступный RabbitMQ → `Degraded` (`200`): сервис работает и без брокера.
- **Миграции** применяются при старте приложения (всегда) — удобно для `docker compose up`.

## Тесты

Интеграционным тестам нужны реальные PostgreSQL и RabbitMQ. Два способа, оба требуют только Docker:

**1. В контейнере, без .NET SDK на машине** (PostgreSQL и RabbitMQ берутся из compose, отдельная БД `taskapi_tests`):

```bash
docker compose --profile tests run --rm tests
```

**2. С хоста** (нужен .NET 9 SDK; Testcontainers сам поднимает PostgreSQL и RabbitMQ, при первом запуске скачиваются образы):

```bash
dotnet test
```

| Набор | Что проверяет |
|---|---|
| `CompleteTaskIntegrationTest` | создать → завершить → сообщение с нужным форматом пришло в RabbitMQ (тест из задания) |
| `ConcurrencyTests` | 5 параллельных `complete` → один 200, четыре 409, одно сообщение |
| `ValidationTests` | пустой/пробельный/длинный Title → 400 (имена полей в camelCase), trim, неизвестный Priority → 400, дефолты, `CompletedAt` = null |
| `RabbitUnavailableTests` | брокер лежит → 200, задача завершена, ошибка с payload в логе; `/health` |
| `GracefulShutdownTests` | остановка публикатора досылает очередь; при лежащем брокере не зависает и логирует недоставленное |
| `TasksCrudTests` | GET без параметров возвращает все в порядке создания, пагинация и её границы, сохранение всех полей в БД, DELETE, 404 |

## Где срезал углы и что сделал бы дальше

- **HACK: потеря события при падении процесса** между коммитом и публикацией (событие живёт в памяти). Задание допускает
  «fail silently с логом». Дальше — transactional outbox: событие пишется в таблицу в той же транзакции, публикатор читает её и ретраит.
- **HACK: без publisher confirms** — «ушло в сокет» не значит «принято брокером». Дальше — confirms + ретраи (Polly).
- **HACK: миграции при старте приложения** — в проде отдельным шагом деплоя.
