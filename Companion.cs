using System.Diagnostics;

namespace PawFamily;

public sealed class Companion : Form
{
    readonly Preferences prefs = Preferences.Load(); readonly SessionMonitor monitor = new();
    readonly PetArt parent = new() { Dock = DockStyle.Fill }; readonly System.Windows.Forms.Timer clock = new() { Interval = 50 };
    readonly NotifyIcon tray; readonly ToolTip tips = new();
    readonly RoamingMotion roaming = new(); readonly Stopwatch uptime = Stopwatch.StartNew(); double lastTick;
    List<Chat> chats = []; ProjectOrbit? family; ChatWindow? chatWindow; bool scanning, initialized, dragging, held, dialogOpen, navigating; int ticks, page; Point grab;
    DateTime lastHover = DateTime.UtcNow; readonly bool demo; Mood? preview; DateTime previewUntil;
    public static readonly Color Ink = Color.FromArgb(237, 236, 246), Panel = Color.FromArgb(29, 30, 43), Accent = Color.FromArgb(172, 153, 244), ClearKey = Color.FromArgb(1,1,1);
    public Companion(bool demo)
    {
        this.demo = demo; Text = "Paw Family"; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
        BackColor = ClearKey; TransparencyKey = ClearKey; ClientSize = new(140, 140); StartPosition = FormStartPosition.Manual;
        var area = Screen.PrimaryScreen!.WorkingArea; Location = prefs.X < 0 ? new(area.Right - 175, area.Bottom - 165) : new(prefs.X, prefs.Y);
        Clamp(); Controls.Add(parent); parent.Animal = prefs.Animal; parent.Pack = EffectivePack;
        parent.MouseEnter += (_, _) => ShowFamily(); parent.MouseMove += (_, e) => { lastHover = DateTime.UtcNow; if (e.Button == MouseButtons.Left) { if (Math.Abs(e.X - grab.X) + Math.Abs(e.Y - grab.Y) > 5) dragging = true; if (dragging) { Location = new(Cursor.Position.X - grab.X, Cursor.Position.Y - grab.Y); family?.Hide(); } } };
        parent.MouseDown += (_, e) => { grab = e.Location; held = true; dragging = false; };
        parent.MouseUp += (_, e) => { held = false; if (e.Button != MouseButtons.Left) return; bool moved = dragging; dragging = false; if (moved) { Clamp(); prefs.X = Left; prefs.Y = Top; Save(); } else OpenChats(null); };
        var menu = new ContextMenuStrip(); menu.Items.Add("All chats", null, (_, _) => OpenChats(null));
        menu.Items.Add("Cat", null, (_, _) => SetAnimal("cat")); menu.Items.Add("Dog", null, (_, _) => SetAnimal("dog"));
        var wander = new ToolStripMenuItem("Walk around") { Checked = prefs.RoamingEnabled, CheckOnClick = true };
        wander.CheckedChanged += (_, _) => { prefs.RoamingEnabled = wander.Checked; Save(); }; menu.Items.Add(wander);
        menu.Items.Add("Import my GIF animations…", null, (_, _) => { dialogOpen = true; try { ImportPack(); } finally { dialogOpen = false; } });
        if (Directory.Exists(Path.Combine(AppContext.BaseDirectory,"Assets","cat"))) menu.Items.Add("Use my previous cat pack", null, (_, _) => { prefs.AnimationFolder = ""; prefs.AnimatedCat = true; SetAnimal("cat"); });
        menu.Items.Add("Use original drawn pets", null, (_, _) => { prefs.AnimationFolder = ""; prefs.AnimatedCat = false; parent.Pack = EffectivePack; Save(); RebuildFamily(); });
        menu.Items.Add("Preview: needs attention", null, (_, _) => Preview(Mood.Attention)); menu.Items.Add("Preview: completed", null, (_, _) => Preview(Mood.Done));
        menu.Items.Add("How to use Paw Family", null, (_, _) => { dialogOpen=true;try{ShowGuide();}finally{dialogOpen=false;} });
        menu.Items.Add("Show pet", null, (_, _) => { Show(); Clamp(); }); menu.Items.Add("Exit Paw Family", null, (_, _) => Close()); parent.ContextMenuStrip = menu;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
        tray = new NotifyIcon { Icon = Icon, Text = "Paw Family — your project pets", Visible = true, ContextMenuStrip = menu }; tray.DoubleClick += (_, _) => OpenChats(null);
        clock.Tick += Tick; clock.Start(); Shown += async (_, _) => { await Scan(); if(!demo && !prefs.WelcomeShown) { prefs.WelcomeShown=true;Save();tray.BalloonTipTitle="Meet your project family";tray.BalloonTipText="Hover over the pet for projects. Click a pet for chats. Right-click for colors, animations and help.";tray.ShowBalloonTip(6500); } };
        FormClosed += (_, _) => { clock.Stop(); clock.Dispose(); tray.Visible = false; tray.Dispose(); family?.Dispose(); chatWindow?.Dispose(); tips.Dispose(); };
    }
    string EffectivePack => prefs.AnimationFolder != "" ? prefs.AnimationFolder : prefs.Animal == "cat" && prefs.AnimatedCat ? Path.Combine(AppContext.BaseDirectory,"Assets","cat") : "";
    void SetAnimal(string name) { prefs.Animal = name; parent.Animal = name; parent.Pack = EffectivePack; Save(); RebuildFamily(); }
    void Save() { try { prefs.Save(); } catch (Exception ex) { MessageBox.Show("Could not save your preferences: " + ex.Message); } }
    void Clamp() { var a = Screen.FromPoint(Location).WorkingArea; Location = new(Math.Clamp(Left, a.Left, Math.Max(a.Left, a.Right - Width)), Math.Clamp(Top, a.Top, Math.Max(a.Top, a.Bottom - Height))); }
    public void Preview(Mood mood) { preview = mood; previewUntil = DateTime.UtcNow.AddSeconds(8); }
    void Tick(object? sender, EventArgs e)
    {
        ticks++; parent.Phase = ticks / 12d; parent.Mood = preview != null && DateTime.UtcNow < previewUntil ? preview.Value : PetMood(chats);
        double now = uptime.Elapsed.TotalSeconds, elapsed = now - lastTick; lastTick = now;
        var nearby = Bounds; nearby.Inflate(28,28);
        bool interacting = held || dragging || dialogOpen || nearby.Contains(Cursor.Position) || family?.Visible == true || chatWindow?.Visible == true || parent.ContextMenuStrip?.Visible == true;
        var step = roaming.Step(Location, Screen.FromControl(this).WorkingArea, Size, now, elapsed, prefs.RoamingEnabled, interacting, parent.Mood);
        Location = step.Location; parent.Walking = step.Walking; parent.FacingLeft = step.FacingLeft; parent.Invalidate();
        if (family is { Visible: true })
        {
            if (family.Bounds.Contains(Cursor.Position) || Bounds.Contains(Cursor.Position)) lastHover = DateTime.UtcNow;
            else if ((DateTime.UtcNow - lastHover).TotalMilliseconds > 700) family.Hide();
            family.UpdatePets(parent.Phase,key=>PetMood(chats.Where(c=>c.Project==key)));
        }
        if (ticks % 80 == 0) _ = Scan();
    }
    Mood PetMood(IEnumerable<Chat> source) => Records.Aggregate(source.Select(c => { var clone = c.Copy(); if (c.Mood == Mood.Done && prefs.Acknowledged.GetValueOrDefault(c.Id) >= c.Updated.ToUnixTimeMilliseconds()) clone.Mood = Mood.Quiet; return clone; }));
    async Task Scan()
    {
        if (scanning || IsDisposed) return; scanning = true;
        try
        {
            var updated = demo ? DemoChats() : await Task.Run(monitor.Scan);
            if (IsDisposed) return;
            if (!initialized) foreach (var c in updated.Where(c => c.Mood == Mood.Done)) prefs.Acknowledged[c.Id] = c.Updated.ToUnixTimeMilliseconds();
            chats = updated; initialized = true;
            var keys = FamilyKey(); if (family?.Tag as string != keys) RebuildFamily();
            if (chatWindow is { IsDisposed: false }) chatWindow.UpdateChats(chats, demo ? "DEMO — sample chats only" : monitor.Health);
        }
        catch (Exception ex) { if (chatWindow is { IsDisposed: false }) chatWindow.SetHealth("Could not refresh local chats: " + ex.Message); }
        finally { scanning = false; }
    }
    List<string> Projects() => chats.Select(c=>c.Project).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k=>k==ProjectCatalog.Others?1:0).ThenBy(ProjectName,StringComparer.OrdinalIgnoreCase).ToList();
    string FamilyKey()=>string.Join("|",Projects().Select(p=>p+":"+chats.Count(c=>c.Project==p)));
    public Color ProjectColor(string key)
    {
        var legacy=chats.FirstOrDefault(c=>c.Project==key)?.LegacyProjectKey??"";
        if (prefs.Colors.TryGetValue(key, out var hex) || (legacy!=""&&prefs.Colors.TryGetValue(legacy,out hex))) { try { return ColorTranslator.FromHtml(hex); } catch { } }
        Color[] colors = [Color.FromArgb(235,174,128), Color.FromArgb(156,184,248), Color.FromArgb(172,215,171), Color.FromArgb(217,161,205), Color.FromArgb(225,206,132), Color.FromArgb(149,214,220)];
        uint hash = 2166136261; foreach (var ch in key.ToUpperInvariant()) hash = (hash ^ ch) * 16777619;
        return colors[hash % colors.Length];
    }
    public void ChangeColor(string key) { dialogOpen=true;try{using var picker = new ColorDialog { Color = ProjectColor(key), FullOpen = true }; if (picker.ShowDialog() != DialogResult.OK) return; prefs.Colors[key] = ColorTranslator.ToHtml(picker.Color); Save(); RebuildFamily(); chatWindow?.Refresh();}finally{dialogOpen=false;} }
    public string ProjectName(string key) => key==ProjectCatalog.Others?"Others":chats.FirstOrDefault(c=>c.Project==key)?.ProjectLabel??Path.GetFileName(key.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));
    void RebuildFamily()
    {
        bool visible = family?.Visible == true; family?.Dispose(); family = null; if (visible) ShowFamily();
    }
    void ShowFamily()
    {
        if (dragging || held || chatWindow?.Visible==true) return;
        lastHover=DateTime.UtcNow;
        if(family==null)
        {
            var projects=Projects();int pages=Math.Max(1,(projects.Count+5)/6);page=Math.Clamp(page,0,pages-1);
            family=new ProjectOrbit(projects.Skip(page*6).Take(6).Select(key=>(key,ProjectName(key),chats.Count(c=>c.Project==key),ProjectColor(key))),prefs.Animal,EffectivePack,page,pages,
                OpenChats,ChangeColor,delta=>{page=(page+delta+pages)%pages;RebuildFamily();});
            family.Tag=FamilyKey();
        }
        family.Pop(Bounds);
    }
    void OpenChats(string? project)
    {
        family?.Hide(); if (chatWindow is not { IsDisposed: false }) chatWindow = new ChatWindow(this);
        chatWindow.SelectProject(project); chatWindow.UpdateChats(chats, demo ? "DEMO — sample chats only" : monitor.Health); chatWindow.ShowNear(Bounds); chatWindow.Activate();
    }
    public void OpenChat(Chat chat)
    {
        if(navigating)return;
        if (demo) { MessageBox.Show("This is a sample chat in Demo mode.", "Paw Family"); return; }
        navigating=true;
        try { ChatNavigator.Open(chat); chatWindow?.Hide(); family?.Hide(); prefs.Acknowledged[chat.Id] = chat.Updated.ToUnixTimeMilliseconds(); Save(); }
        catch (Exception ex) { MessageBox.Show("Could not open this chat: " + ex.Message); }
        finally { var delay=new System.Windows.Forms.Timer{Interval=1500};delay.Tick+=(_,_)=>{navigating=false;delay.Dispose();};delay.Start(); }
    }
    void ImportPack()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose idle.gif, working.gif, attention.gif and done.gif. Optional: walk.gif (facing right)." };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        string[] names = ["idle.gif", "working.gif", "attention.gif", "done.gif"];
        try
        {
            foreach (var name in names) { var path = Path.Combine(dialog.SelectedPath,name); if (!File.Exists(path)) throw new IOException("Missing " + name); if (new FileInfo(path).Length > 20_000_000) throw new IOException(name + " exceeds 20 MB"); using var img = Image.FromFile(path); if (img.Width > 2048 || img.Height > 2048) throw new IOException(name + " exceeds 2048 × 2048 pixels"); }
            var walk = Path.Combine(dialog.SelectedPath,"walk.gif");
            if (File.Exists(walk)) { if (new FileInfo(walk).Length > 20_000_000) throw new IOException("walk.gif exceeds 20 MB"); using var img = Image.FromFile(walk); if (img.Width > 2048 || img.Height > 2048) throw new IOException("walk.gif exceeds 2048 × 2048 pixels"); names = [.. names, "walk.gif"]; if (File.Exists(Path.Combine(dialog.SelectedPath,"walk-faces-left.txt"))) names = [.. names,"walk-faces-left.txt"]; }
            var target = Path.Combine(Preferences.DataDirectory,"animations",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(target); foreach (var name in names) File.Copy(Path.Combine(dialog.SelectedPath,name),Path.Combine(target,name));
            prefs.AnimationFolder = target; parent.Pack = target; Save(); RebuildFamily();
        }
        catch (Exception ex) { MessageBox.Show("Animation pack could not be imported: " + ex.Message); }
    }
    public void AcknowledgeDone() { foreach (var c in chats.Where(c => c.Mood == Mood.Done)) prefs.Acknowledged[c.Id] = c.Updated.ToUnixTimeMilliseconds(); Save(); }
    static void ShowGuide()=>MessageBox.Show("Hover over the parent pet to see your projects.\nClick a kitten for that project's chats, or the parent for every chat.\nClick a chat once to open it. Press Esc to close the chat list.\n\nRaised paw: a recorded request needs you.\nHappy bounce: a new task finished.\nDrag the parent to move it. Right-click a kitten to change its color.\n\nOnly local Codex chats are supported. Some approval or question events are not exposed in local files, so keep the app's own notifications enabled too.\n\nPaw Family is an independent, free companion. It never answers or approves on your behalf.","Paw Family · Quick guide",MessageBoxButtons.OK,MessageBoxIcon.Information);
    internal static List<Chat> DemoChats() => [new() { Id="11111111-1111-1111-1111-111111111111", Title="Review the homepage update", Project=@"C:\Projects\Website", ProjectLabel="Website", Mood=Mood.Attention, Updated=DateTimeOffset.UtcNow }, new() { Id="22222222-2222-2222-2222-222222222222", Title="Build the trip planner", Project=@"C:\Projects\Travel", ProjectLabel="Travel", Mood=Mood.Working, Updated=DateTimeOffset.UtcNow }, new() { Id="33333333-3333-3333-3333-333333333333", Title="Prepare the weekly report", Project=@"C:\Projects\Research", ProjectLabel="Research", Mood=Mood.Done, Updated=DateTimeOffset.UtcNow }, new() { Id="44444444-4444-4444-4444-444444444444",Title="A quick question",Mood=Mood.Quiet,Updated=DateTimeOffset.UtcNow }];
}
