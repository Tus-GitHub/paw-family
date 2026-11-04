using System.Text.Json;

namespace PawFamily;

static class SelfTest
{
    public static void Run()
    {
        int count=0;
        void Check(bool ok,string name) { if(!ok)throw new Exception("FAIL: "+name); count++; }
        string Record(string type,object payload)=>JsonSerializer.Serialize(new {type,timestamp="2026-11-04T10:00:00Z",payload});
        var chat=new Chat(); Records.Apply(chat,Record("session_meta",new{id="11111111-1111-1111-1111-111111111111",cwd=@"C:\Projects\A"}));
        Check(chat.WorkingDirectory==@"C:\Projects\A","working directory metadata");
        Records.Apply(chat,Record("event_msg",new{type="task_started"}));Check(chat.Mood==Mood.Working,"start");
        Records.Apply(chat,Record("response_item",new{type="function_call",name="functions.request_permissions",call_id="a"}));Check(chat.Mood==Mood.Attention,"permission request");
        Records.Apply(chat,Record("response_item",new{type="function_call_output",call_id="other"}));Check(chat.Mood==Mood.Attention,"unrelated tool output preserves wait");
        Records.Apply(chat,Record("response_item",new{type="function_call_output",call_id="a"}));Check(chat.Mood==Mood.Working,"answered permission clears wait");
        Records.Apply(chat,Record("event_msg",new{type="task_complete"}));Check(chat.Mood==Mood.Done,"completion");
        Records.Apply(chat,Record("response_item",new{type="message",role="user",content="permission approval"}));Check(chat.Mood==Mood.Done,"ordinary prose not treated as approval");
        Check(!Records.Apply(chat,"{partial"),"partial JSON tolerated");
        Check(Records.Link(chat)=="codex://threads/11111111-1111-1111-1111-111111111111","canonical chat link");
        Check(Records.Aggregate([chat,new Chat{Mood=Mood.Attention}])==Mood.Attention,"attention priority");
        var asyncChat=new Chat();
        Records.Apply(asyncChat,Record("response_item",new{type="function_call",name="request_user_input_async",call_id="question"}));
        Records.Apply(asyncChat,Record("response_item",new{type="function_call_output",call_id="question",output="{\"accepted\":true}"}));
        Check(asyncChat.Mood==Mood.Attention,"queued question stays pending after tool returns");
        Records.Apply(asyncChat,Record("event_msg",new{type="task_complete"}));Check(asyncChat.Mood==Mood.Attention,"final response preserves unanswered question");
        Records.Apply(asyncChat,Record("event_msg",new{type="user_message"}));Check(asyncChat.Mood==Mood.Working&&asyncChat.PendingQuestions.Count==0,"user reply clears question alert");
        Records.Apply(asyncChat,Record("response_item",new{type="function_call",name="request_user_input_async",call_id="rejected"}));
        Records.Apply(asyncChat,Record("response_item",new{type="function_call_output",call_id="rejected",output="{\"accepted\":false}"}));
        Check(asyncChat.PendingQuestions.Count==0&&asyncChat.Mood==Mood.Working,"rejected question is not left pending");
        var root=Path.Combine(AppContext.BaseDirectory,"test-fixtures",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,"sessions"));
        var file=Path.Combine(root,"sessions","rollout-test.jsonl");
        File.WriteAllLines(file,[Record("session_meta",new{id=chat.Id,cwd=@"C:\Projects\A"}),Record("event_msg",new{type="task_complete"})]);
        File.WriteAllText(Path.Combine(root,"session_index.jsonl"),JsonSerializer.Serialize(new{id=chat.Id,thread_name="Readable title"})+"\n");
        var monitor=new SessionMonitor(root);Check(monitor.Scan().Single().Title=="Readable title","title index");
        File.AppendAllText(file,Record("event_msg",new{type="request_user_input"})+"\n");Check(monitor.Scan().Single().Mood==Mood.Attention,"changed session refresh");
        File.WriteAllLines(file,[Record("session_meta",new{id=chat.Id,cwd=@"C:\Projects\A"}),Record("event_msg",new{type="task_complete"})]);Check(monitor.Scan().Single().Mood==Mood.Done,"truncated session refresh");
        var bounds = new Rectangle(-100, 0, 800, 600); var size = new Size(140,140); var position = new Point(280,400);
        var motion = new RoamingMotion(7);
        var step = motion.Step(position,bounds,size,5,.05,true,false,Mood.Quiet); Check(step.Walking,"roaming starts after pause");
        step = motion.Step(step.Location,bounds,size,5.1,.05,true,true,Mood.Quiet); Check(!step.Walking,"hover stops roaming");
        step = motion.Step(position,bounds,size,10,.05,true,false,Mood.Attention); Check(!step.Walking && step.Location == position,"approval stops movement immediately");
        step = motion.Step(position,bounds,size,14,.05,false,false,Mood.Quiet); Check(!step.Walking,"walk toggle disables movement");
        step = motion.Step(position,bounds,size,20,.05,true,false,Mood.Done); Check(!step.Walking,"celebration stays in place");
        var atEdge = new RoamingMotion(9); step = atEdge.Step(new Point(bounds.Right-size.Width,400),bounds,size,5,.05,true,false,Mood.Working);
        Check(step.FacingLeft && step.Location.X <= bounds.Right-size.Width,"turn away from right edge");
        for(int i=1;i<3000;i++) { step = atEdge.Step(step.Location,bounds,size,5+i*.05,.05,true,false,Mood.Quiet); Check(step.Location.X>=bounds.Left && step.Location.X<=bounds.Right-size.Width && step.Location.Y>=bounds.Top && step.Location.Y<=bounds.Bottom-size.Height,"stays inside screen"); }
        step = new RoamingMotion().Step(new Point(50,50),new Rectangle(0,0,100,100),size,10,.1,true,false,Mood.Quiet); Check(!step.Walking && step.Location == Point.Empty,"small screen stays bounded");
        using(var metadata=JsonDocument.Parse("""{"local-projects":{"a":{"name":"Alpha","rootPaths":["C:\\Projects\\A"]},"b":{"name":"Nested","rootPaths":["C:\\Projects\\A\\Nested"]}},"projectless-thread-ids":["loose"],"thread-project-assignments":{"explicit":{"projectId":"a"}}}"""))
        {
            var catalog=ProjectCatalog.Parse(metadata.RootElement);
            var a=new Chat{Id="one",WorkingDirectory=@"C:\Projects\A\src"};catalog.Classify(a);Check(a.ProjectLabel=="Alpha","subdirectories group into actual project");
            a=new Chat{Id="loose",WorkingDirectory=@"C:\Projects\A"};catalog.Classify(a);Check(a.Project=="Others","explicit projectless chat stays Others");
            a=new Chat{Id="random",WorkingDirectory=@"C:\Users\User\Documents\Codex\2026-11-04\one-chat"};catalog.Classify(a);Check(a.Project=="Others","generated workspace is not a project");
            a=new Chat{Id="explicit",WorkingDirectory=@"C:\worktree\elsewhere"};catalog.Classify(a);Check(a.ProjectLabel=="Alpha","assigned project overrides working folder");
            a=new Chat{Id="nested",WorkingDirectory=@"C:\Projects\A\Nested\src"};catalog.Classify(a);Check(a.ProjectLabel=="Nested","longest registered root");
            a=new Chat{Id="sibling",WorkingDirectory=@"C:\Projects\AB"};catalog.Classify(a);Check(a.Project=="Others","path boundary excludes siblings");
            a=new Chat{Id="upper",WorkingDirectory=@"c:\PROJECTS\a\SRC"};catalog.Classify(a);Check(a.ProjectLabel=="Alpha","case insensitive Windows roots");
        }
        for(int n=1;n<=6;n++)for(int i=0;i<n;i++){var pos=ProjectOrbit.OrbitPosition(i,n);Check(pos.X>=0&&pos.Y>=0&&pos.X+148<=460&&pos.Y+148<=460,"orbit stays within transparent surface");}
        using(var shared=JsonDocument.Parse("""{"local-projects":{"a":{"name":"Alpha","rootPaths":["C:\\shared"]},"b":{"name":"Beta","rootPaths":["C:\\shared"]}},"thread-project-assignments":{"one":{"projectId":"a"},"two":{"projectId":"b"}}}"""))
        {
            var catalog=ProjectCatalog.Parse(shared.RootElement);var one=new Chat{Id="one"};var two=new Chat{Id="two"};catalog.Classify(one);catalog.Classify(two);Check(one.Project!=two.Project,"separate projects sharing a folder keep separate kittens");
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test-result.txt"),$"PASS: {count} assertions (parser, project classification, roaming boundaries, and orbit layout)\n");
    }
}
