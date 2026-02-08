# XTS API Client (C#)

A small, focused **.NET console application** that demonstrates how to:

- Authenticate to the **XTS Market Data API** (token-based auth)
- Download **OHLC** (Open/High/Low/Close/Volume/OI) candles
- Stream market-data updates via a **WebSocket layer (sandbox/demo implementation)**

This repository is intentionally kept minimal: it provides a clean baseline client (`XTSClient`) + models + a runnable demo (`Program.cs`).

---

## What’s implemented (current code)

- **Market Data Login**
  - Calls `POST /apimarketdata/auth/login`
  - Stores `Token` and `UserID`
  - Automatically attaches `Authorization: <token>` for subsequent requests

- **OHLC download**
  - Calls `GET /apimarketdata/instruments/ohlc`
  - Parses both response shapes seen in the API:
    - `Result.DataReponse` as **CSV string** (`"timestamp|open|high|low|close|volume|oi"`)
    - `Result.DataReponse` as a **JSON array of arrays**

- **WebSocket streaming (sandbox)**
  - `MarketDataSocket` currently **simulates** streaming events to demonstrate the event-driven API.
  - It does **not** perform a real Socket.IO/WebSocket connection yet (even though `SocketIOClient` is referenced).

---

## Project structure

```text
 .
 ├── README.md
 ├── .gitignore
 └── XTSClient/
     ├── Models/
     │   ├── InstrumentModels.cs
     │   ├── LoginModels.cs
     │   └── OHLCModels.cs
     ├── Services/
     │   ├── XTSClient.cs
     │   └── MarketDataSocket.cs
     ├── Program.cs
     ├── XTSApiClient.csproj
     ├── bin/
     └── obj/
```

- **`Models/`**
  - DTOs for API responses and basic domain objects
- **`Services/XTSClient.cs`**
  - REST client wrapper for login + OHLC
- **`Services/MarketDataSocket.cs`**
  - Sandbox socket abstraction that raises events (`OnTouchlineData`, `OnMarketDepthData`, ...)
- **`Program.cs`**
  - End-to-end runnable demo (login -> OHLC -> streaming)

---

## Prerequisites

- **.NET SDK** matching the project target framework (`net10.0` in `XTSApiClient.csproj`).
- Valid **XTS API credentials**:
  - `API_KEY`
  - `SECRET_KEY`
- Network access to your configured base URL.

---

## Build & run

From the repository root:

```bash
dotnet restore
dotnet run --project .\XTSApiClient\XTSApiClient.csproj
```

---

## Configuration

Configuration is currently done via **constants in `Program.cs`**:

```csharp
private const string BASE_URL = "https://xts.rmoneyindia.co.in:3000";
private const string SOCKET_URL = "https://xts.rmoneyindia.co.in:3000";
private const string API_KEY = "YOUR_API_KEY";
private const string SECRET_KEY = "YOUR_SECRET_KEY";
```

### Security note

- **Do not commit real credentials**.
- Prefer loading secrets from environment variables or user-secrets for real projects. (Not implemented yet in this repository.)

---

## Usage (library-style)

Even though this repo is a console app, the key pieces are reusable.

### 1) Login

```csharp
using var client = new XTSClient(baseUrl, apiKey, secretKey);
var login = await client.MarketDataLoginAsync();

Console.WriteLine(client.UserID);
Console.WriteLine(client.Token);
```

Behavior:

- After login, `XTSClient` sets `Authorization` header on its internal `HttpClient`.
- `XTSClient.IsLoggedIn` becomes `true`.

### 2) Download OHLC candles

```csharp
var endTime = DateTime.Now;
var startTime = endTime.AddMinutes(-30);

var candles = await client.GetOHLCAsync(
    exchangeSegment: XTSClient.NSECM,
    exchangeInstrumentID: 2885,
    startTime: startTime,
    endTime: endTime,
    compressionValue: 60
);
```

Notes:

- `compressionValue` defaults to `60`.
- The request uses formatted time: `"MMM dd yyyy HHmmss"` (escaped into the query string).
- If you call OHLC methods before login, the client throws `InvalidOperationException("Login first")`.

