using FluentAssertions;
using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Application.Options;
using KodiNet.Domain.Enums;
using KodiNet.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace KodiNet.Infrastructure.Tests.Services;

public sealed class PiStatusStoreTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Options with very small intervals to rapidly test.
    /// </summary>
    private static IOptions<PollingOptions> FastOptions(int maxConcurrency = 5) =>
        Options.Create(new PollingOptions
        {
            StatusIntervalSeconds = 1,
            ListRefreshIntervalSeconds = 2,
            MaxConcurrency = maxConcurrency
        });

    private static PiDto MakePi(int id, string ip = "192.168.1.1") =>
        new(id, $"Pi {id}", ip, 1, "Site A", "Salle", "Pi 4B", 8080, "/videos", 22);

    private static PiStatusDto MakeStatus(int piId, PiStatus status) =>
        new(piId, status, "19.0", 10.0, 512_000_000, 45.0, 1_073_741_824);

    // ── StartAsync idempotent ─────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();
        mockPi.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([MakePi(1)]);

        var store = new PiStatusService(mockPi.Object, mockKodi.Object, FastOptions());

        await store.StartAsync();
        await store.StartAsync();   // second call — should not restart
        await store.StartAsync();   // third call

        // GetAllAsync called ONE time even with 3 StartAsync
        mockPi.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once, "StartAsync should be idempotent");

        await store.DisposeAsync();
    }

    // ── StateChanged invoked one time per cycle ───────────────────────────────

    [Fact]
    public async Task PollAll_ShouldInvokeStateChanged_OncePerCycle_NotPerPi()
    {
        var pis = Enumerable.Range(1, 10).Select(i => MakePi(i)).ToList();

        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();

        mockPi.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(pis);
        mockKodi.Setup(s => s.GetStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => MakeStatus(id, PiStatus.Idle));

        var store        = new PiStatusService(mockPi.Object, mockKodi.Object, FastOptions());
        var stateChanges = 0;
        store.StateChanged += () => Interlocked.Increment(ref stateChanges);

        await store.StartAsync();

        // Let one cycle finish
        await Task.Delay(300, TestContext.Current.CancellationToken);
        await store.DisposeAsync();

        // First StartAsync + first poll = 2 maximum
        // (1 for the initial list loading, 1 for the first poll)
        stateChanges.Should().BeLessThanOrEqualTo(3,
            "StateChanged should not be invoked one time per Pi");

        stateChanges.Should().BeGreaterThanOrEqualTo(1,
            "StateChanged should be invoked at least one time");
    }

    // ── MaxConcurrency respecté ───────────────────────────────────────────────

    [Fact]
    public async Task PollAll_ShouldRespectMaxConcurrency()
    {
        const int piCount       = 20;
        const int maxConcurrent = 3;
        var       concurrentNow = 0;
        var       maxObserved   = 0;
        var       lockObj       = new object();

        var pis      = Enumerable.Range(1, piCount).Select(i => MakePi(i)).ToList();
        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();

        mockPi.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(pis);

        mockKodi.Setup(s => s.GetStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(async (int id, CancellationToken ct) =>
                {
                    lock (lockObj)
                    {
                        concurrentNow++;
                        if (concurrentNow > maxObserved)
                            maxObserved = concurrentNow;
                    }
                    await Task.Delay(50, ct);   // simulate network latence
                    lock (lockObj) { concurrentNow--; }
                    return MakeStatus(id, PiStatus.Idle);
                });

        var store = new PiStatusService(mockPi.Object, mockKodi.Object,
                                      FastOptions(maxConcurrency: maxConcurrent));

        await store.StartAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);
        await store.DisposeAsync();

        maxObserved.Should().BeLessThanOrEqualTo(maxConcurrent,
            $"should not have more than {maxConcurrent} interrogated Kodis simultaneously");
    }

    // ── Backoff on offline Pi ─────────────────────────────────────────────────

    [Fact]
    public async Task OfflinePi_ShouldBePolledLessFrequently_AfterRepeatedFailures()
    {
        var pi       = MakePi(1);
        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();
        var callCount = 0;

        mockPi.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([pi]);

        mockKodi.Setup(s => s.GetStatusAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Pi hors ligne"))
                .Callback(() => Interlocked.Increment(ref callCount));

        // Really small interval to quickly call multiple cycles 
        var opts  = Options.Create(new PollingOptions
        {
            StatusIntervalSeconds = 1,
            MaxConcurrency        = 5
        });
        var store = new PiStatusService(mockPi.Object, mockKodi.Object, opts);

        await store.StartAsync();
        await Task.Delay(2500, TestContext.Current.CancellationToken);   // 2.5 cycles with 1s interval
        await store.DisposeAsync();

        // Without backoff : 2 cycles → at least 2 calls
        // With backoff  : after the first failure, the Pi is not polled during ~20s
        //                 → 1 call on a 2.5s window
        callCount.Should().Be(1,
            "the offline Pi should not be re-polled right after a failure");
    }

    // ── RefreshPisAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshPisAsync_ShouldUpdatePiList_AndNotifySubscribers()
    {
        var initialPis = new List<PiDto> { MakePi(1) };
        var updatedPis = new List<PiDto> { MakePi(1), MakePi(2) };

        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();

        mockPi.SetupSequence(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(initialPis)
              .ReturnsAsync(updatedPis);

        var store        = new PiStatusService(mockPi.Object, mockKodi.Object, FastOptions());
        var notifications = new List<DateTime>();
        store.StateChanged += () => notifications.Add(DateTime.UtcNow);

        await store.StartAsync();
        notifications.Clear();   // ignore the initial notification

        await store.RefreshPisAsync();

        store.Pis.Should().HaveCount(2);
        notifications.Should().HaveCount(1,
            "RefreshPisAsync should notify exactly one time");

        await store.DisposeAsync();
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Stats_ShouldReflectCurrentStatusMap()
    {
        var pis      = Enumerable.Range(1, 5).Select(i => MakePi(i)).ToList();
        var mockPi   = new Mock<IRaspberryPiService>();
        var mockKodi = new Mock<IKodiService>();

        mockPi.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(pis);

        mockKodi.Setup(s => s.GetStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => id switch
                {
                    1 => MakeStatus(1, PiStatus.Playing),
                    2 => MakeStatus(2, PiStatus.Playing),
                    3 => MakeStatus(3, PiStatus.Offline),
                    _ => MakeStatus(id, PiStatus.Idle)
                });

        var store = new PiStatusService(mockPi.Object, mockKodi.Object, FastOptions());
        await store.StartAsync();
        await Task.Delay(300, TestContext.Current.CancellationToken);

        store.Stats.Total.Should().Be(5);
        store.Stats.Playing.Should().Be(2);
        store.Stats.Offline.Should().Be(1);

        await store.DisposeAsync();
    }
}