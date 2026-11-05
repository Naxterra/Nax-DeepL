namespace NaxTranslate.Tests;

public class AutoTargetTests
{
    [Fact]
    public void german_text_with_german_target_switches_to_english_us()
    {
        Assert.Equal("en-US", DeepLApi.AutoTarget("DE", "de"));
    }

    [Fact]
    public void english_text_with_british_english_target_switches_to_german()
    {
        Assert.Equal("de", DeepLApi.AutoTarget("EN", "en-GB"));
    }

    [Fact]
    public void turkish_text_with_turkish_target_switches_to_german()
    {
        Assert.Equal("de", DeepLApi.AutoTarget("TR", "tr"));
    }

    [Fact]
    public void text_in_other_language_than_target_keeps_target()
    {
        Assert.Null(DeepLApi.AutoTarget("EN", "de"));
    }

    [Fact]
    public void unknown_detected_language_keeps_target()
    {
        Assert.Null(DeepLApi.AutoTarget(null, "de"));
    }

    [Fact]
    public void every_switch_target_is_in_the_target_list()
    {
        Assert.Contains("en-US", DeepLApi.TgtLangs.Values);
        Assert.Contains("de", DeepLApi.TgtLangs.Values);
    }

    [Fact]
    public void german_text_with_english_picked_as_source_switches_to_german_to_english()
    {
        // The case from the window: source "Englisch", target "Deutsch", but the text is German.
        Assert.Equal(("de", "en-US"), DeepLApi.AutoSwitch("en", "DE", "de"));
    }

    [Fact]
    public void automatic_source_switches_only_the_target()
    {
        Assert.Equal(("auto", "en-US"), DeepLApi.AutoSwitch("auto", "DE", "de"));
    }

    [Fact]
    public void fixed_source_that_matches_text_and_target_switches_nothing()
    {
        // German picked on both sides on purpose, for example to polish a German text.
        Assert.Null(DeepLApi.AutoSwitch("de", "DE", "de"));
    }

    [Fact]
    public void text_in_other_language_than_target_switches_nothing_whatever_the_source()
    {
        Assert.Null(DeepLApi.AutoSwitch("en", "TR", "de"));
        Assert.Null(DeepLApi.AutoSwitch("auto", "EN", "de"));
    }

    [Fact]
    public void detected_language_missing_from_source_list_falls_back_to_automatic_source()
    {
        // French text with English picked and French as target is not possible here, so use a
        // language the source list lacks together with a matching target code.
        var sw = DeepLApi.AutoSwitch("en", "FR", "fr");
        Assert.Equal(("auto", "de"), sw);
    }
}
