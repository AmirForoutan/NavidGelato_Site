using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NavidGelato_Site.Services;

public interface IMessageContentFilter
{
    string Filter(string? value, bool enabled, string? configuredTerms);
}

public sealed class MessageContentFilter : IMessageContentFilter
{
    private const string SeparatorPattern = @"[\s\u200c\u200d_\-–—.،,;؛:!?؟/\\|*#@~`'""(){}\[\]]*";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(150);

    public string Filter(string? value, bool enabled, string? configuredTerms)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var text = NormalizeText(value);
        if (!enabled || string.IsNullOrWhiteSpace(configuredTerms))
            return text;

        var terms = ParseTerms(configuredTerms);
        foreach (var term in terms)
        {
            var pattern = BuildPattern(term);
            if (pattern is null)
                continue;

            try
            {
                text = Regex.Replace(
                    text,
                    pattern,
                    "***",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    RegexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                // A malformed/hostile message should never block delivery.
            }
        }

        return text;
    }

    private static IReadOnlyList<string> ParseTerms(string value) =>
        value.Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeTerm)
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(x => x.Length)
            .ToArray();

    private static string? BuildPattern(string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(normalizedTerm))
            return null;

        var compact = new string(normalizedTerm.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length < 2)
            return null;

        var parts = compact
            .Select(ch => $"{Regex.Escape(ch.ToString())}+")
            .ToArray();

        return $"(?<![\\p{{L}}\\p{{N}}]){string.Join(SeparatorPattern, parts)}(?![\\p{{L}}\\p{{N}}])";
    }

    private static string NormalizeTerm(string value) =>
        NormalizeText(value).Trim();

    private static string NormalizeText(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark || ch == 'ـ')
                continue;

            builder.Append(ch switch
            {
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                'ۀ' or 'ة' => 'ه',
                'ؤ' => 'و',
                'إ' or 'أ' => 'ا',
                _ => ch
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
