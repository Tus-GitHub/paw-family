using System.Text.Json;

namespace PawFamily;

public enum Mood { Quiet, Working, Attention, Done, Error }
public sealed class Chat
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "Untitled chat";
    public string Project { get; set; } = ProjectCatalog.Others;
    public string ProjectLabel { get; set; } = ProjectCatalog.Others;
    public string LegacyProjectKey { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public Mood Mood { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string PendingCall { get; set; } = "";
    public HashSet<string> PendingQuestions { get; set; } = [];
    public string Status => Mood switch { Mood.Working => "Working", Mood.Attention => "Needs you", Mood.Done => "Completed", Mood.Error => "Stopped / error", _ => "No live status" };
    public Chat Copy() { var copy=(Chat)MemberwiseClone();copy.PendingQuestions=new(PendingQuestions);return copy; }
}

public static class Records
{
    public static string Str(JsonElement e, string key) => e.ValueKind==JsonValueKind.Object && e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    public static bool Apply(Chat chat, string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if(root.ValueKind!=JsonValueKind.Object)return false;
            if (!root.TryGetProperty("payload", out var p) || p.ValueKind != JsonValueKind.Object) return false;
            var type = Str(root, "type"); var evt = Str(p, "type");
            if (type == "session_meta")
            {
                var id = Str(p, "id"); if (id == "") id = Str(p, "session_id");
                if (id != "") chat.Id = id;
                var cwd = Str(p, "cwd"); if (cwd != "") chat.WorkingDirectory = cwd;
            }
            if (type == "turn_context" && Str(p, "cwd") is { Length: > 0 } path) chat.WorkingDirectory = path;
            Mood? mood = null;
            if (type == "event_msg")
            {
                if (evt is "user_message" or "turn_aborted" or "task_aborted") chat.PendingQuestions.Clear();
                mood = evt switch
                {
                    "task_started" or "turn_started" or "user_message" => Mood.Working,
                    "task_complete" or "turn_complete" => Mood.Done,
                    "request_user_input" or "exec_approval_request" or "apply_patch_approval_request" or "patch_approval_request" or "elicitation_request" or "mcp_elicitation_request" or "request_permissions" => Mood.Attention,
                    "turn_aborted" or "task_aborted" or "error" => Mood.Error,
                    _ => null
                };
                if (mood != null) chat.PendingCall = "";
            }
            if (type == "response_item")
            {
                if (evt == "message" && Str(p, "role") == "assistant" && Str(p, "phase") == "final") { mood = Mood.Done; chat.PendingCall = ""; }
                var name = Str(p, "name").Split('.').Last();
                if (evt == "function_call" && name == "request_user_input_async" && Str(p,"call_id") is {Length:>0} questionId)
                    chat.PendingQuestions.Add(questionId);
                if (evt == "function_call_output" && chat.PendingQuestions.Contains(Str(p,"call_id")))
                {
                    try { using var output=JsonDocument.Parse(Str(p,"output")); if(output.RootElement.ValueKind==JsonValueKind.Object&&output.RootElement.TryGetProperty("accepted",out var accepted)&&accepted.ValueKind==JsonValueKind.False){chat.PendingQuestions.Remove(Str(p,"call_id"));mood=Mood.Working;} } catch(JsonException) { }
                }
                if (evt == "function_call" && name is "request_user_input" or "request_permissions")
                { mood = Mood.Attention; chat.PendingCall = Str(p, "call_id"); }
                if (evt == "function_call_output" && chat.PendingCall != "" && Str(p, "call_id") == chat.PendingCall)
                { mood = Mood.Working; chat.PendingCall = ""; }
            }
            if (mood != null) chat.Mood = mood.Value;
            if (chat.PendingQuestions.Count>0) chat.Mood=Mood.Attention;
            if (DateTimeOffset.TryParse(Str(root, "timestamp"), out var at)) chat.Updated = at;
            return true;
        }
        catch (JsonException) { return false; }
    }
    public static string Link(Chat chat) => Guid.TryParse(chat.Id, out var id) ? "codex://threads/" + id : throw new ArgumentException("Invalid chat ID");
    public static Mood Aggregate(IEnumerable<Chat> chats)
    {
        var moods = chats.Select(c => c.Mood).ToHashSet();
        return moods.Contains(Mood.Attention) ? Mood.Attention : moods.Contains(Mood.Error) ? Mood.Error : moods.Contains(Mood.Working) ? Mood.Working : moods.Contains(Mood.Done) ? Mood.Done : Mood.Quiet;
    }
}

