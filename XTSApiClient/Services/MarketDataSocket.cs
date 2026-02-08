using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XTSApiClient.Models;

namespace XTSApiClient.Services
{
    public class MarketDataSocket : IDisposable
    {
        private readonly string _socketUrl;
        private readonly string _token;
        private readonly string _userId;

        private CancellationTokenSource? _cts;

        public bool IsConnected { get; private set; }

        public event Action? OnConnected;
        public event Action<string>? OnTouchlineData;
        public event Action<string>? OnMarketDepthData;
        public event Action<string>? OnError;

        public MarketDataSocket(string socketUrl, string token, string userId)
        {
            _socketUrl = socketUrl;
            _token = token;
            _userId = userId;
        }

        public Task ConnectAsync()
        {
            IsConnected = true;
            _cts = new CancellationTokenSource();

            Console.WriteLine($"[Socket] Connected (sandbox) to {_socketUrl}");
            OnConnected?.Invoke();

            return Task.CompletedTask;
        }

        public async Task SubscribeAsync(List<Instrument> instruments)
        {
            if (!IsConnected || _cts == null)
            {
                OnError?.Invoke("Socket not connected");
                return;
            }

            Console.WriteLine("[Socket] Subscribed to instruments:");

            foreach (var inst in instruments)
            {
                Console.WriteLine(
                    $"  → Segment: {inst.ExchangeSegment}, ID: {inst.ExchangeInstrumentID}"
                );
            }

            _ = Task.Run(async () =>
            {
                int tick = 1;

                while (!_cts.IsCancellationRequested && tick <= 10)
                {
                    OnTouchlineData?.Invoke(
                        $"NIFTY | Price: {22500 + tick * 5} | Time: {DateTime.Now:T}"
                    );

                    OnMarketDepthData?.Invoke(
                        $"Depth snapshot #{tick}"
                    );

                    tick++;
                    await Task.Delay(1000);
                }
            });
        }

        public Task UnsubscribeAsync(List<Instrument> instruments)
        {
            Console.WriteLine("[Socket] Unsubscribed from instruments");
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            _cts?.Cancel();
            IsConnected = false;

            Console.WriteLine("[Socket] Disconnected (sandbox)");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts?.Cancel();
        }
    }
}
