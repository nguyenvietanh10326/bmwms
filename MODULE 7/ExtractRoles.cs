using System;
using System.IO;
using System.Text.RegularExpressions;

var text = File.ReadAllText(@"d:\bmwms\MODULE 7\ucs.txt");
var regex = new Regex(@"UC-\d+:\s*(?<name>.*?)\s+UI-type block.*?Allowed Roles\s*(?<roles>.*?)\s*Verification Criteria", RegexOptions.Singleline);
var matches = regex.Matches(text);

using var writer = new StreamWriter(@"d:\bmwms\MODULE 7\extracted_roles.txt");
foreach (Match match in matches)
{
    var name = match.Groups["name"].Value.Trim();
    var roles = Regex.Replace(match.Groups["roles"].Value.Trim(), @"\s+", " ");
    writer.WriteLine($"{name} => {roles}");
}
