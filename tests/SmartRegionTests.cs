using System;
using System.Drawing;
using System.Linq;
using System.Threading;
namespace ScreenCraft
{
    static class SmartRegionTests
    {
        internal static void Run(Action<string,Action> run,Action<bool,string> check)
        {
            run("smart regions/nested controls window and monitor form distinct increasing ranges",delegate{
                var screen=new Rectangle(-1600,0,1600,900);var window=new Rectangle(-1400,100,850,650);var panel=new Rectangle(-1300,240,650,400);var button=new Rectangle(-1250,280,96,67);var point=new Point(-1220,300);
                var choices=SmartRegions.At(point,window,screen,screen,new[]{button,panel,button,new Rectangle(-100,1,40,40),new Rectangle(-1230,290,3,3)});
                check(choices.Length==4&&choices[0]==button&&choices[1]==panel&&choices[2]==window&&choices[3]==screen,"nested order, dedup or negative coordinates");
                check(SmartRegions.At(new Point(-1320,200),window,screen,screen,new[]{button,panel}).Length==2,"old child rectangle followed pointer outside control");
            });
            run("smart regions/overlarge provider rectangles clip to visible window and desktop",delegate{
                var desktop=new Rectangle(0,0,1200,800);var window=new Rectangle(-100,-40,700,500);var choices=SmartRegions.At(new Point(100,100),window,desktop,desktop,new[]{new Rectangle(-200,-200,2000,2000),Rectangle.Empty});
                check(choices.Length==2&&choices[0]==new Rectangle(0,0,600,460)&&choices[1]==desktop,"provider bounds escaped captured pixels");
            });
            run("smart regions/slow provider keeps only latest pending request on one worker",delegate{
                using(var entered=new ManualResetEvent(false))using(var release=new ManualResetEvent(false))using(var done=new ManualResetEvent(false)){
                    int obsolete=0,latest=0;SmartRegionWorker.Request(delegate{entered.Set();release.WaitOne();});check(entered.WaitOne(2000),"worker not started");
                    try{SmartRegionWorker.Request(delegate{Interlocked.Increment(ref obsolete);});SmartRegionWorker.Request(delegate{Interlocked.Increment(ref latest);done.Set();});}finally{release.Set();}
                    check(done.WaitOne(2000)&&obsolete==0&&latest==1,"stale requests queued or worker did not recover");
                }
            });
        }
    }
}
