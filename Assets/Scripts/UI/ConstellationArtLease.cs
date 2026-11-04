using System;
using System.Collections.Generic;
using UnityEngine;

namespace Apocalypse
{
    // Keep full-resolution pixel art only while a view displays it. Shared ownership
    // prevents closing one view from unloading an illustration used by another.
    public sealed class ConstellationArtLease : IDisposable
    {
        sealed class Entry { public Texture2D texture; public int owners; }
        static readonly Dictionary<string,Entry> resident=new Dictionary<string,Entry>();
        string id;
        public Texture2D Texture { get; private set; }
        public static int ResidentCount { get { return resident.Count; } }

        public Texture2D Use(string next)
        {
            if(next==id && Texture!=null)return Texture;
            Dispose();
            if(System.Array.IndexOf(ArtCatalog.ConstellationIds,next)<0)return null;
            if(!resident.TryGetValue(next,out var entry))
            {
                var texture=ArtCatalog.Constellation(next);
                if(texture==null)return null;
                entry=new Entry{texture=texture};resident.Add(next,entry);
            }
            entry.owners++;id=next;Texture=entry.texture;return Texture;
        }

        public void Dispose()
        {
            if(id!=null && resident.TryGetValue(id,out var entry) && --entry.owners==0)
            {
                resident.Remove(id);
                if(entry.texture!=null)Resources.UnloadAsset(entry.texture);
            }
            id=null;Texture=null;
        }
    }
}
