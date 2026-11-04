namespace Apocalypse
{
    public static class PortraitIdentity
    {
        public const int Count=12;
        public static int Index(string name)
        {
            uint hash=2166136261;foreach(char c in name){hash^=c;hash*=16777619;}
            bool female=name.EndsWith("서연")||name.EndsWith("하린")||name.EndsWith("민서")||name.EndsWith("유진");
            return (female?6:0)+(int)(hash%6);
        }
    }
}