### 3) Demo helper: Top 5 NIFTY 50 OHLC

The console demo also calls:

- `GetTop5Nifty50OHLCAsync(startTime, endTime, compressionValue)`

This method fetches data for:

- RELIANCE (2885)
- HDFCBANK (1333)
- INFY (1594)
- TCS (11536)
- ICICIBANK (4963)

---

## WebSocket streaming (sandbox)

`MarketDataSocket` exposes a small event-driven API:

- `OnConnected`
- `OnTouchlineData` (string payload)
- `OnMarketDepthData` (string payload)
- `OnError`

The current implementation:

- Marks itself connected in `ConnectAsync()`
- Prints subscription requests in `SubscribeAsync(...)`
- Emits **10 ticks** of demo messages once per second
- Stops when disconnected or canceled

### CSV persistence (DATA folder)

When you run the console demo, data is automatically saved into:

- `XTSApiClient/DATA/`

This is handled by `Services/CsvSaver.cs`.

#### OHLC CSV files

- **When created**
  - After OHLC is fetched, the demo saves a CSV per symbol **only if candles are present**.
- **File name pattern**
  - `OHLC_<SYMBOL>_<START>_<END>_C<COMPRESSION>.csv`
  - Example:
    - `OHLC_RELIANCE_20260208_153000_20260208_160000_C60.csv`
- **Columns**
  - `Timestamp,Open,High,Low,Close,Volume,OpenInterest`
  - `Timestamp` is written in ISO-8601 round-trip format.

#### Stream CSV file

- **When created**
  - At the start of the socket demo.
- **File name pattern**
  - `STREAM_<STARTED_AT>.csv`
  - Example:
    - `STREAM_20260208_154810.csv`
- **Columns**
  - `Timestamp,EventType,Message`
- **Events written**
  - `TOUCHLINE`
  - `MARKET_DEPTH`
  - `ERROR`

Example usage (same as `Program.cs` pattern):

```csharp
using var socket = new MarketDataSocket(socketUrl, token, userId);

socket.OnConnected += () => Console.WriteLine("WebSocket connected!");
socket.OnTouchlineData += msg => Console.WriteLine($"[LIVE] {msg}");
socket.OnMarketDepthData += msg => Console.WriteLine($"[DEPTH] {msg}");
socket.OnError += err => Console.WriteLine($"[ERROR] {err}");

await socket.ConnectAsync();
await socket.SubscribeAsync(new List<Instrument>
{
    new Instrument { ExchangeSegment = 1, ExchangeInstrumentID = 2885 },
});

await Task.Delay(TimeSpan.FromSeconds(30));
await socket.UnsubscribeAsync(...);
await socket.DisconnectAsync();
```

---

## Implemented endpoints (REST)

- **Login**
  - `POST /apimarketdata/auth/login`
- **OHLC**
  - `GET /apimarketdata/instruments/ohlc?exchangeSegment=...&exchangeInstrumentID=...&startTime=...&endTime=...&compressionValue=...`

---

## Common issues / troubleshooting

- **401/403 or login failures**
  - Verify `API_KEY` / `SECRET_KEY`
  - Confirm `BASE_URL` is correct for your environment

- **OHLC returns empty list**
  - Check you’re using the correct `exchangeSegment` and `exchangeInstrumentID`
  - Ensure your `startTime`/`endTime` range contains trading data

- **Unexpected parsing errors**
  - OHLC parsing uses `decimal.Parse(...)` / `long.Parse(...)` for CSV responses.
  - If your API returns values with unexpected formatting/culture, you may need to adjust parsing.

---

## Notes for contributors / next steps

Good next improvements (not implemented yet):

- Move secrets out of `Program.cs` into environment variables or `appsettings.json`
- Implement a **real** Socket.IO / WebSocket connection using the already referenced `SocketIOClient` package
- Add additional endpoints (instrument master, quotes, etc.) and typed models
- Add automated tests for OHLC parsing variants
