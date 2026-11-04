using System.Text.Json;

namespace PawFamily;

public sealed record ProjectInfo(string Key, string Name, string[] Roots);

public sealed class ProjectCatalog
{
    public const string Others = "Others";
    readonly Dictionary<string,ProjectInfo> projects = new();
    readonly Dictionary<string,string> assignments = new();
    readonly HashSet<string> projectless = new();
    readonly Dictionary<string,string> hints = new();
    public static ProjectCatalog Read(string home)
    {
        try { using var fs=new FileStream(Path.Combine(home,".codex-global-state.json"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete); using var doc=JsonDocument.Parse(fs); return Parse(doc.RootElement); }
        catch(IOException) { return new(); } catch(JsonException) { return new(); } catch(UnauthorizedAccessException) { return new(); }
    }
    public static ProjectCatalog Parse(JsonElement root)
    {
        var catalog=new ProjectCatalog();
        if(root.ValueKind!=JsonValueKind.Object)return catalog;
        if(root.TryGetProperty("local-projects",out var all) && all.ValueKind==JsonValueKind.Object)
            foreach(var item in all.EnumerateObject())
            {
                if(item.Value.ValueKind!=JsonValueKind.Object)continue;
                var roots=item.Value.TryGetProperty("rootPaths",out var paths)&&paths.ValueKind==JsonValueKind.Array?paths.EnumerateArray().Where(p=>p.ValueKind==JsonValueKind.String).Select(p=>Normalize(p.GetString()!)).Where(p=>p!="").ToArray():[];
                var name=Records.Str(item.Value,"name");
                catalog.projects[item.Name]=new("project:"+item.Name,name==""?"Project":name,roots);
            }
        if(root.TryGetProperty("thread-project-assignments",out var assigned)&&assigned.ValueKind==JsonValueKind.Object)
            foreach(var item in assigned.EnumerateObject()) if(item.Value.ValueKind==JsonValueKind.Object)catalog.assignments[item.Name]=Records.Str(item.Value,"projectId");
        if(root.TryGetProperty("projectless-thread-ids",out var loose)&&loose.ValueKind==JsonValueKind.Array)
            foreach(var id in loose.EnumerateArray())if(id.ValueKind==JsonValueKind.String)catalog.projectless.Add(id.GetString()!);
        if(root.TryGetProperty("thread-workspace-root-hints",out var hinted)&&hinted.ValueKind==JsonValueKind.Object)
            foreach(var item in hinted.EnumerateObject())if(item.Value.ValueKind==JsonValueKind.String)catalog.hints[item.Name]=Normalize(item.Value.GetString()!);
        return catalog;
    }
    public void Classify(Chat chat)
    {
        ProjectInfo? match=null;
        if(assignments.TryGetValue(chat.Id,out var assigned)) projects.TryGetValue(assigned,out match);
        else if(!projectless.Contains(chat.Id))
        {
            var path=Normalize(chat.WorkingDirectory);
            var hint=hints.GetValueOrDefault(chat.Id,"");
            match=projects.Values.SelectMany(p=>p.Roots.Select(r=>(Project:p,Root:r))).Where(p=>Inside(path,p.Root)||Inside(hint,p.Root)).OrderByDescending(p=>p.Root.Length).Select(p=>p.Project).FirstOrDefault();
        }
        chat.Project=match?.Key??Others; chat.ProjectLabel=match?.Name??Others;
        chat.LegacyProjectKey=match?.Roots.FirstOrDefault()??"";
    }
    static bool Inside(string path,string root)=>path!=""&&root!=""&&(path.Equals(root,StringComparison.OrdinalIgnoreCase)||path.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase));
    static string Normalize(string path)=>path.Replace('/','\\').TrimEnd('\\');
}
