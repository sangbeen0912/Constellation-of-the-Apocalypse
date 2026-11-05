using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Apocalypse
{
    public sealed partial class GameApp : MonoBehaviour
    {
        public GameObject buttonPrefab, panelPrefab;
        public GameEngine Game { get; private set; }
        public SaveManager Saves { get; private set; }
        [NonSerialized] public string saveDirectoryOverride;
        Canvas canvas;
        Font font;
        Balance balance;
        Content content;
        string tab = "쉘터", notice = "";
        bool title = true, confirmNew, busy;
        int speed = 1, visibleLogs;
        float clock;
        Goal goal;
        readonly Color bg = new Color(.027f,.032f,.031f), panel = new Color(.075f,.083f,.079f);
        readonly Color gold = new Color(.89f,.72f,.39f), muted = new Color(.60f,.63f,.65f), ink = new Color(.89f,.88f,.83f);
        Texture2D city, gate;
        readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        readonly Dictionary<int, Texture2D> facilities = new Dictionary<int, Texture2D>();
        readonly Dictionary<string,float> scrollPositions = new Dictionary<string,float>();
        readonly CodexIndex codex = new CodexIndex();
        readonly ConstellationArtLease constellationArt = new ConstellationArtLease();
        bool constellationArtUsed;
        ScrollRect activeScroll;
        string scrollKey;
        public CodexIndex Codex { get { return codex; } }
        public string ActiveTab { get { return tab; } }
        public void Navigate(string destination) { choosingExploration=false;tab = destination; codexPage=personnelPage=relicPage=0; Render(); }
        public void Start()
        {
            if (canvas != null) return;
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Environment.GetCommandLineArgs().Contains("-smoke-test"))
            {
                string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args,"-qa-output");
                saveDirectoryOverride = System.IO.Path.Combine(at >= 0 && at + 1 < args.Length ? args[at+1] : Application.temporaryCachePath,"SmokeSaves-"+Guid.NewGuid().ToString("N"));
                gameObject.AddComponent<SmokeRunner>();
            }
            #endif
            Application.targetFrameRate = 60;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            balance = GameData.LoadBalance(); content = GameData.LoadContent(); Saves = new SaveManager(string.IsNullOrEmpty(saveDirectoryOverride) ? Application.persistentDataPath : saveDirectoryOverride);
            var go = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            city = ArtCatalog.Environment(4); gate = ArtCatalog.Environment(5); Render();
        }
        public void BeginNew(uint seed = 0)
        {
            var fresh=GameEngine.New(seed == 0 ? (uint)DateTime.UtcNow.Ticks : seed,balance,content);
            if(!Saves.ResetProgress(fresh.S)){notice="새로 시작 저장 실패 · "+Saves.LastError;confirmNew=false;Render();return;}
            ClearBattles();
            ReleaseOmen();Attach(fresh);ResetNewGameView();
            title = false; confirmNew = false; tab = "쉘터";notice=Saves.LastError;Render();
        }
        public bool LoadGame()
        {
            var state = Saves.Read();
            if (state == null) { notice = "불러오기 실패: " + Saves.LastError; Render(); return false; }
            ClearBattles();transitWalking=choosingExploration=false;Attach(new GameEngine(state, balance, content)); title = false;confirmNew=false; tab = "쉘터"; notice = Saves.LastError; Render(); return true;
        }
        void Attach(GameEngine game)
        {
            if(Game!=null)Game.Checkpoint-=Save;Game = game; Game.InteractiveCombat=true; Game.Checkpoint += Save; visibleLogs = Game.S.logs.Count; goal = Game.S.goal;
        }
        bool saveQueued;
        void Save(){saveQueued=true;}
        void FlushSave(){if(!saveQueued||Game==null)return;saveQueued=false;if(!Saves.Write(Game.S))notice="저장 실패 · "+Saves.LastError;}
        void LateUpdate(){FlushSave();}
        void Commit() { Save();FlushSave(); Render(); }
        void Act(Action action)
        {
            if (busy) return; busy = true;
            try { RecordBattles(action); Commit(); } finally { busy = false; }
        }
        void Update()
        {
            if(title){if(confirmNew&&Input.GetKeyDown(KeyCode.Escape)){confirmNew=false;Render();}return;}
            if (Game == null) return;
            if (UpdateTransitWalk()) return;
            if (UpdateBattlePresentation()) return;
            if (UpdateOmen()) return;
            if(Input.GetKeyDown(KeyCode.Escape)){var back=BackNavigation(out _);if(back!=null){back();return;}}
            clock += Time.unscaledDeltaTime;
            float delay = speed == 0 ? .8f : speed == 1 ? .2f : 0;
            if (clock < delay) return;
            clock = 0;
            if(Game.S.phase==Phase.Event&&Game.S.controlLocked&&Game.S.pending!=null)
            {ResolveEventChoice(Game.S.pending.id,Game.AutomaticChoice(Game.S.pending));return;}
            if (visibleLogs < Game.S.logs.Count) { visibleLogs = speed == 2 ? Game.S.logs.Count : visibleLogs + 1; if(logBody!=null)RefreshLogRows(); return; }
            if (Game.S.phase == Phase.Running) { Game.Tick(); if(IsFieldPhase&&fieldMain!=null)RefreshEncounter();else Render(); }
        }
        void OnApplicationQuit() { if (Game != null){Save();FlushSave();} }
        void OnDestroy()
        {
            DetachIllustration();
            constellationArt.Dispose();
            ReleaseOmen();
            ClearBattles();
            if(battleInterior!=null){Resources.UnloadAsset(battleInterior);battleInterior=null;}
            if(areaAtlas!=null){Resources.UnloadAsset(areaAtlas);areaAtlas=null;}
            if (font != null) Destroy(font);
        }
        void DetachIllustration()
        {
            if(canvas==null || constellationArt.Texture==null)return;
            foreach(var image in canvas.GetComponentsInChildren<RawImage>(true))
                if(image.texture==constellationArt.Texture)image.texture=null;
        }
        RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = panelPrefab != null ? Instantiate(panelPrefab, parent) : new GameObject(name, typeof(RectTransform), typeof(Image));
            go.name = name; if (go.transform.parent != parent) go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            if(color.a>.5f){var edge=go.AddComponent<Outline>();edge.effectColor=new Color(.25f,.25f,.23f);edge.effectDistance=new Vector2(1,-1);}
            return r;
        }
        Text Label(Transform parent, string value, int size = 25, float height = 46, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement)); go.transform.SetParent(parent,false);
            var t = go.GetComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.color = color ?? ink;
            t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            go.GetComponent<LayoutElement>().preferredHeight = height; return t;
        }
        Text Positioned(Transform parent, string text, Vector2 min, Vector2 max, int size, Color? color = null)
        {
            var t = Label(parent,text,size,0,color); var r = t.rectTransform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return t;
        }
        public Button MakeButton(Transform parent, string text, Action click, bool enabled = true, bool accent = false)
        {
            GameObject go;
            if (buttonPrefab != null) go = Instantiate(buttonPrefab, parent);
            else
            {
                go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)); go.transform.SetParent(parent, false);
                var t = Label(go.transform,text,23,0); t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(20,0); t.rectTransform.offsetMax = new Vector2(-20,0);
            }
            go.name = "Button " + text;
            go.GetComponent<Image>().color = Color.clear;
            var label = go.GetComponentInChildren<Text>(); label.font = font; label.text = text; label.color = enabled ? ink : muted; label.fontSize = 21;
            var layout = go.GetComponent<LayoutElement>(); if (layout == null) layout = go.AddComponent<LayoutElement>(); layout.preferredHeight = 38; layout.flexibleWidth = 1;
            var edge = go.GetComponent<Outline>(); if (edge == null) edge = go.AddComponent<Outline>();
            edge.effectColor = accent ? new Color(.68f,.54f,.32f) : new Color(.23f,.29f,.32f); edge.effectDistance = new Vector2(1,-1);
            label.rectTransform.offsetMin=new Vector2(28,0);label.rectTransform.offsetMax=new Vector2(-12,0);
            edge.enabled=false;
            var surface=Plate(go.GetComponent<RectTransform>(),accent?new Color(.28f,.24f,.15f):new Color(.14f,.17f,.15f),true,accent);
            var b = go.GetComponent<Button>();b.targetGraphic=surface; b.interactable = enabled; b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => click()); return b;
        }
        RectTransform Stack(Transform parent, int padding = 24)
        {
            var r = Box(parent,"Stack",Vector2.zero,Vector2.one,Color.clear);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(padding,padding,padding,padding); v.spacing = 8;
            v.childControlHeight = true; v.childForceExpandHeight = false; v.childControlWidth = true; v.childForceExpandWidth = true; return r;
        }
        RectTransform Scroller(Transform parent)
        {
            var viewport = Box(parent,"Viewport",Vector2.zero,Vector2.one,Color.clear); viewport.offsetMax = new Vector2(-20,0); viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = parent.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 48; scroll.movementType = ScrollRect.MovementType.Clamped;
            var r = Stack(viewport); r.anchorMin = new Vector2(0,1); r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f,1);
            var fitter = r.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = r; scroll.viewport = viewport;
            var rail = Box(parent,"Scroll track",new Vector2(1,0),Vector2.one,new Color(.04f,.06f,.08f)); rail.offsetMin = new Vector2(-14,8); rail.offsetMax = new Vector2(-4,-8);
            var handle = Box(rail,"Scroll handle",Vector2.zero,Vector2.one,new Color(.40f,.47f,.49f));
            var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop; bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>();
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            activeScroll = scroll; return r;
        }
        RectTransform Row(Transform parent, float height = 58)
        {
            var go = new GameObject("Row",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(LayoutElement)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var h = go.GetComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childControlWidth = true; h.childForceExpandWidth = true;
            h.childControlHeight = true; h.childForceExpandHeight = true;
            var element=go.GetComponent<LayoutElement>();element.preferredHeight=height;element.minHeight=height;element.flexibleHeight=0;
            return go.GetComponent<RectTransform>();
        }
        void Art(Transform parent, Texture2D texture, float height, bool fitWhole = false)
        {
            if(texture==null){Label(parent,"이미지를 불러오지 못했습니다.",20,height,muted);return;}
            var go = new GameObject("Pixel illustration",typeof(RectTransform),typeof(LayoutElement)); go.transform.SetParent(parent,false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var picture = new GameObject("Artwork",typeof(RectTransform),typeof(RawImage)); picture.transform.SetParent(go.transform,false);
            var rect = picture.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            picture.GetComponent<RawImage>().texture = texture;
            go.AddComponent<RectMask2D>();
            var aspect = picture.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = fitWhole || texture.width == texture.height ? AspectRatioFitter.AspectMode.FitInParent : AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio=texture.width/(float)texture.height;
        }
        Texture2D Portrait(Character c)
        {
            if (!portraits.ContainsKey(c.name)) portraits[c.name] = ArtCatalog.Portrait(c.name);
            return portraits[c.name];
        }
        Texture2D Facility(int index)
        { if (!facilities.ContainsKey(index)) facilities[index] = ArtCatalog.Environment(index); return facilities[index]; }
        string Personality(Character c) { return PersonalityNames[(int)c.personality] + (c.species==Species.Human?(c.mutant?" · 돌연변이":""):" · "+SpeciesName(c.species)); }
        void Meter(Transform parent, string name, double value, double max, Color color)
        {
            var row = Row(parent,30); var layout = row.GetComponent<HorizontalLayoutGroup>(); layout.childForceExpandWidth = false; layout.childForceExpandHeight = false; layout.childAlignment = TextAnchor.MiddleLeft;
            var label = Label(row, name + "  " + Rules.Round(value) + " / " + Rules.Round(max),19,30);
            label.horizontalOverflow=HorizontalWrapMode.Overflow;
            var textLayout = label.GetComponent<LayoutElement>(); textLayout.minWidth = 185; textLayout.preferredWidth = 185; textLayout.flexibleWidth = 1;
            var track = Box(row,"Meter",Vector2.zero,Vector2.one,new Color(.1f,.14f,.18f));
            var barLayout = track.gameObject.AddComponent<LayoutElement>(); barLayout.minWidth = 72; barLayout.preferredWidth = 72; barLayout.minHeight = 16; barLayout.preferredHeight = 16;
            var fill=Box(track,"Fill",Vector2.zero,new Vector2((float)Math.Max(0,Math.Min(1,value/max)),1),color);
            hudMeters.Add(new HudMeter{name=name,text=label,fill=fill});
        }
        public void Render()
        {
            if (canvas == null) return;
            if(!title&&fieldMain!=null&&IsFieldPhase&&!OmenVisible&&omenTexture==null){RefreshEncounter();if(transitWalking)RenderTransitWalk(fieldMain);return;}
            fieldMain=null;fieldContentKey=fieldArtKey=null;
            logBody=null;logRows.Clear();hudMeters.Clear();explorationStatus=null;
            if (activeScroll != null && scrollKey != null) scrollPositions[scrollKey] = activeScroll.verticalNormalizedPosition;
            activeScroll = null;
            foreach (Transform child in canvas.transform) { if(child==battleLayer)continue;child.gameObject.SetActive(false); Destroy(child.gameObject); }
            DetachIllustration();
            constellationArtUsed=false;
            try
            {
            var root = Box(canvas.transform,"Background",Vector2.zero,Vector2.one,bg);
            root.SetAsFirstSibling();
            Positioned(root,"종말의 성좌",new Vector2(.025f,.925f),new Vector2(.37f,.985f),34,gold);
            Positioned(root,"CONSTELLATION OF THE APOCALYPSE",new Vector2(.027f,.90f),new Vector2(.4f,.93f),14,muted);
            if (title) { RenderTitle(root); return; }
            var s = Game.S;
            RenderResourceHeader(root);
            var side = Stack(Box(root,"Sidebar",new Vector2(.015f,.14f),new Vector2(.19f,.885f),panel),12);
            Plate((RectTransform)side.parent,panel);
            side.GetComponent<VerticalLayoutGroup>().spacing = 6;
            if (s.avatar != null)
            {
                RenderDossier(side);
            }
            else Label(side,"새로운 별의 그릇을 찾습니다.",22,64,gold);
            foreach (string item in new[] { "쉘터", "전투준비", "하루 기록", "성좌 도감", "기록 보관소" })
            { string t = item; MakeButton(side,t,() => Navigate(t),true,tab == t || t=="쉘터"&&Array.IndexOf(ArtCatalog.FacilityNames,tab)>=0).GetComponent<LayoutElement>().preferredHeight = 40; }
            var bottom = Row(side,44); MakeButton(bottom,"저장",() => { Save(); notice = string.IsNullOrEmpty(Saves.LastError) ? "자동 저장 완료" : Saves.LastError; Render(); });
            MakeButton(bottom,"타이틀",() => { title = true; Render(); });
            scrollKey = s.phase == Phase.Event ? "event:"+s.pending.id : s.phase == Phase.Selection || s.phase == Phase.Death ? "selection" : tab == "성좌 도감" ? "codex:"+(codex.SelectedId ?? "list") : tab;
            var main=Box(root,"Main",new Vector2(.205f,.14f),new Vector2(.985f,.885f),panel);
            Plate(main,panel);
            bool preparation=tab=="전투준비"&&s.phase!=Phase.Selection&&s.phase!=Phase.Death&&s.phase!=Phase.Event&&s.phase!=Phase.Victory&&s.phase!=Phase.Report;
            bool runningLog=s.phase==Phase.Running&&tab!="성좌 도감"&&tab!="기록 보관소"&&!preparation;
            bool historyScreen=!IsFieldPhase&&(s.phase==Phase.Morning||s.phase==Phase.Running)&&(tab=="하루 기록"||tab=="기록 보관소"||s.phase==Phase.Running);
            var body = historyScreen ? Scroller(main) : main;
            if(runningLog&&body!=main)body.parent.GetComponent<RectTransform>().offsetMin=new Vector2(0,90);
            if (s.phase == Phase.Selection || s.phase == Phase.Death) RenderSelectionScreen(main);
            else if (IsFieldPhase) RenderEncounter(main);
            else if (s.phase == Phase.Victory) RenderVictory(Stack(main));
            else if (s.phase == Phase.Report) { tab="쉘터"; RenderFacilityScreen(main); }
            else if (tab == "성좌 도감") RenderCodexScreen(main);
            else if (preparation) RenderPreparation(body);
            else if (tab == "기록 보관소") RenderRecords(body);
            else if (tab == "하루 기록" || s.phase == Phase.Running) RenderLogs(body);
            else RenderFacilityScreen(main);
            timeStatus=Positioned(root,string.IsNullOrEmpty(notice)?Rules.Clock(s.hours)+" · 남은 "+Math.Max(0,Rules.DayHours-s.hours).ToString("0.0")+"시간":notice,new Vector2(.025f,.025f),new Vector2(.19f,.12f),21,gold);
            var footer = Row(Box(root,"Footer",new Vector2(.44f,.045f),new Vector2(.985f,.095f),Color.clear),38);
            foreach (int n in new[] {0,1,2}) { int selected = n; MakeButton(footer,new[] { "일반 · 0.8초", "빠르게 · 0.2초", "즉시 표시" }[n],() => { speed = selected; Render(); },true,speed == n); }
            if(IsFieldPhase)RefreshEncounter();
            RenderBackNavigation(root);RenderOmen(root);RenderBattleScene();RenderTransitWalk(root);
            Canvas.ForceUpdateCanvases();
            if (activeScroll != null) activeScroll.verticalNormalizedPosition = scrollPositions.ContainsKey(scrollKey) ? scrollPositions[scrollKey] : 1;
            }
            finally { if(!constellationArtUsed)constellationArt.Dispose(); }
        }
        void RenderTitle(Transform root)
        {
            var left = Stack(Box(root,"Title artwork",new Vector2(.025f,.10f),new Vector2(.63f,.85f),panel),30);
            Art(left,city,380); Label(left,"무너진 세계,\n아직 꺼지지 않은 별.",48,155,gold);
            Label(left,"화신체를 후원하고 일곱 악마를 동시에 봉인하십시오.\n당신의 계약은 죽음 너머에도 기록됩니다.",25,95);
            var right = Stack(Box(root,"Title actions",new Vector2(.65f,.10f),new Vector2(.975f,.85f),panel),35);
            Label(right,"성좌의 관측소",34,80,gold); Label(right,"자동 행동 · 선택 · 생존 · 계승",23,65,muted);
            MakeButton(right,"이어하기",() => LoadGame(),Saves.Exists&&!confirmNew,true);
            MakeButton(right,"새로 시작",() => {confirmNew=true;Render();},!confirmNew);
            Label(right,"무작위 생존자 한 명과 첫 계약을 맺습니다.\n쉘터에서 준비한 뒤 하루 목표를 정하고\n화신체의 자동 행동을 지켜보십시오.\n중요 이벤트에서만 운명에 개입합니다.",24,210);
            if (!string.IsNullOrEmpty(notice)) Label(right,notice,19,100,gold);
            if(confirmNew)RenderNewGameWarning(root);
        }
        void RenderLogs(Transform body)
        {
            if(Game.S.lastNamedDay==Game.S.day&&!string.IsNullOrEmpty(Game.S.lastNamedName))
            {
                Label(body,"네임드 조우 · 돌아온 화신체",30,55,gold);
                var reveal=Row(body,136);Art(reveal,ArtCatalog.Portrait(Game.S.lastNamedName),128,true);
                Label(reveal,"네임드 좀비 · "+Game.S.lastNamedName+"\n과거 화신체가 악마군단의 적으로 돌아왔다.",25,120,gold);
            }
            RenderExplorationControls(body);
            Label(body,"오늘의 관측 기록",36,65,gold);
            Label(body,Game.S.phase == Phase.Running ? "화신체가 자동으로 행동하고 있습니다." : "전투준비에서 오늘의 활동을 선택하고 진행합니다.",23,45,muted);
            logBody=body;RefreshLogRows();
        }
        Transform logBody;
        readonly List<Text> logRows=new List<Text>(60);
        void RefreshLogRows()
        {
            int end=Math.Min(visibleLogs,Game.S.logs.Count),count=Math.Min(60,end);
            while(logRows.Count<count){var row=Label(logBody,"",22,52);row.name="Observation log row";logRows.Add(row);}
            for(int i=0;i<logRows.Count;i++)
            {
                bool used=i<count;if(logRows[i].gameObject.activeSelf!=used)logRows[i].gameObject.SetActive(used);
                if(used)logRows[i].text=Game.S.logs[end-1-i].Replace("전투 시작 · 망자","전투 시작 · 일반 좀비").Replace("균열 진행도","군단 봉쇄 돌파");
            }
        }
        void ConstellationArt(Transform parent,string id,float height,float blankHeight)
        {
            var texture = constellationArt.Use(id);
            constellationArtUsed=texture!=null;
            if (texture == null) { BlankConstellationArt(parent,blankHeight); return; }
            Art(parent,texture,height,true);
        }
        void BlankConstellationArt(Transform parent,float height)
        {
            var blank = Box(parent,"Constellation art blank",Vector2.zero,Vector2.one,new Color(.055f,.070f,.083f));
            blank.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }
        RectTransform PrepCard(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var frame=Box(parent,name,min,max,new Color(.035f,.045f,.055f));
            Plate(frame,new Color(.065f,.078f,.070f));
            var card=Stack(frame,12);
            card.GetComponent<VerticalLayoutGroup>().spacing=6;return card;
        }
        void FixedArt(Transform parent,Texture2D texture,Vector2 min,Vector2 max)
        {
            var frame=Box(parent,"Scene illustration",min,max,Color.clear);frame.gameObject.AddComponent<RectMask2D>();
            var picture=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));picture.transform.SetParent(frame,false);
            var image=picture.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            var aspect=picture.GetComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio=texture==null?16f/9:texture.width/(float)texture.height;
        }
        void RenderPreparation(Transform body)
        {
            if(choosingExploration&&Game.S.phase==Phase.Morning){RenderExplorationPicker(body);return;}
            var s=Game.S;var stage=Box(body,"Preparation",new Vector2(.012f,.018f),new Vector2(.988f,.982f),Color.clear);
            var current=s.activeRift>=0?s.demons[s.activeRift]:null;int progress=current==null?0:current.progress;
            Positioned(stage,"전투준비",new Vector2(0,.93f),new Vector2(.36f,1),32,gold);
            Positioned(stage,current==null?"활성 균열 없음":Rules.Sins[s.activeRift]+"의 군단",new Vector2(.36f,.93f),new Vector2(.685f,1),25,muted);
            DrawAreaArt(stage,goal==Goal.Rift?Area.Rift:s.exploration.selected,new Vector2(0,.42f),new Vector2(.685f,.915f));
            var route=PrepCard(stage,"Rift progress",new Vector2(0,.17f),new Vector2(.685f,.32f));
            Label(route,"군단 봉쇄 돌파  "+progress+" / "+Rules.RiftProgressRequired,27,38,gold);
            var track=Box(route,"Progress track",Vector2.zero,Vector2.one,new Color(.10f,.12f,.14f));
            var trackLayout=track.gameObject.AddComponent<LayoutElement>();trackLayout.preferredHeight=16;trackLayout.flexibleHeight=0;
            for(int segment=0;segment<Rules.RiftProgressRequired;segment++)
            {
                var mark=Box(track,"Progress segment",new Vector2(segment/(float)Rules.RiftProgressRequired,0),new Vector2((segment+1)/(float)Rules.RiftProgressRequired,1),segment<progress?gold:new Color(.16f,.18f,.20f));
                mark.offsetMin=new Vector2(1,2);mark.offsetMax=new Vector2(-1,-2);
            }
            var controls=Box(stage,"Departure controls",Vector2.zero,new Vector2(.685f,.15f),panel);Plate(controls,panel);
            Positioned(controls,"오늘의 활동: "+(goal==Goal.Rift?"공략":"탐사"),new Vector2(.025f,.25f),new Vector2(.30f,.75f),25,gold);
            int optionIndex=0;
            foreach(Goal option in new[]{Goal.Explore,Goal.Rift})
            {
                Goal selected=option;float x=.32f+optionIndex++*.20f;
                var button=PlacedButton(controls,option==Goal.Explore?"탐사":"공략",new Vector2(x,.24f),new Vector2(x+.185f,.76f),()=>{goal=selected;choosingExploration=selected==Goal.Explore;Render();},s.phase==Phase.Morning&&(option==Goal.Explore||s.activeRift>=0&&s.day>s.avatar.selectedDay),goal==option);
                bool chosen=goal==option;
                DistinctAction(button,chosen?new Color(.45f,.75f,.82f):new Color(.065f,.10f,.14f),chosen?new Color(.02f,.055f,.07f):new Color(.92f,.95f,.98f),chosen?new Color(.75f,.94f,1):new Color(.33f,.47f,.58f),tick:chosen);
            }
            var proceed=PlacedButton(controls,"진행",new Vector2(.76f,.24f),new Vector2(.975f,.76f),()=>{if(goal==Goal.Explore){choosingExploration=true;Render();}else Depart();},s.phase==Phase.Morning,true);
            DistinctAction(proceed,new Color(.96f,.75f,.32f),new Color(.07f,.045f,.01f),new Color(1,.89f,.58f),forward:true);
            var seals=PrepCard(stage,"Current seals",new Vector2(.705f,.65f),Vector2.one);
            Label(seals,"악마 봉인  "+Game.ActiveSealCount+" / 7",29,48,gold);
            var marks=Row(seals,44);
            foreach(var demon in s.demons)
            {
                var mark=new GameObject("Seal "+demon.sin,typeof(RectTransform),typeof(CanvasRenderer),typeof(SealGlyph));mark.transform.SetParent(marks,false);
                mark.GetComponent<SealGlyph>().raycastTarget=false;
                mark.GetComponent<SealGlyph>().color=demon.remaining>0?gold:new Color(.25f,.28f,.30f);
            }
            string active=string.Join("  ·  ",s.demons.Where(d=>d.remaining>0).Select(d=>Rules.Sins[(int)d.sin]+" "+d.remaining+"일"));
            Label(seals,active.Length==0?"현재 봉인된 악마가 없습니다.":active,21,120,muted);
            var growth=PrepCard(stage,"Enemy scaling",new Vector2(.705f,.395f),new Vector2(1,.63f));
            Label(growth,"현재 적 강화",27,32,gold);
            Label(growth,"체력  "+Rules.Round(Game.EnemyHealthMultiplier*100)+"%",29,42,new Color(.83f,.36f,.32f));
            Label(growth,"공격력  "+Rules.Round(Game.EnemyAttackMultiplier*100)+"%",29,42,new Color(.83f,.36f,.32f));
            Label(growth,"현재 봉인 "+Game.ActiveSealCount+"명 기준",19,24,muted);
            var recent=PrepCard(stage,"Recent records",new Vector2(.705f,0),new Vector2(1,.375f));
            Label(recent,"최근 기록",27,40,gold);
            var records=s.logs.Where(l=>l.Contains("전투 시작")||l.Contains("봉인 성공")).Reverse().Take(4).Reverse().ToList();
            if(records.Count==0)Label(recent,"전투 기록이 여기에 표시됩니다.",21,80,muted);
            else foreach(string line in records)Label(recent,line,21,52,ink);
        }
        void RenderRecords(Transform body)
        {
            var p = Game.S.permanent; Label(body,"별이 기억하는 이름",36,65,gold);
            Label(body,"승리 " + p.victories + "회  /  지난 화신체 " + p.deaths.Count + "명",25,60);
            foreach (var death in p.deaths) Label(body,death.name + " · " + death.day + "일 · " + (death.combat ? "전투" : "비전투") + " · " + death.cause,23,65);
            Label(body,"네임드 좀비 기록",28,55,gold);
            foreach (var n in p.named) Label(body,n.name + " · " + Rules.Sins[(int)n.legion] + " 군단 · 체력 " + n.health + " · 공격 " + n.attack + (n.defeated ? " · 안식" : " · 재등장 가능"),23,65);
        }
        void RenderVictory(Transform body)
        {
            Art(body,gate,300); Label(body,"일곱 봉인, 하나의 새벽",46,100,gold);
            Label(body,"지상의 마지막 균열이 닫혔습니다.\n" + Game.S.avatar.name + "과 당신의 계약이 종말을 멈췄습니다.\n" + Game.S.day + "일 · 누적 봉인 " + Game.S.totalSeals + "회 · 누적 신위 " + Game.S.totalEarned,28,200);
            MakeButton(body,"승리 기록을 저장하고 타이틀로",() => { Save(); title = true; Render(); },true,true);
        }
    }
}
