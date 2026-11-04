using UnityEngine;

namespace Apocalypse
{
    // Stable name mapping also preserves the appearance of characters in existing saves.
    public static class ArtCatalog
    {
        static readonly string[] RelicIds={"gungnir","grail","solomon","necronomicon","antikythera","nebra","dagger","bicorne","yata-mirror","green-dragon-blade"};
        public static string PortraitKey(string name)
        {
            switch(name){case "카엘":return "Art/Portraits/kael-v1";case "네리사":return "Art/Portraits/nerissa-v1";case "베이르":return "Art/Portraits/beyr-v1";case "라이라":return "Art/Portraits/lyra-v1";}
            return "Art/Portraits/survivor-" + PortraitIdentity.Index(name).ToString("00") + "-v3";
        }
        public static Texture2D Relic(string id)
        {
            if(System.Array.IndexOf(RelicIds,id)<0)return null;
            return Resources.Load<Texture2D>("Art/Relics/"+id+"-v1");
        }

        public static Texture2D Portrait(string name) { return Resources.Load<Texture2D>(PortraitKey(name)); }
        public static readonly string[] FacilityNames={"의무실","예배방","보급창고","인재관리실"};
        public static readonly string[] EnvironmentNames={"clinic-v2","chapel-v2","warehouse-v2","personnel-v2","city-v2","rift-v2"};
        public static Texture2D Environment(int index) { return Resources.Load<Texture2D>("Art/Environments/"+EnvironmentNames[index]); }
        public static readonly string[] ConstellationIds={"sejong","joan","napoleon","zhuge","leonardo","alexander","tesla","hua","leonidas","musashi","curie","cleopatra","yi","genghis","artemis","heracles","arthur","zeus","hades","yog"};

        public static Texture2D Constellation(string id)
        {
            if (System.Array.IndexOf(ConstellationIds,id)<0) return null;
            string version=id=="napoleon"||id=="yog"?"-v1":"-v2";
            return Resources.Load<Texture2D>("Art/Constellations/" + id + version);
        }
    }
}
