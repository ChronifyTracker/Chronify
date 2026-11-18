using Chronify.ViewModels;

namespace Chronify.Tests;

public class DurationTextTests
{
    [Fact]
    public void ZeroIsShownAsEmpty()
    {
        Assert.Equal("", DurationText.From(0));
    }

    [Fact]
    public void UnderTenMinutesIsExact()
    {
        Assert.Equal("1 мин", DurationText.From(1));
        Assert.Equal("9 мин", DurationText.From(9));
    }

    [Fact]
    public void UnderAnHourIsRoundedToFiveMinutes()
    {
        Assert.Equal("10 мин", DurationText.From(12));
        Assert.Equal("30 мин", DurationText.From(29));
        Assert.Equal("45 мин", DurationText.From(45));
    }

    [Fact]
    public void AlmostAnHourBecomesOneHour()
    {
        Assert.Equal("1 ч", DurationText.From(58));
        Assert.Equal("1 ч", DurationText.From(60));
    }

    [Fact]
    public void UnderFiveHoursIsRoundedToHalfHours()
    {
        Assert.Equal("1 ч", DurationText.From(70));
        Assert.Equal("1,5 ч", DurationText.From(90));
        Assert.Equal("2 ч", DurationText.From(125));
        Assert.Equal("4,5 ч", DurationText.From(270));
    }

    [Fact]
    public void FiveHoursAndMoreIsRoundedToHours()
    {
        Assert.Equal("5 ч", DurationText.From(305));
        Assert.Equal("10 ч", DurationText.From(600));
        Assert.Equal("30 ч", DurationText.From(1810));
    }

    [Fact]
    public void TwoDaysAndMoreIsRoundedToDays()
    {
        Assert.Equal("2 дн", DurationText.From(2880));
        Assert.Equal("3 дн", DurationText.From(4000));
        Assert.Equal("69 дн", DurationText.From(TaskItem.MaxEstimatedMinutes));
    }
}
