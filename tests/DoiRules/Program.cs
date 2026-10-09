using HuitJournal.Api.Services;
void Check(bool condition) { if (!condition) throw new Exception("DOI rule failed"); }
Check(DoiTools.Normalize(" https://doi.org/10.1234/AbC ") == "10.1234/abc");
Check(DoiTools.Normalize("http://dx.doi.org/10.1234/ABC") == "10.1234/abc");
Check(DoiTools.Normalize(null) == null);
Check(DoiTools.Payload("10.1234/a+b") == "\"10.1234/a+b\"");
Check(DoiTools.NameKey("Nguyễn Văn Cường") == DoiTools.NameKey("Cuong Nguyen Van"));
Check(DoiTools.TitleKey("Title: Example.") == DoiTools.TitleKey("TITLE example"));
foreach (var bad in new[] { "garbage", "10.123/a", "10.1234/a b", "10.1234/<script>", "10.1234/" + new string('a', 100) }) {
    try { DoiTools.Normalize(bad); throw new Exception("Invalid DOI accepted"); } catch (ArgumentException) { }
}
Console.WriteLine("PASS DOI normalization, invalid input, stable JSON payload and title/author comparison");
