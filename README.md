# GrpcPing — gRPC Port Connectivity Checker

A small WPF desktop app (Windows, .NET 8) that checks the availability of a gRPC service by host and port. The check uses the standard gRPC Health Check API; TLS is optional. The app also includes a minimal local test gRPC server so you can quickly “ping” a port without external dependencies.

## Features
- Check connectivity to `host:port` over gRPC (HTTP/2), choose `http`/`https`.
- Use gRPC Health Check API:
  - If the service returns `SERVING`, the connection is considered successful.
  - If the Health service is not implemented (`UNIMPLEMENTED`), the transport connection is still considered established (the channel opens but there is no health status).
- Display connection status and last error (including `RpcException` code/details).
- Built-in test server (Kestrel + gRPC Health Checks), starts on `127.0.0.1:<port>` with one click.

## Requirements
- Windows 10/11.
- .NET SDK 8.0 or Visual Studio 2022 with .NET 8 support.

## Build and Run
- Using command line:
  - `dotnet build GrpcPing.sln`
  - `dotnet run --project GrpcPing/GrpcPingWpf.csproj`
- Using Visual Studio:
  - Open `GrpcPing.sln` and run the `GrpcPingWpf` configuration.

Executable after build: `GrpcPing/bin/Debug/net8.0-windows/GrpcPingWpf.exe` (or `Release` depending on configuration).

## Usage
1. Enter `Address` (e.g., `127.0.0.1` or `server.domain`) and `Port`.
2. Enable `Use TLS (https)` if needed.
3. Click `Check connection` — the app will:
   - create a gRPC channel to `http(s)://<Address>:<Port>`,
   - call `grpc.health.v1.Health/Check` with ~2s deadline,
   - display the result: `Connected (SERVING)`, another Health status, or an error.
4. For a quick local check without an external service, use `Start test server (<Port>)`:
   - starts local Kestrel at `127.0.0.1:<Port>` with HTTP/2 and Health endpoint,
   - the button toggles start/stop; the status line shows the current port.

Notes:
- When TLS is enabled, client certificate validation is deliberately disabled for convenience in local testing. Use only in non-production environments.
- The test server runs without TLS by default (h2c). If you enable TLS in server code, you will need a development certificate.

## Architecture
- UI: `MainWindow.xaml` / `MainWindow.xaml.cs`.
- Logic: `MainViewModel` — creates `GrpcChannel`, performs Health request, manages statuses.
- Test server: `TestServerManager` — minimal ASP.NET Core/Kestrel with `AddGrpc()` and `AddGrpcHealthChecks()`.
- Target framework and packages — see `GrpcPing/GrpcPingWpf.csproj`.

## Known Quirks
- If the Health service is not implemented by the server, the app shows “Connected (Health service is not implemented)” because the gRPC transport channel is established.
- The Health check deadline is short (~2 seconds) for fast diagnostics, which may be tight for slow networks.

## License
MIT License

-----------

# GrpcPing — проверка подключения к gRPC-порту

Небольшое WPF‑приложение (Windows, .NET 8), которое проверяет доступность gRPC‑сервиса по адресу и порту. Проверка выполняется через стандартный gRPC Health Check API; при желании можно использовать TLS. В приложение встроен простой локальный тестовый сервер gRPC, чтобы быстро «попинговать» порт без внешних зависимостей.

## Возможности
- Проверка соединения к `host:port` по gRPC (HTTP/2), с выбором `http`/`https`.
- Использование gRPC Health Check API:
  - Если сервис отвечает `SERVING` — соединение считается успешным.
  - Если Health‑сервис не реализован (`UNIMPLEMENTED`) — соединение также считается установленным (канал открыт, но без статуса здоровья).
- Отображение статуса и последней ошибки (в т.ч. кода/деталей `RpcException`).
- Встроенный тестовый сервер (Kestrel + gRPC Health Checks), поднимается на `127.0.0.1:<порт>` одной кнопкой.

## Требования
- Windows 10/11.
- .NET SDK 8.0 или Visual Studio 2022 с поддержкой .NET 8.

## Сборка и запуск
- Через командную строку:
  - `dotnet build GrpcPing.sln`
  - `dotnet run --project GrpcPing/GrpcPingWpf.csproj`
- Через Visual Studio:
  - Откройте `GrpcPing.sln` и запустите конфигурацию `GrpcPingWpf`.

Исполняемый файл после сборки: `GrpcPing/bin/Debug/net8.0-windows/GrpcPingWpf.exe` (или `Release` при соответствующей конфигурации).

## Использование
1. Введите `Address` (например, `127.0.0.1` или `server.domain`) и `Port`.
2. При необходимости включите `Use TLS (https)`.
3. Нажмите `Check connection` — приложение:
   - создаст gRPC‑канал к `http(s)://<Address>:<Port>`,
   - вызовет `grpc.health.v1.Health/Check` с дедлайном ~2 секунды,
   - отобразит результат: `Connected (SERVING)`, иной статус Health, либо ошибку.
4. Для быстрой проверки без внешнего сервиса воспользуйтесь `Start test server (<Port>)`:
   - поднимет локальный Kestrel на `127.0.0.1:<Port>` с HTTP/2 и Health‑эндпоинтом,
   - кнопка переключает запуск/остановку; строка состояния покажет текущий порт.

Примечания:
- При включённом TLS клиент намеренно отключает проверку сертификата (для удобства локального теста). Используйте только в тестовой среде.
- Тестовый сервер по умолчанию стартует без TLS (h2c). Если включить TLS в коде сервера, понадобится dev‑сертификат.

## Архитектура
- UI: `MainWindow.xaml`/`MainWindow.xaml.cs`.
- Логика: `MainViewModel` — формирует канал `GrpcChannel`, выполняет Health‑запрос, управляет статусами.
- Тестовый сервер: `TestServerManager` — минимальный ASP.NET Core/Kestrel с `AddGrpc()` и `AddGrpcHealthChecks()`.
- Целевой фреймворк и пакеты — смотрите `GrpcPing/GrpcPingWpf.csproj`.

## Известные особенности
- Если Health‑сервис не реализован на стороне сервера, соединение помечается как «Connected (Health service is not implemented)», так как установлена транспортная связность gRPC.
- Таймаут Health‑проверки короткий (порядка 2 секунд), что удобно для быстрой диагностики, но может быть недостаточно для медленных сетей.

## Лицензирование
MIT License
