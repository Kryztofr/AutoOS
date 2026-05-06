using System.Text.RegularExpressions;

namespace AutoOS.Views.Settings.BIOS;

public partial class BiosSettingParser
{
    public static IEnumerable<BiosSettingModel> ParseFromStream(Stream stream)
    {
        var lines = new List<string>();
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }

        BiosSettingModel current = null;
        bool readingOptions = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("Setup Question", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null)
                {
                    yield return current;
                }

                current = new BiosSettingModel
                {
                    Line = i,
                    OriginalLines = lines
                };
                readingOptions = false;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var rawQuestion = parts[1].Trim().Replace('�', '™');

                    var match = TrailingWordRegex().Match(rawQuestion);
                    current.SetupQuestion = match.Success ? match.Groups[1].Value : rawQuestion;
                }

                continue;
            }

            if (current == null) continue;

            if (line.StartsWith("Help String", StringComparison.OrdinalIgnoreCase))
            {
                current.HelpString = FormatHelpString(line.Split('=', 2)[1].Trim());
                continue;
            }

            if (line.StartsWith("Token", StringComparison.OrdinalIgnoreCase))
            {
                current.Token = line.Split('=', 2)[1].Split("//")[0].Trim();
                continue;
            }

            if (line.StartsWith("Offset", StringComparison.OrdinalIgnoreCase))
            {
                current.Offset = line.Split('=', 2)[1].Trim();
                continue;
            }

            if (line.StartsWith("Width", StringComparison.OrdinalIgnoreCase))
            {
                current.Width = line.Split('=', 2)[1].Trim();
                continue;
            }

            if (line.StartsWith("BIOS Default", StringComparison.OrdinalIgnoreCase))
            {
                var part = line.Split('=', 2)[1].Split("//")[0].Trim();
                var match = Regex.Match(part, @"\[[^\]]+\](.+)");
                current.BiosDefault = match.Success ? match.Groups[1].Value.Trim() : ExtractValue(part);
                continue;
            }

            if (line.StartsWith("Value", StringComparison.OrdinalIgnoreCase))
            {
                var valuePart = line.Split('=', 2)[1].Split("//")[0].Trim();
                current.Value = ExtractValue(valuePart);
                continue;
            }

            if (line.StartsWith("Options", StringComparison.OrdinalIgnoreCase))
            {
                current.Options = [];
                readingOptions = true;

                var inline = line[(line.IndexOf('=') + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(inline))
                    ParseOptionLine(inline, current.Options);

                continue;
            }

            if (readingOptions)
            {
                if (line.StartsWith("[") || line.StartsWith("*["))
                {
                    ParseOptionLine(line, current.Options);
                }
                else
                {
                    readingOptions = false;
                }
            }
        }

        if (current != null)
        {
            yield return current;
        }

        static string ExtractValue(string raw)
        {
            raw = raw.Trim();

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
                return raw[1..^1];
            if (raw.StartsWith("<") && raw.EndsWith(">"))
                return raw[1..^1];
            if (raw.StartsWith("{") && raw.EndsWith("}"))
                return raw.TrimStart('{').TrimEnd('}').Trim();

            return raw;
        }

        static void ParseOptionLine(string line, List<Option> options)
        {
            var match = Regex.Match(line, @"^\*?\[(\w+)\](.*)$");
            if (!match.Success) return;

            var isSelected = line.StartsWith("*");
            var index = match.Groups[1].Value.Trim();
            var label = match.Groups[2].Value.Trim();
            var commentIndex = label.IndexOf("//");
            if (commentIndex >= 0) label = label[..commentIndex].Trim();

            options.Add(new Option
            {
                Index = index,
                Label = label,
                IsSelected = isSelected
            });
        }
    }

    static string FormatHelpString(string help)
    {
        if (string.IsNullOrWhiteSpace(help))
            return "No help string";

        help = help.Trim();

        // replace � with °
        help = help.Replace('�', '°');

        // new lines before bracketed markers like [Auto]:
        help = BracketedMarkerRegex().Replace(help, "\n");

        // insert newline before technical markers like Min.: Max.: etc.
        foreach (var marker in new[] { "Min.:", "Max.:", "Standard:", "Increment:" })
        {
            int index = help.IndexOf(marker);
            if (index > 0)
            {
                var before = help[..index].TrimEnd();
                var after = help[index..].TrimStart();
                help = $"{before}\n{after}";
                break;
            }
        }

        // new lines before lines starting with * or * followed by space
        help = AsteriskMarkerRegex().Replace(help, "\n");

        // new line before NOTE: or Note:
        help = NoteMarkerRegex().Replace(help, "\n");

        return help;
    }

    [GeneratedRegex(@"^(.*?)\s{2,}\w+$", RegexOptions.Compiled)]
    private static partial Regex TrailingWordRegex();
    [GeneratedRegex(@"(?<!^)(?=\[\w.*?\]:)", RegexOptions.Compiled)]
    private static partial Regex BracketedMarkerRegex();

    [GeneratedRegex(@"(?<!^)(?=\*\s?)", RegexOptions.Compiled)]
    private static partial Regex AsteriskMarkerRegex();

    [GeneratedRegex(@"(?<!^)(?=NOTE:|Note:)", RegexOptions.Compiled)]
    private static partial Regex NoteMarkerRegex();
}