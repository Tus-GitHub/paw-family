namespace PawFamily;

static class Program
{
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-test")) { SelfTest.Run(); return; }
        if (args.Contains("--render-preview")) { Preview.Render(); return; }
        if (args.Contains("--export-icon")) { Preview.ExportIcon(); return; }
        if (args.Contains("--render-chat-preview")) { Preview.RenderChat(); return; }
        if (args.Contains("--animation-preview")) { Application.Run(new AnimationPreview()); return; }
        if (args.Length==2&&args[0]=="--test-navigate") { try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"navigation-test.txt"),ChatNavigator.Open(new Chat{Id=args[1]}));}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"navigation-test.txt"),ex.ToString());} return; }
        if(args.Contains("--project-report")){var report=new SessionMonitor().Scan().GroupBy(c=>c.ProjectLabel).Select(g=>new{Project=g.Key,Chats=g.Count()});File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"projects-report.json"),System.Text.Json.JsonSerializer.Serialize(report));return;}
        if(args.Contains("--orbit-preview")){var orbit=Preview.CreateOrbit();orbit.IsPreview=true;orbit.ShowInTaskbar=true;orbit.KeyPreview=true;orbit.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape)orbit.Close();};orbit.Pop(new Rectangle(850,700,140,140));Application.Run(orbit);return;}
        if(args.Contains("--chat-preview")){using var companion=new Companion(true);var window=new ChatWindow(companion){ShowInTaskbar=true};window.UpdateChats(Companion.DemoChats(),"DEMO · Sample chats");window.ShowNear(new Rectangle(850,700,140,140));Application.Run(window);return;}
        using var mutex = new Mutex(true, "Local.PawFamily.v1", out var first);
        if (!first) { MessageBox.Show("Paw Family is already running. Look for its pet or tray icon.", "Paw Family"); return; }
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "Paw Family");
        Application.Run(new Companion(args.Contains("--demo")));
    }
}
