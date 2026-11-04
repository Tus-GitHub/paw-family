namespace PawFamily;

public sealed class AnimationPreview : Form
{
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 }; double phase;
    public AnimationPreview()
    {
        Text = "Paw Family — animation preview"; ClientSize = new(780,285); BackColor = Companion.Panel;
        StartPosition = FormStartPosition.CenterScreen; Font = new("Segoe UI",11); ForeColor = Companion.Ink;
        string[] titles = ["Walking left", "Walking right", "Needs your approval", "Completed"];
        for(int i=0;i<4;i++)
        {
            var pet = new PetArt {Location=new(22+i*193,38),Size=new(160,160), Pack=Path.Combine(AppContext.BaseDirectory,"Assets","cat"), Walking=i<2, FacingLeft=i==0, Mood=i==2?Mood.Attention:i==3?Mood.Done:Mood.Quiet};
            Controls.Add(pet); Controls.Add(new Label {Text=titles[i],Location=new(i*193,212),Size=new(193,35),TextAlign=ContentAlignment.MiddleCenter});
        }
        Controls.Add(new Label {Text="Animation preview only — no approval is being requested",Location=new(20,252),Size=new(740,25),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(179,177,197)});
        timer.Tick+=(_,_)=>{phase+=.08;foreach(var pet in Controls.OfType<PetArt>()){pet.Phase=phase;pet.Invalidate();}};timer.Start();
        FormClosed+=(_,_)=>timer.Dispose();
    }
}