// Read-only session adapter. Incomplete trailing JSON is retried next scan.
public sealed class SessionMonitor
{
    readonly string home;
    readonly Dictionary<string, (long Size, DateTime Write, Chat Chat)> cache = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> titles = new();
    public string Health { get; private set; } = "Reading local chats…";
    public SessionMonitor(string? root = null) => home = root ?? Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
    public List<Chat> Scan()
    {
        var index = Path.Combine(home, "session_index.jsonl");
        if (File.Exists(index))
        {
            try { foreach (var line in SharedLines(index)) { try { using var d = JsonDocument.Parse(line); var id = Records.Str(d.RootElement, "id"); var title = Records.Str(d.RootElement, "thread_name"); if (id != "") titles[id] = title; } catch (JsonException) { } } } catch (IOException) { }
        }
        var root = Path.Combine(home, "sessions");
        if (!Directory.Exists(root)) { Health = "Local Codex sessions folder not found"; return []; }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(root, "rollout-*.jsonl", SearchOption.AllDirectories))
        {
            seen.Add(file);
            try
            {
                var fi = new FileInfo(file);
                if (cache.TryGetValue(file, out var old) && old.Size == fi.Length && old.Write == fi.LastWriteTimeUtc) continue;
                var chat = new Chat();
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(fs);
                var first = reader.ReadLine(); if (first != null) Records.Apply(chat, first);
                if (fs.Length > 1024 * 1024) { reader.DiscardBufferedData(); fs.Seek(-1024 * 1024, SeekOrigin.End); reader.ReadLine(); }
                string? line; while ((line = reader.ReadLine()) != null) Records.Apply(chat, line);
                if (chat.Id != "") cache[file] = (fi.Length, fi.LastWriteTimeUtc, chat);
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        foreach (var key in cache.Keys.Where(k => !seen.Contains(k)).ToArray()) cache.Remove(key);
        var results = cache.Values.Select(v => v.Chat.Copy()).GroupBy(c => c.Id).Select(g => g.OrderByDescending(c => c.Updated).First()).ToDictionary(c => c.Id);
        foreach (var pair in titles)
        {
            if (!results.TryGetValue(pair.Key, out var chat)) results[pair.Key] = chat = new Chat { Id = pair.Key };
            if (pair.Value != "") chat.Title = pair.Value;
        }
        var catalog=ProjectCatalog.Read(home);
        foreach (var c in results.Values)
        {
            catalog.Classify(c);
            if (c.Mood == Mood.Working && DateTimeOffset.UtcNow - c.Updated > TimeSpan.FromMinutes(10)) c.Mood = Mood.Quiet;
        }
        Health = $"{results.Count} local chats · refreshed {DateTime.Now:t}";
        return results.Values.OrderByDescending(c => c.Updated).ToList();
    }
    static IEnumerable<string> SharedLines(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs); string? line;
        while ((line = sr.ReadLine()) != null) yield return line;
    }
}

public sealed class Preferences
{
    public string Animal { get; set; } = "cat";
    public bool AnimatedCat { get; set; } = false;
    public bool RoamingEnabled { get; set; } = true;
    public bool WelcomeShown { get; set; }
    public string AnimationFolder { get; set; } = "";
    public Dictionary<string, string> Colors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long> Acknowledged { get; set; } = new();
    public int X { get; set; } = -1; public int Y { get; set; } = -1;
    public static string DataDirectory => Environment.GetEnvironmentVariable("PAWFAMILY_DATA_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PawFamily");
    public static string FilePath => Path.Combine(DataDirectory, "preferences.json");
    public static Preferences Load()
    {
        try
        {
            var legacy = Path.Combine(AppContext.BaseDirectory,"data","preferences.json");
            var prefs = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(File.Exists(FilePath)?FilePath:legacy)) ?? new();
            prefs.Colors = new(prefs.Colors ?? new(),StringComparer.OrdinalIgnoreCase);
            prefs.Acknowledged ??= new();
            prefs.AnimationFolder ??= "";
            return prefs;
        }
        catch { return new(); }
    }
    public void Save() { Directory.CreateDirectory(DataDirectory); var temporary=FilePath+".tmp";File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));File.Move(temporary,FilePath,true); }
}
