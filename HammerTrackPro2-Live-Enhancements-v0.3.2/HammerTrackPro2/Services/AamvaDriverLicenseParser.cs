using System.Text.RegularExpressions;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed partial class AamvaDriverLicenseParser
{
    private static readonly string[] KnownCodes=["DAQ","DCS","DAC","DCT","DAD","DAA","DAG","DAI","DAJ","DAK"];

    public DriverLicenseScanData Parse(string rawBarcode)
    {
        if(string.IsNullOrWhiteSpace(rawBarcode))throw new InvalidOperationException("The driver's-license barcode was empty.");
        var normalized=rawBarcode.Replace('\u001d','\n').Replace('\u001e','\n').Replace('\r','\n');
        if(!normalized.Contains("ANSI ",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The scanned PDF417 barcode is not an AAMVA driver's license.");
        var fields=ReadFields(normalized);
        var license=Value(fields,"DAQ");
        var last=Value(fields,"DCS");
        var first=FirstNonEmpty(Value(fields,"DAC"),Value(fields,"DCT"));
        if((string.IsNullOrWhiteSpace(first)||string.IsNullOrWhiteSpace(last))&&fields.TryGetValue("DAA",out var fullName))SplitFullName(fullName,ref first,ref last);
        if(string.IsNullOrWhiteSpace(license)&&string.IsNullOrWhiteSpace(first)&&string.IsNullOrWhiteSpace(last))throw new InvalidOperationException("The license barcode did not contain recognizable bidder information.");
        return new DriverLicenseScanData(first,last,Value(fields,"DAG"),Value(fields,"DAI"),Value(fields,"DAJ"),NormalizeZip(Value(fields,"DAK")),license);
    }

    private static Dictionary<string,string> ReadFields(string text)
    {
        var fields=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(Match match in FieldPattern().Matches(text))
        {
            var code=match.Groups[1].Value;var value=match.Groups[2].Value.Trim();
            if(KnownCodes.Contains(code,StringComparer.OrdinalIgnoreCase)&&!fields.ContainsKey(code))fields[code]=value;
        }
        return fields;
    }

    private static string Value(IReadOnlyDictionary<string,string> fields,string code)=>fields.TryGetValue(code,out var value)?value.Trim():string.Empty;
    private static string FirstNonEmpty(params string[] values)=>values.FirstOrDefault(value=>!string.IsNullOrWhiteSpace(value))??string.Empty;
    private static string NormalizeZip(string zip){var compact=new string(zip.Where(character=>char.IsLetterOrDigit(character)||character=='-').ToArray());if(compact.Length==9&&compact.All(char.IsDigit))return compact[..5]+"-"+compact[5..];return compact;}
    private static void SplitFullName(string fullName,ref string first,ref string last){var parts=fullName.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);if(parts.Length>=2){last=parts[0];first=parts[1].Split(' ',StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??string.Empty;}else{var words=fullName.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(words.Length>0)first=words[0];if(words.Length>1)last=words[^1];}}

    [GeneratedRegex(@"(?:^|\n|DL|ID)(DAQ|DCS|DAC|DCT|DAD|DAA|DAG|DAI|DAJ|DAK)([^\n]+)",RegexOptions.IgnoreCase)]
    private static partial Regex FieldPattern();
}
