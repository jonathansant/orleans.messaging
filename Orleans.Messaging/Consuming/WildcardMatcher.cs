namespace PatternMatching;

public static class WildcardMatcher
{
    public static bool Test(string pattern, string input) => Match(pattern, input, 0, 0);

    private static bool Match(string pattern, string input, int patternIndex, int inputIndex) =>
        patternIndex == pattern.Length
            ? inputIndex == input.Length
            : pattern[patternIndex] switch
            {
                '*' => Match(pattern, input, patternIndex + 1, inputIndex) ||
                       (inputIndex < input.Length && Match(pattern, input, patternIndex, inputIndex + 1)),
                '?' => Match(pattern, input, patternIndex + 1, inputIndex) ||
                       (inputIndex < input.Length && Match(pattern, input, patternIndex + 1, inputIndex + 1)),
                _ => inputIndex < input.Length && pattern[patternIndex] == input[inputIndex] &&
                     Match(pattern, input, patternIndex + 1, inputIndex + 1)
            };
}
