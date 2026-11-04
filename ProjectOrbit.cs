using System.Drawing.Drawing2D;

namespace PawFamily;

public sealed class ProjectOrbit : Form
{
    readonly List<(Panel Bubble,Point Target,PetArt Pet,string Key)> bubbles=[];
    readonly System.Windows.Forms.Timer animation=new(){Interval=16};
    readonly System.Diagnostics.Stopwatch elapsed=new();
    readonly ToolTip tips=new();
    readonly Point center=new(230,230);
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool IsPreview {get;set;}
    public ProjectOrbit(IEnumerable<(string Key,string Name,int Count,Color Color)> projects,string animal,string pack,int page,int pages,Action<string> select,Action<string> color,Action<int> changePage)
    {
        Text="Paw Family · Projects"; FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
        AutoScaleMode=AutoScaleMode.None;ClientSize=new(460,460);BackColor=Companion.ClearKey;TransparencyKey=Companion.ClearKey;DoubleBuffered=true;
        var items=projects.ToArray();
        for(int i=0;i<items.Length;i++)
        {
            var item=items[i];var target=OrbitPosition(i,items.Length);
            var bubble=new OrbitBubble(item.Color){Size=new(148,148),Location=new(center.X-74,center.Y-74),Cursor=Cursors.Hand};
            var pet=new PetArt {Location=new(29,3),Size=new(90,90),Animal=animal,Pack=pack,Fur=item.Color};
            var label=new Label{Text=item.Name+"\n"+item.Count+(item.Count==1?" chat":" chats"),Location=new(5,101),Size=new(138,43),TextAlign=ContentAlignment.MiddleCenter,Font=new("Segoe UI",10,FontStyle.Bold),ForeColor=Color.White,BackColor=Color.Transparent,AutoEllipsis=true};
            void Open(object? s,EventArgs e)=>select(item.Key);
            pet.Click+=Open;label.Click+=Open;bubble.Click+=Open;
            var menu=new ContextMenuStrip();menu.Items.Add("Choose project color…",null,(_,_)=>color(item.Key));bubble.ContextMenuStrip=pet.ContextMenuStrip=label.ContextMenuStrip=menu;
            tips.SetToolTip(pet,item.Name+" · "+item.Count+" chats"); tips.SetToolTip(label,item.Name);
            bubble.Disposed+=(_,_)=>menu.Dispose();
            bubble.Controls.Add(pet);bubble.Controls.Add(label);Controls.Add(bubble);bubbles.Add((bubble,target,pet,item.Key));
        }
        var hub=new Label{Text=items.Length==0?"No local\nchats yet":"PROJECTS\n"+(pages>1?$"{page+1} / {pages}":"Choose a pet"),Size=new(110,54),Location=new(center.X-55,center.Y-27),TextAlign=ContentAlignment.MiddleCenter,Font=new("Segoe UI",9,FontStyle.Bold),BackColor=Companion.Panel,ForeColor=Companion.Ink};
        using(var shape=new GraphicsPath()){shape.AddEllipse(0,0,hub.Width,hub.Height);hub.Region=new Region(shape);} Controls.Add(hub);
        if(pages>1)
        {
            var previous=new Label{Text="‹",Location=new(178,255),Size=new(40,30),BackColor=Companion.Panel,ForeColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand,Font=new("Segoe UI",14)};
            var next=new Label{Text="›",Location=new(242,255),Size=new(40,30),BackColor=Companion.Panel,ForeColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand,Font=new("Segoe UI",14)};
            previous.Click+=(_,_)=>changePage(-1);next.Click+=(_,_)=>changePage(1);Controls.Add(previous);Controls.Add(next);
        }
        animation.Tick+=(_,_)=>Animate();Disposed+=(_,_)=>{animation.Dispose();tips.Dispose();};
    }
    protected override bool ShowWithoutActivation=>!IsPreview;
    protected override CreateParams CreateParams {get{var p=base.CreateParams;if(!IsPreview)p.ExStyle|=0x08000000|0x00000080;return p;}}
    internal void ArrangeForPreview(){foreach(var b in bubbles)b.Bubble.Location=b.Target;}
    public static Point OrbitPosition(int index,int count)
    {
        double angle=-Math.PI/2+2*Math.PI*index/Math.Max(1,count);
        return new(230+(int)(152*Math.Cos(angle))-74,230+(int)(152*Math.Sin(angle))-74);
    }
    public void Pop(Rectangle anchor)
    {
        var area=Screen.FromRectangle(anchor).WorkingArea;
        Location=new(Math.Clamp(anchor.Left+anchor.Width/2-Width/2,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(anchor.Top-Height+35,area.Top,Math.Max(area.Top,area.Bottom-Height)));
        if(Visible)return;
        Opacity=.15;elapsed.Restart();animation.Start();Show();
    }
    void Animate()
    {
        double t=Math.Clamp(elapsed.Elapsed.TotalMilliseconds/320,0,1), eased=1-Math.Pow(1-t,3);
        foreach(var b in bubbles)b.Bubble.Location=new(center.X-74+(int)((b.Target.X-center.X+74)*eased),center.Y-74+(int)((b.Target.Y-center.Y+74)*eased));
        Opacity=.15+.85*eased;if(t>=1)animation.Stop();
    }
    public void UpdatePets(double phase,Func<string,Mood> mood)
    {foreach(var b in bubbles){b.Pet.Phase=phase;b.Pet.Mood=mood(b.Key);b.Pet.Invalidate();}}
    sealed class OrbitBubble:Panel
    {
        readonly Color color;
        public OrbitBubble(Color color){this.color=color;BackColor=Companion.ClearKey;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Companion.ClearKey);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using var fill=new LinearGradientBrush(new Rectangle(25,0,98,98),Color.FromArgb(color.R/3,color.G/3,color.B/3),Companion.Panel,90);e.Graphics.FillEllipse(fill,25,0,98,98);
            using var ring=new Pen(color,2);e.Graphics.DrawEllipse(ring,25,0,98,98);
            using var chip=new SolidBrush(Companion.Panel);using var path=new GraphicsPath();path.AddArc(0,99,18,18,180,90);path.AddArc(130,99,18,18,270,90);path.AddArc(130,130,18,18,0,90);path.AddArc(0,130,18,18,90,90);path.CloseFigure();e.Graphics.FillPath(chip,path);
        }
    }
}
