using System.Drawing.Imaging;

namespace PawFamily;

static class Preview
{
    internal static ProjectOrbit CreateOrbit()=>new(new[]{("web","Website",4,Color.CornflowerBlue),("travel","Travel",2,Color.FromArgb(143,206,170)),("Others","Others",5,Color.SandyBrown),("research","Research",3,Color.Orchid)},"cat","",0,1,_=>{},_=>{},_=>{});
    public static void Render()
    {
        using var image=new Bitmap(1120,680);using var g=Graphics.FromImage(image);
        g.Clear(Color.FromArgb(18,20,30));
        using var white=new SolidBrush(Companion.Ink);using var muted=new SolidBrush(Color.FromArgb(175,180,202));
        using var title=new Font("Segoe UI",34,FontStyle.Bold);using var body=new Font("Segoe UI",16);using var small=new Font("Segoe UI",11);
        g.DrawString("Your projects.\nA little family.",title,white,50,52);
        g.DrawString("Hover to meet your project pets.\nClick a kitten to find its chats.",body,muted,54,194);
        using(var pet=new PetArt{Size=new(220,220),Mood=Mood.Attention,Phase=.6,BackColor=Companion.ClearKey})
        using(var bitmap=new Bitmap(220,220)){pet.DrawToBitmap(bitmap,new Rectangle(0,0,220,220));bitmap.MakeTransparent(Companion.ClearKey);g.DrawImage(bitmap,105,292);}
        g.DrawString("A raised paw means a chat needs you.",small,white,54,536);
        g.DrawString("Paw Family · v0.3 preview",small,muted,54,610);
        using(var orbit=CreateOrbit())
        using(var bitmap=new Bitmap(460,460))
        {
            orbit.Location=new(-20000,-20000);orbit.Show();orbit.ArrangeForPreview();orbit.UpdatePets(.6,key=>key=="web"?Mood.Attention:key=="research"?Mood.Done:Mood.Quiet);
            orbit.DrawToBitmap(bitmap,new Rectangle(0,0,460,460));bitmap.MakeTransparent(Companion.ClearKey);g.DrawImage(bitmap,590,75);orbit.Close();
        }
        g.DrawString("One kitten per project. Loose chats in Others.",small,white,593,567);
        g.DrawString("Actual controls and original artwork · illustrative project names",small,muted,593,610);
        image.Save(Path.Combine(AppContext.BaseDirectory,"preview.png"),ImageFormat.Png);
    }
    public static void ExportIcon()
    {
        int[] sizes=[16,32,48,256];var images=new List<byte[]>();
        foreach(int size in sizes)
        {
            using var pet=new PetArt{Size=new(size,size),BackColor=Companion.ClearKey};
            using var bitmap=new Bitmap(size,size);pet.DrawToBitmap(bitmap,new Rectangle(0,0,size,size));bitmap.MakeTransparent(Companion.ClearKey);
            using var stream=new MemoryStream();bitmap.Save(stream,ImageFormat.Png);images.Add(stream.ToArray());
        }
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"paw-family.ico"));using var writer=new BinaryWriter(output);
        writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
        for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]%256));writer.Write((byte)(sizes[i]%256));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}
        foreach(var data in images)writer.Write(data);
    }
    public static void RenderChat()
    {
        using var owner=new Companion(true);using var window=new ChatWindow(owner);
        window.SelectProject(null);window.UpdateChats(Companion.DemoChats(),"DEMO · Sample chats");window.Location=new(-20000,-20000);window.Show();window.PerformLayout();Application.DoEvents();
        using var bitmap=new Bitmap(window.Width,window.Height);window.DrawToBitmap(bitmap,new Rectangle(Point.Empty,window.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory,"chat-preview.png"),ImageFormat.Png);
    }
}
