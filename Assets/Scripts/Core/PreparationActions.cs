using System;
namespace Apocalypse
{
    public sealed partial class GameEngine
    {
        public int UpgradeBatchCost(Stat stat,int count)
        {
            if(S.avatar==null||(stat!=Stat.Health&&stat!=Stat.Attack)||count<1||count>10)return -1;
            int total=0,completed=UpgradeCount(stat);for(int i=0;i<count;i++)total+=10*(1+(completed+i)/10);return total;
        }
        public bool UpgradeBatch(Stat stat,int count)
        {
            int cost=UpgradeBatchCost(stat,count);
            return cost>0&&S.divinity>=cost&&Invest(stat,cost);
        }
        public bool SupplyBatch(int count)
        {
            if(count<1||count>10||S.phase!=Phase.Morning||S.divinity<Rules.SupplyCost*count||Content.supplies.Count==0)return false;
            for(int i=0;i<count;i++)if(!Supply())throw new InvalidOperationException("Validated supply batch failed.");
            return true;
        }
    }
}

