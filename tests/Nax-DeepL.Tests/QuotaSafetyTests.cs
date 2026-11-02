namespace NaxDeepL.Tests;

/// <summary>Guards against the app using up the DeepL character quota without the user noticing.</summary>
public class QuotaSafetyTests
{
    static readonly DateTime T0 = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ctrl_c_pressed_twice_quickly_fires_hotkey_once()
    {
        var d = new CtrlCCDetector();
        Assert.False(d.OnCDown(ctrlHeld: true, T0));
        d.OnCUp();
        Assert.True(d.OnCDown(ctrlHeld: true, T0.AddMilliseconds(200)));
    }

    [Fact]
    public void holding_ctrl_c_with_key_repeat_never_fires_hotkey()
    {
        var d = new CtrlCCDetector();
        var fired = 0;
        // Windows repeats the key-down about every 33 ms while the key is held.
        for (var i = 0; i < 60; i++)
            if (d.OnCDown(ctrlHeld: true, T0.AddMilliseconds(i * 33))) fired++;
        Assert.Equal(0, fired);
    }

    [Fact]
    public void holding_after_hotkey_fired_does_not_fire_again()
    {
        var d = new CtrlCCDetector();
        d.OnCDown(true, T0); d.OnCUp();
        Assert.True(d.OnCDown(true, T0.AddMilliseconds(150)));
        for (var i = 1; i < 30; i++)
            Assert.False(d.OnCDown(true, T0.AddMilliseconds(150 + i * 33)));
    }

    [Fact]
    public void two_ctrl_c_presses_too_far_apart_do_not_fire_hotkey()
    {
        var d = new CtrlCCDetector();
        d.OnCDown(true, T0); d.OnCUp();
        Assert.False(d.OnCDown(true, T0.AddMilliseconds(700)));
    }

    [Fact]
    public void plain_c_between_ctrl_c_presses_does_not_fire_hotkey()
    {
        var d = new CtrlCCDetector();
        d.OnCDown(true, T0);  d.OnCUp();
        d.OnCDown(false, T0.AddMilliseconds(100)); d.OnCUp();
        Assert.False(d.OnCDown(true, T0.AddMilliseconds(200)));
    }

    [Fact]
    public async Task text_over_character_limit_is_never_sent_to_deepl()
    {
        var sent = 0;
        using var http = new ResilientHttp(
            () => new FakeHandler((_, _) =>
            {
                Interlocked.Increment(ref sent);
                return Task.FromResult(FakeHandler.Json(System.Net.HttpStatusCode.OK,
                    """{"translations":[{"text":"x"}]}"""));
            }),
            TimeSpan.FromSeconds(5), [TimeSpan.FromMilliseconds(10)]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            DeepLApi.TranslateAsync(new string('a', DeepLApi.MaxChars + 1), "de", accessToken: "tok", http: http));
        Assert.Equal(0, sent);
    }

    [Fact]
    public void offline_retry_pause_doubles_and_stops_at_five_minutes()
    {
        var pause = TimeSpan.FromSeconds(5);
        var waited = TimeSpan.Zero;
        var attempts = 0;
        while (waited < TimeSpan.FromHours(1))
        {
            waited += pause;
            attempts++;
            pause = RetryBackoff.Next(pause);
        }
        Assert.Equal(RetryBackoff.Max, pause);
        // Every few seconds for an hour would be 720 attempts.
        Assert.True(attempts < 25, $"{attempts} retries in one hour");
    }
}
