using System.Runtime.CompilerServices;

private static string GetSourceFolder([CallerFilePath] string file = "") =>
    Path.GetDirectoryName(file);

var files = Directory.GetFiles(".");
var script =
    string.Join(",\n  ", files.Select(file => (Path.GetFileName(file), File.ReadAllBytes(file)))
        .Where(item => {
            if (item.Item2.Length < 16) return false;
            var prefix = new byte[8];
            Array.Copy(item.Item2, 0, prefix, 0, prefix.Length);
            var text = System.Text.Encoding.ASCII.GetString(prefix);
            return text == "Salted__";
        })
        .OrderBy(item => item.Item1)
        .Select(item => (item.Item1, string.Join("", item.Item2.Select(value => value.ToString("x2")))))
        .Select(item => $"'{item.Item1}' : '{item.Item2}'"));

var template = File.ReadAllText(Path.Join(GetSourceFolder(), "template.html"));
var content = template.Replace("const files = { };", "const files = {\n  " + script + "\n};");
Console.WriteLine(content);
