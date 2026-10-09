using System.Text;
using System.Text.RegularExpressions;

namespace HuitJournal.Api.Services;

public static class DoiTools
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var doi = Regex.Replace(value.Trim(), @"^https?://(?:dx\.)?doi\.org/", "", RegexOptions.IgnoreCase);
        if (doi.Length > 100 || !Regex.IsMatch(doi, @"^10\.\d{4,9}/[A-Za-z0-9._;()/:+\-]+$"))
            throw new ArgumentException("DOI không hợp lệ. Nhập dạng 10.xxxx/mã-bài hoặc URL https://doi.org/… (tối đa 100 ký tự).");
        return doi.ToLowerInvariant();
    }

    public static string TitleKey(string value) => new(value.Normalize(NormalizationForm.FormKC)
        .Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public static string Payload(string? doi) => doi == null ? "null" : "\"" + doi + "\"";

    public static string NameKey(string value) => string.Join(" ", Regex.Replace(
        new string(value.Normalize(NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant().Replace('đ', 'd'),
        @"[^\p{L}\p{N}]+", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x, StringComparer.Ordinal));
}
