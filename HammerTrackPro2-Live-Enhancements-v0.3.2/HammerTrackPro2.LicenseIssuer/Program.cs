using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

if(args.Length==0) return Usage();
try
{
    return args[0].ToLowerInvariant() switch
    {
        "keygen"=>GenerateKeys(args),
        "issue"=>IssueLicense(args),
        "issue-demo"=>IssueLicense(args, true),
        "issue-module"=>IssueModule(args),
        _=>Usage()
    };
}
catch(Exception exception)
{
    Console.Error.WriteLine(exception.Message); return 1;
}

static int GenerateKeys(string[] arguments)
{
    if(arguments.Length!=2)throw new InvalidOperationException("Usage: keygen <secure-output-folder>");
    Directory.CreateDirectory(arguments[1]); var privatePath=Path.Combine(arguments[1],"hammertrack-license-private.pem"); var publicPath=Path.Combine(arguments[1],"hammertrack-license-public.pem");
    if(File.Exists(privatePath)||File.Exists(publicPath))throw new InvalidOperationException("Key files already exist. Refusing to overwrite them.");
    using var rsa=RSA.Create(3072); File.WriteAllText(privatePath,rsa.ExportRSAPrivateKeyPem()); File.WriteAllText(publicPath,rsa.ExportSubjectPublicKeyInfoPem());
    Console.WriteLine($"Private key: {privatePath}"); Console.WriteLine($"Public key: {publicPath}"); return 0;
}

static int IssueLicense(string[] arguments, bool demo = false)
{
    var options=Parse(arguments.Skip(1));
    var key=Required(options,"key"); var customer=Required(options,"customer"); var machines=Required(options,"machines").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(value=>value.ToUpperInvariant()).Distinct().ToArray(); var output=Required(options,"output");
    if(machines.Length==0)throw new InvalidOperationException("At least one Machine ID is required.");
    if(demo && options.ContainsKey("expires"))throw new InvalidOperationException("Demo licenses always last seven days; do not supply --expires.");
    var issuedUtc=DateTime.UtcNow;
    var payload=new LicensePayload(Guid.NewGuid().ToString("D"),customer,machines,issuedUtc,demo?issuedUtc.AddDays(7):options.TryGetValue("expires",out var expires)?DateTime.Parse(expires).ToUniversalTime():null,demo?"Demo":null);
    var payloadJson=JsonSerializer.Serialize(payload,IssuerJson.Options); using var rsa=RSA.Create(); rsa.ImportFromPem(File.ReadAllText(key)); var signature=rsa.SignData(Encoding.UTF8.GetBytes(payloadJson),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); File.WriteAllText(output,JsonSerializer.Serialize(new LicenseDocument(payload,Convert.ToBase64String(signature)),IssuerJson.Options));
    if(demo)Console.WriteLine($"Seven-day demo expires {payload.ExpiresUtc:O}. Scanner and Wholesale are disabled.");
    Console.WriteLine($"Issued {payload.LicenseId} to {customer} for {machines.Length} PC(s): {output}"); return 0;
}

static int IssueModule(string[] arguments)
{
    var options=Parse(arguments.Skip(1));
    var key=Required(options,"key");var customer=Required(options,"customer");var module=Required(options,"module");var machines=Required(options,"machines").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(value=>value.ToUpperInvariant()).Distinct().ToArray();var output=Required(options,"output");
    if(machines.Length==0)throw new InvalidOperationException("At least one Machine ID is required.");
    var payload=new ModuleActivationPayload(Guid.NewGuid().ToString("D"),module,customer,machines,DateTime.UtcNow,options.TryGetValue("expires",out var expires)?DateTime.Parse(expires).ToUniversalTime():null);
    var payloadJson=JsonSerializer.Serialize(payload,IssuerJson.Options);using var rsa=RSA.Create();rsa.ImportFromPem(File.ReadAllText(key));var signature=rsa.SignData(Encoding.UTF8.GetBytes(payloadJson),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
    var documentJson=JsonSerializer.Serialize(new ModuleActivationDocument(payload,Convert.ToBase64String(signature)),IssuerJson.Options);
    var activationKey=Convert.ToBase64String(Encoding.UTF8.GetBytes(documentJson));Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);File.WriteAllText(output,activationKey);
    Console.WriteLine($"Issued {module} activation {payload.ActivationId} to {customer} for {machines.Length} PC(s): {output}");return 0;
}

static Dictionary<string,string> Parse(IEnumerable<string> arguments)
{
    var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase); using var iterator=arguments.GetEnumerator();
    while(iterator.MoveNext()) { var name=iterator.Current; if(!name.StartsWith("--")||!iterator.MoveNext())throw new InvalidOperationException("Options use --name value."); values[name[2..]]=iterator.Current; }
    return values;
}
static string Required(Dictionary<string,string> values,string name)=>values.TryGetValue(name,out var value)&&!string.IsNullOrWhiteSpace(value)?value:throw new InvalidOperationException($"Missing --{name}.");
static int Usage() { Console.WriteLine("HammerTrack Pro License Issuer\n  keygen <secure-output-folder>\n  issue --key <private.pem> --customer <name> --machines <id,id> --output <license.htplicense> [--expires <date>]\n  issue-demo --key <private.pem> --customer <name> --machines <id,id> --output <demo.htplicense>\n  issue-module --key <private.pem> --module Wholesale --customer <name> --machines <id,id> --output <activation.txt> [--expires <date>]"); return 1; }

internal sealed record LicensePayload(string LicenseId,string CustomerName,string[] MachineIds,DateTime IssuedUtc,DateTime? ExpiresUtc,[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Edition=null);
internal sealed record LicenseDocument(LicensePayload Payload,string Signature);
internal sealed record ModuleActivationPayload(string ActivationId,string Module,string CustomerName,string[] MachineIds,DateTime IssuedUtc,DateTime? ExpiresUtc);
internal sealed record ModuleActivationDocument(ModuleActivationPayload Payload,string Signature);
internal static class IssuerJson { internal static readonly JsonSerializerOptions Options=new() { WriteIndented=true }; }
