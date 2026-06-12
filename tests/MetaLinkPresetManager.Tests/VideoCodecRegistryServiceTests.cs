using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class VideoCodecRegistryServiceTests
{
    [Theory]
    [InlineData(VideoCodecMode.H264, 0)]
    [InlineData(VideoCodecMode.H265, 1)]
    public void Apply_MapsCodecToHevcDword(
        VideoCodecMode codec,
        int expected)
    {
        var store = new FakeRegistryValueStore();
        var logger = new TestLogger();
        var service = new VideoCodecRegistryService(store, logger);

        service.Apply(codec);

        Assert.Equal(expected, store.DwordValue);
        Assert.False(store.DeleteCalled);
        Assert.Contains(
            logger.Messages,
            message => message.Contains(
                "registry-backed Video Codec",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_SystemDefaultDeletesOverride()
    {
        var store = new FakeRegistryValueStore();
        var service = new VideoCodecRegistryService(store, new TestLogger());

        service.Apply(VideoCodecMode.Default);

        Assert.True(store.DeleteCalled);
        Assert.Null(store.DwordValue);
    }

    [Fact]
    public void Restore_PassesCapturedValueBackToStore()
    {
        var expected = new RegistryValueSnapshot(
            true,
            1,
            4);
        var store = new FakeRegistryValueStore { Snapshot = expected };
        var service = new VideoCodecRegistryService(store, new TestLogger());

        var snapshot = service.Capture();
        service.Restore(snapshot);

        Assert.Same(expected, store.RestoredSnapshot);
    }

    [Fact]
    public void Apply_Av1IsRejected()
    {
        var service = new VideoCodecRegistryService(
            new FakeRegistryValueStore(),
            new TestLogger());

        Assert.Throws<NotSupportedException>(
            () => service.Apply(VideoCodecMode.Av1));
    }

    private sealed class FakeRegistryValueStore : IRegistryValueStore
    {
        public RegistryValueSnapshot Snapshot { get; set; } =
            new(false, null, -1);
        public RegistryValueSnapshot? RestoredSnapshot { get; private set; }
        public int? DwordValue { get; private set; }
        public bool DeleteCalled { get; private set; }

        public RegistryValueSnapshot Capture()
        {
            return Snapshot;
        }

        public void SetDword(int value)
        {
            DwordValue = value;
            DeleteCalled = false;
        }

        public void Delete()
        {
            DwordValue = null;
            DeleteCalled = true;
        }

        public void Restore(RegistryValueSnapshot snapshot)
        {
            RestoredSnapshot = snapshot;
        }
    }
}
