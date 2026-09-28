using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Algora.Core;

public sealed class Interpreter
{
    private readonly Dictionary<string, object> _vars = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object> Variables => _vars;

    public string Run(string source)
    {
        _vars.Clear();
        var output = new List<string>();
        var lines = source.Replace("\r", "").Split('\n');

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("!")) continue;
            if (IsStructural(line)) continue;

            if (Regex.IsMatch(line, @"^(ΓΡΑΨΕ|ΕΜΦΑΝΙΣΕ)\b", RegexOptions.IgnoreCase))
            {
                var expr = Regex.Replace(line, @"^(ΓΡΑΨΕ|ΕΜΦΑΝΙΣΕ)\s*", "", RegexOptions.IgnoreCase);
                output.Add(EvaluatePrintable(expr));
                continue;
            }

            var assign = Regex.Match(line, @"^([\p{L}_][\p{L}\p{N}_]*)\s*(?:<-|←)\s*(.+)$");
            if (assign.Success)
            {
                _vars[assign.Groups[1].Value] = Evaluate(assign.Groups[2].Value);
                continue;
            }
        }
        return string.Join(Environment.NewLine, output);
    }

    private static bool IsStructural(string line) =>
        Regex.IsMatch(line, @"^(ΠΡΟΓΡΑΜΜΑ|ΑΛΓΟΡΙΘΜΟΣ|ΜΕΤΑΒΛΗΤΕΣ|ΑΡΧΗ|ΤΕΛΟΣ(_ΠΡΟΓΡΑΜΜΑΤΟΣ)?|ΑΚΕΡΑΙΕΣ|ΠΡΑΓΜΑΤΙΚΕΣ|ΧΑΡΑΚΤΗΡΕΣ|ΛΟΓΙΚΕΣ)\b", RegexOptions.IgnoreCase);

    private string EvaluatePrintable(string expr)
    {
        var parts = expr.Split(',', StringSplitOptions.TrimEntries);
        return string.Join(" ", parts.Select(p =>
        {
            if ((p.StartsWith("'") && p.EndsWith("'")) || (p.StartsWith("\"") && p.EndsWith("\"")))
                return p[1..^1];
            return Convert.ToString(Evaluate(p), CultureInfo.CurrentCulture) ?? "";
        }));
    }

    private object Evaluate(string expr)
    {
        expr = expr.Trim();
        if (_vars.TryGetValue(expr, out var exact)) return exact;
        foreach (var pair in _vars.OrderByDescending(x => x.Key.Length, Comparer<int>.Default))
            expr = Regex.Replace(expr, $@"\b{Regex.Escape(pair.Key)}\b", Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? "0", RegexOptions.IgnoreCase);
        var table = new DataTable { Locale = CultureInfo.InvariantCulture };
        return table.Compute(expr.Replace(",", "."), "");
    }
}
