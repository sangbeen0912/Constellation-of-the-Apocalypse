using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Apocalypse;

public class SimulationTests
{
    [Serializable] public class Summary
    {
        public int runs, wins, deaths, replacements, seals, totalDays, earnedDivinity, unlocked, totalActions, iterations;
        public double meanLifeDays, meanUnlocks, sealSuccessPerRun, winRate, meanDivinity;
        public int combatDeaths, starvationDeaths, breakdownDeaths;
        public System.Collections.Generic.List<int> winningSeeds=new System.Collections.Generic.List<int>();
        public string policy = "최고 기본 공격력 후보 선택, 식량200 확보 시 첫날 공격 투자, 식량200까지 무작위 보급, 체력60%·정신력50 미만 치료, 가능 전투 우선. 최대100일.";
    }
    [TestCase(100)] [TestCase(1000)]
    public void SeededRunsStayValidAndCanReachVictory(int runs)
    {
        var b=GameData.LoadBalance();var c=GameData.LoadContent();var report=new Summary{runs=runs};int lifeDays=0;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../TestResults"));Directory.CreateDirectory(output);
        for(uint seed=1;seed<=runs;seed++)
        {
            var g=GameEngine.New(seed,b,c);string initial=JsonUtility.ToJson(g.S);int budget=0;
            try
            {
                while(g.S.day<=100&&g.S.phase!=Phase.Victory)
                {
                    if(++budget>20000)throw new InvalidOperationException("Simulation step budget exhausted");
                    Advance(g);
                    g.Validate();
                }
                if(seed<=3)File.WriteAllText(Path.Combine(output,"campaign-sample-"+seed+".json"),JsonUtility.ToJson(g.S,true));
                report.iterations+=budget;report.totalDays+=g.S.day;report.earnedDivinity+=g.S.totalEarned;report.unlocked+=g.S.permanent.unlocked.Count;
                report.seals+=g.S.totalSeals;report.totalActions+=g.S.totalActions;report.deaths+=g.S.permanent.deaths.Count;report.replacements+=g.S.permanent.deaths.Count;
                foreach(var d in g.S.permanent.deaths){lifeDays+=d.day-d.selectedDay+1;if(d.combat)report.combatDeaths++;else if(d.cause.Contains("굶주림"))report.starvationDeaths++;else report.breakdownDeaths++;}
                if(g.S.phase==Phase.Victory){report.wins++;report.winningSeeds.Add((int)seed);}
            }
            catch(Exception e)
            {
                // Rebuild the exact pre-failure state only on failure. Serializing every
                // successful tick made the 1,000-run audit exceed Unity's timeout.
                // Initial IDs and RNG are retained, so this reproduces the old snapshot.
                var replay=new GameEngine(JsonUtility.FromJson<GameState>(initial),b,c);
                for(int step=1;step<budget;step++)Advance(replay);
                File.WriteAllText(Path.Combine(output,"failure-"+runs+"-seed-"+seed+".json"),JsonUtility.ToJson(replay.S,true));
                Assert.Fail("Seed "+seed+" / "+e);
            }
        }
        report.meanLifeDays=report.deaths==0?0:lifeDays/(double)report.deaths;report.meanUnlocks=report.unlocked/(double)runs;
        report.sealSuccessPerRun=report.seals/(double)runs;report.winRate=report.wins/(double)runs;report.meanDivinity=report.earnedDivinity/(double)runs;
        File.WriteAllText(Path.Combine(output,"simulation-"+runs+".json"),JsonUtility.ToJson(report,true));
        TestContext.WriteLine(JsonUtility.ToJson(report));Assert.Greater(report.wins,0,"정상 정책으로 도달 가능한 승리가 있어야 합니다.");
    }
    static void Advance(GameEngine g)
    {
        CampaignPolicy.Advance(g);
    }
}
