using Xunit;

namespace Johannes.Tests;

public class TypographyTests
{
	[Theory]
	[InlineData('\u00A0', true)]
	[InlineData('\u202F', true)]
	[InlineData(' ', false)]
	[InlineData('\t', false)]
	[InlineData('a', false)]
	public void IsNonBreakingSpace_RecognizesOnlyNonBreakingSpaces(char c, bool expected) =>
		Assert.Equal(expected, Typography.IsNonBreakingSpace(c));

	[Theory]
	[InlineData(' ', true)]
	[InlineData('\u00A0', true)]
	[InlineData('\u202F', true)]
	[InlineData('\t', false)]
	[InlineData('x', false)]
	public void IsSpace_ExcludesTab(char c, bool expected) =>
		Assert.Equal(expected, Typography.IsSpace(c));

	[Theory]
	[InlineData(' ', true)]
	[InlineData('\u00A0', true)]
	[InlineData('\u202F', true)]
	[InlineData('\t', true)]
	[InlineData('\n', false)]
	public void IsSpaceOrTab_IncludesTab(char c, bool expected) =>
		Assert.Equal(expected, Typography.IsSpaceOrTab(c));

	[Theory]
	[InlineData('!', true)]
	[InlineData('?', true)]
	[InlineData(':', true)]
	[InlineData(';', true)]
	[InlineData('.', false)]
	[InlineData(',', false)]
	public void IsDoublePunctuation_MatchesFrenchDoublePunctuation(char c, bool expected) =>
		Assert.Equal(expected, Typography.IsDoublePunctuation(c));

	[Fact]
	public void NoBreakSpaceText_MatchesNoBreakSpaceChar() =>
		Assert.Equal(Typography.NoBreakSpaceText, Typography.NoBreakSpace.ToString());
}
