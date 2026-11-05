using System.Linq;
namespace Apocalypse
{
    // Declared QA player policy, not NPC autonomy or a hidden game difficulty modifier.
    // Shares the same public commands as a player: no resource/stat/roll overrides.
    public static class CampaignPolicy
    {
        public static void Advance(GameEngine g)
        {
            var s=g.S;
            switch(s.phase)
            {
                case Phase.Selection:case Phase.Death:
                    g.Select(s.candidates.OrderByDescending(c=>c.baseAttack).First().id);break;
                case Phase.Morning:
                    if(s.avatar.selectedDay==s.day&&s.food>=200&&s.divinity>0)g.Invest(Stat.Attack,s.divinity);
                    for(int i=0;i<400&&s.food<200&&s.divinity>0;i++)g.Supply();
                    if(s.avatar.health<s.avatar.MaxHealth*.6)g.Treat(false,false,s.medicine>0);
                    if(s.avatar==null)break;
                    if(s.avatar.sanity<50)g.Treat(true,false,s.syringes>0);
                    if(s.avatar==null)break;
                    if(s.divinity>50)
                    {
                        // Rising prices reward buying both stats instead of spending every
                        // point on increasingly costly attack. Uses public commands only.
                        int budget=s.divinity-5;
                        g.Invest(Stat.Health,budget/2);
                        if(s.divinity>5)g.Invest(Stat.Attack,s.divinity-5);
                    }
                    g.StartDay(Goal.Rift);break;
                case Phase.Event:
                    var e=s.pending;int choice=g.AutomaticChoice(e);
                    if(!s.controlLocked&&e.options!=null)
                    {
                        int combat=Enumerable.Range(0,e.options.Length).Where(i=>g.CanChoose(e,i,out _)&&(e.options[i].normal>0||e.options[i].special>0||e.options[i].boss))
                            .OrderByDescending(i=>e.options[i].boss?100:e.options[i].special*3+e.options[i].normal).DefaultIfEmpty(-1).First();
                        if(combat>=0)choice=combat;
                        else if(e.options.Any(o=>o.divinity))choice=e.options.Length-1;
                    }
                    g.Resolve(e.id,choice);break;
                case Phase.Running:g.Tick();break;
                case Phase.Report:g.NextDay();break;
            }
        }
    }
}

