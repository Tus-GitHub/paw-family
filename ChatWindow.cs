namespace PawFamily;

public sealed class ChatWindow : Form
{
    readonly Companion owner;
    readonly Label heading = new(), health = new(); readonly TextBox search = new();
    readonly ListBox list = new(); readonly CheckBox attention = new();
    List<Chat> chats = [];
    readonly Label empty = new(){Text="No chats here yet.\nTry another project or clear the filter.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(185,182,204),BackColor=Color.FromArgb(23,24,35)};
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? Project { get; set; }
    public ChatWindow(Companion owner)
    {
        this.owner = owner; Text = "Paw Family · Chats"; Size = new(540,560); MinimumSize = new(500,440); StartPosition = FormStartPosition.Manual;
        FormBorderStyle=FormBorderStyle.None;TopMost=true;
        BackColor = Companion.Panel; ForeColor = Companion.Ink; Font = new("Segoe UI",11); ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new(22), ColumnCount=1, RowCount=6 };
        layout.RowStyles.Add(new(SizeType.Absolute,57)); layout.RowStyles.Add(new(SizeType.Absolute,42)); layout.RowStyles.Add(new(SizeType.Absolute,36)); layout.RowStyles.Add(new(SizeType.Percent,100)); layout.RowStyles.Add(new(SizeType.Absolute,48)); layout.RowStyles.Add(new(SizeType.Absolute,38)); Controls.Add(layout);
        var titleRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};titleRow.ColumnStyles.Add(new(SizeType.Percent,100));titleRow.ColumnStyles.Add(new(SizeType.Absolute,40));heading.Dock=DockStyle.Fill; heading.Font=new("Segoe UI",18,FontStyle.Bold); heading.AutoEllipsis=true; titleRow.Controls.Add(heading,0,0);
        var close=new Button{Text="×",AccessibleName="Close chat list",Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,ForeColor=Companion.Ink,Font=new("Segoe UI",18),Margin=Padding.Empty};close.FlatAppearance.BorderSize=0;close.Click+=(_,_)=>Hide();titleRow.Controls.Add(close,1,0);layout.Controls.Add(titleRow,0,0);
        search.Dock=DockStyle.Fill; search.AccessibleName="Search chats or projects";search.PlaceholderText="Search chats or projects…"; search.BackColor=Color.FromArgb(43,44,60); search.ForeColor=Companion.Ink; search.BorderStyle=BorderStyle.FixedSingle; search.TextChanged += (_,_)=>Render(); var searchRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};searchRow.ColumnStyles.Add(new(SizeType.Absolute,75));searchRow.ColumnStyles.Add(new(SizeType.Percent,100));searchRow.Controls.Add(new Label{Text="Search",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new("Segoe UI",10)},0,0);searchRow.Controls.Add(search,1,0);layout.Controls.Add(searchRow,0,1);
        attention.Text="Needs my attention only"; attention.AutoSize=true; attention.CheckedChanged+=(_,_)=>Render(); layout.Controls.Add(attention,0,2);
        list.Dock=DockStyle.Fill; list.BackColor=Color.FromArgb(23,24,35); list.ForeColor=Companion.Ink; list.BorderStyle=BorderStyle.None; list.DrawMode=DrawMode.OwnerDrawFixed; list.ItemHeight=78; list.IntegralHeight=false; list.DrawItem+=DrawChat;
        list.MouseClick+=(_,e)=>{if(e.Button==MouseButtons.Left&&list.IndexFromPoint(e.Location)>=0)OpenSelected();}; list.KeyDown+=(_,e)=> { if(e.KeyCode==Keys.Enter) { OpenSelected(); e.Handled=true; } }; var listArea=new Panel{Dock=DockStyle.Fill};listArea.Controls.Add(list);listArea.Controls.Add(empty);layout.Controls.Add(listArea,0,3);
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,Padding=new(0,8,0,0)};
        Button Add(string text, Action action) { var b=new Button {Text=text,AutoSize=true,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(49,46,70),ForeColor=Companion.Ink,Font=new("Segoe UI",9),Height=31}; b.FlatAppearance.BorderSize=0; b.Click+=(_,_)=>action(); buttons.Controls.Add(b); return b; }
        Add("Clear completed alerts",owner.AcknowledgeDone);var projectColor=Add("Project color",()=>{if(Project!=null)owner.ChangeColor(Project);});projectColor.Name="projectColor";layout.Controls.Add(buttons,0,4);
        health.Dock=DockStyle.Fill; health.ForeColor=Color.FromArgb(170,168,190); health.Font=new("Segoe UI",9); layout.Controls.Add(health,0,5);
        FormClosing+=(_,e)=> { if(e.CloseReason==CloseReason.UserClosing) {e.Cancel=true;Hide();} };
        Resize+=(_,_)=>RoundCorners();RoundCorners();
        KeyPreview=true;KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){Hide();e.Handled=true;}};
    }
    public void SelectProject(string? project){if(Project!=project){search.Clear();attention.Checked=false;}Project=project;foreach(var button in Controls.Find("projectColor",true))button.Visible=project!=null;}
    public void ShowNear(Rectangle anchor){var area=Screen.FromRectangle(anchor).WorkingArea;Location=new(Math.Clamp(anchor.Right-Width,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(anchor.Top-Height-12,area.Top,Math.Max(area.Top,area.Bottom-Height)));Show();}
    void RoundCorners(){using var p=new System.Drawing.Drawing2D.GraphicsPath();int d=26;p.AddArc(0,0,d,d,180,90);p.AddArc(Width-d,0,d,d,270,90);p.AddArc(Width-d,Height-d,d,d,0,90);p.AddArc(0,Height-d,d,d,90,90);p.CloseFigure();var old=Region;Region=new Region(p);old?.Dispose();}
    void OpenSelected() { if(list.SelectedItem is Chat chat) owner.OpenChat(chat); }
    public void SetHealth(string text)=>health.Text=text;
    public void UpdateChats(List<Chat> source,string status) { chats=source;SetHealth(status);Render(); }
    void Render()
    {
        var selected=(list.SelectedItem as Chat)?.Id;
        var filtered=chats.Where(c=>Project==null || c.Project==Project).Where(c=>!attention.Checked || c.Mood is Mood.Attention or Mood.Error).Where(c=>string.IsNullOrWhiteSpace(search.Text) || (c.Title+" "+c.ProjectLabel).Contains(search.Text,StringComparison.OrdinalIgnoreCase)).OrderBy(c=>c.Mood==Mood.Attention?0:1).ThenByDescending(c=>c.Updated).ToArray();
        heading.Text=(Project==null?"Your pet family":owner.ProjectName(Project))+"  ·  "+filtered.Length;
        list.BeginUpdate();list.Items.Clear();list.Items.AddRange(filtered);for(int i=0;i<list.Items.Count;i++)if(((Chat)list.Items[i]).Id==selected)list.SelectedIndex=i;list.EndUpdate();
        if(list.SelectedIndex<0 && list.Items.Count>0)list.SelectedIndex=0;
        empty.Visible=filtered.Length==0; if(empty.Visible)empty.BringToFront();
    }
    void DrawChat(object? sender,DrawItemEventArgs e)
    {
        if(e.Index<0)return;var c=(Chat)list.Items[e.Index];bool chosen=e.State.HasFlag(DrawItemState.Selected);
        using var bg=new SolidBrush(chosen?Color.FromArgb(51,48,73):list.BackColor);e.Graphics.FillRectangle(bg,e.Bounds);
        using var color=new SolidBrush(owner.ProjectColor(c.Project));e.Graphics.FillEllipse(color,e.Bounds.X+12,e.Bounds.Y+20,12,12);
        using var titleFont=new Font("Segoe UI",11,FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics,c.Title,titleFont,new Rectangle(e.Bounds.X+36,e.Bounds.Y+12,e.Bounds.Width-50,25),Companion.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        var statusColor=c.Mood==Mood.Attention?Color.FromArgb(255,214,119):c.Mood==Mood.Done?Color.FromArgb(132,220,176):Color.FromArgb(177,175,198);
        TextRenderer.DrawText(e.Graphics,c.Status+"  ·  "+owner.ProjectName(c.Project),Font,new Rectangle(e.Bounds.X+36,e.Bounds.Y+41,e.Bounds.Width-50,24),statusColor,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        using var line=new Pen(Color.FromArgb(43,43,58));e.Graphics.DrawLine(line,e.Bounds.Left+12,e.Bounds.Bottom-1,e.Bounds.Right-12,e.Bounds.Bottom-1);
    }
}
