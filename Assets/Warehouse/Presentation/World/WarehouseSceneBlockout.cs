using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Warehouse.Presentation
{
    /// <summary>
    /// Original [P] S1 blockout, built once. Shared materials and cached section visibility;
    /// presentation never writes mock parcels, owners, equipment inventory or money.
    /// </summary>
    public sealed class WarehouseSceneBlockout : MonoBehaviour
    {
        public const float GridCellSize = 1f;
        public static readonly Vector3 GridOrigin = Vector3.zero;
        private readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
        private readonly Dictionary<string, SelectableEntityBinding> entities = new Dictionary<string, SelectableEntityBinding>();
        private readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Renderer[]> sectionRenderers = new Dictionary<string, Renderer[]>();
        private readonly Dictionary<string, Collider[]> sectionColliders = new Dictionary<string, Collider[]>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private readonly HashSet<Collider> decorativeColliders = new HashSet<Collider>();
        private bool initialized;
        private Font signFont;
        private TextMesh inspectionLabel;
        public Transform InspectionParcel { get; private set; }
        public int PlacedEquipmentCount { get; private set; }
        public int CargoVisualCount { get; private set; }
        public IReadOnlyList<Renderer> Renderers => renderers;
        public IEnumerable<SelectableEntityBinding> Entities => entities.Values;
        public int ActiveRendererCount { get { var count=0; foreach(var r in renderers) if(r!=null && r.enabled) count++; return count; } }
        public bool IsInitialized => initialized;

        public void Initialize(IEnumerable<string> truckCargoParcelIds)
        {
            if (initialized) return;
            if (truckCargoParcelIds == null) throw new ArgumentNullException(nameof(truckCargoParcelIds));
            initialized = true;
            signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            PrepareMaterials();
            foreach (var name in new[] { "Shell", "Inbound", "Inspection", "Export", "Conveyors", "Storage", "Yard", "Grid" })
                groups.Add(name, Child(name, transform));
            BuildShell(); BuildInbound(truckCargoParcelIds); BuildInspection(); BuildExport(); BuildConveyors(); BuildStorage(); BuildYard(); BuildGrid(); BuildAnchors();
            foreach (var pair in groups)
            {
                sectionRenderers.Add(pair.Key, pair.Value.GetComponentsInChildren<Renderer>(true));
                sectionColliders.Add(pair.Key, pair.Value.GetComponentsInChildren<Collider>(true));
                renderers.AddRange(sectionRenderers[pair.Key]);
                foreach(var collider in sectionColliders[pair.Key]) if(!collider.enabled) decorativeColliders.Add(collider);
            }
            ShowStation("LOC-WAREHOUSE-OVERVIEW");
        }
        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private void PrepareMaterials()
        {
            AddMaterial("Concrete", new Color(.38f,.42f,.43f), .05f, .25f, Grain(new Color(.4f,.43f,.44f), 311));
            AddMaterial("Wall", new Color(.70f,.73f,.72f), 0f, .2f);
            AddMaterial("Metal", new Color(.27f,.32f,.36f), .75f, .55f);
            AddMaterial("Blue", new Color(.08f,.28f,.43f), .55f, .45f);
            AddMaterial("Rubber", new Color(.055f,.07f,.075f), .05f, .13f);
            AddMaterial("Paper", new Color(.68f,.49f,.30f), 0f, .13f, Grain(new Color(.75f,.57f,.36f), 739));
            AddMaterial("Tape", new Color(.82f,.68f,.43f), 0f, .35f);
            AddMaterial("White", new Color(.92f,.92f,.86f), 0f, .25f);
            AddMaterial("Yellow", new Color(.92f,.66f,.1f), .3f, .35f);
            AddMaterial("Green", new Color(.19f,.50f,.34f), .3f, .35f);
            AddMaterial("Orange", new Color(.85f,.32f,.08f), .3f, .35f);
        }
        private Texture2D Grain(Color color, int seed)
        {
            var texture = new Texture2D(64,64,TextureFormat.RGB24,false) { name="Original procedural grain [P]", wrapMode=TextureWrapMode.Repeat };
            var random = new System.Random(seed); var pixels = new Color[64*64];
            for (var i=0;i<pixels.Length;i++) pixels[i]=color * (0.94f + (float)random.NextDouble()*.12f);
            texture.SetPixels(pixels); texture.Apply(false,true); textures.Add(texture); return texture;
        }
        private void AddMaterial(string key, Color color, float metallic, float smoothness, Texture2D texture=null)
        {
            var template=Resources.Load<Material>("WarehouseMaterials/LitRetention");
            if (template == null) throw new InvalidOperationException("Missing URP Lit retention material; open the committed Unity mobile project.");
            var material = new Material(template) { name="S1 "+key+" [original blockout]", color=color, enableInstancing=true };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smoothness);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",texture);
                else material.mainTexture=texture;
            }
            materials.Add(key,material);
        }
        private GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collision=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position; obj.transform.localScale=size;
            var renderer=obj.GetComponent<Renderer>(); renderer.sharedMaterial=materials[material]; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            var collider=obj.GetComponent<Collider>(); collider.enabled=collision;
            return obj;
        }
        private void Sign(string text, Transform parent, Vector3 position, float size=.22f, Color? color=null)
        {
            var obj=new GameObject("Sign "+text); obj.transform.SetParent(parent,false); obj.transform.localPosition=position;
            var label=obj.AddComponent<TextMesh>(); label.text=text; label.font=signFont; label.fontSize=48; label.characterSize=size; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center; label.color=color ?? Color.white;
            if (signFont != null) obj.GetComponent<MeshRenderer>().sharedMaterial=signFont.material;
        }
        private void Bind(GameObject obj, string id, string station, string kind)
        {
            var binding=obj.AddComponent<SelectableEntityBinding>(); binding.Configure(id,station,kind); entities.Add(id,binding);
        }
        private void BuildShell()
        {
            var g=groups["Shell"];
            Box("Warehouse slab",g,new Vector3(0,-.15f,0),new Vector3(24,.3f,20),"Concrete");
            Box("Left wall",g,new Vector3(-12,2.5f,0),new Vector3(.2f,5,20),"Wall");
            Box("Right wall",g,new Vector3(12,2.5f,0),new Vector3(.2f,5,20),"Wall");
            Box("Dock wall left",g,new Vector3(-10.8f,2.5f,10),new Vector3(2.4f,5,.2f),"Wall");
            Box("Dock wall right",g,new Vector3(3.8f,2.5f,10),new Vector3(16.4f,5,.2f),"Wall");
            for (var x=-11;x<=11;x+=11)
            {
                Box("Structural column",g,new Vector3(x,2.6f,9.5f),new Vector3(.3f,5.2f,.3f),"Metal");
                Box("Open roof beam",g,new Vector3(x,5.1f,0),new Vector3(.2f,.3f,20),"Metal",false);
            }
            for (var z=-8;z<=8;z+=4) Box("Floor seam",g,new Vector3(0,.008f,z),new Vector3(23,.006f,.025f),"Metal",false);
            Sign("WAREHOUSE 01  /  S1 VISUAL PROTOTYPE",g,new Vector3(1,4.5f,9.8f),.19f,new Color(.12f,.2f,.25f));
        }
        private void BuildInbound(IEnumerable<string> ids)
        {
            var g=groups["Inbound"];
            // The opening remains unobstructed; two jambs and header replace a solid door.
            Box("Dock left jamb",g,new Vector3(-9.6f,2.4f,9.7f),new Vector3(.4f,4.8f,.5f),"Yellow");
            Box("Dock right jamb",g,new Vector3(-4.4f,2.4f,9.7f),new Vector3(.4f,4.8f,.5f),"Yellow");
            Box("Rolled shutter",g,new Vector3(-7,4.7f,9.7f),new Vector3(4.8f,.5f,.6f),"Metal");
            var dock=Box("Dock plate",g,new Vector3(-7,.55f,9.7f),new Vector3(4.5f,.18f,2f),"Metal"); Bind(dock,"DEV-DOCK-01","LOC-INBOUND-01","Dock");
            Box("Truck cargo floor",g,new Vector3(-7,1,14.3f),new Vector3(4.1f,.3f,8.5f),"Metal");
            Box("Truck cargo left wall",g,new Vector3(-9.1f,2.6f,14.3f),new Vector3(.18f,3.4f,8.5f),"White");
            Box("Truck cargo right wall",g,new Vector3(-4.9f,2.6f,14.3f),new Vector3(.18f,3.4f,8.5f),"White");
            Box("Truck cargo roof",g,new Vector3(-7,4.35f,14.3f),new Vector3(4.4f,.16f,8.5f),"White");
            Box("Truck front bulkhead",g,new Vector3(-7,2.6f,18.5f),new Vector3(4.4f,3.4f,.18f),"White");
            Box("Truck cab",g,new Vector3(-7,1.7f,20),new Vector3(4.2f,3,2.7f),"Blue");
            var index=0;
            foreach (var id in ids)
            {
                var position=new Vector3(-8.1f+(index%2)*2f,1.85f,11.7f+(index/2)*1.9f);
                var parcel=Parcel("Cargo "+id,g,position,new Vector3(1.35f,1.3f,1.4f));
                Bind(parcel,id,"LOC-INBOUND-01","Parcel"); Sign(id,parcel.transform,new Vector3(0,0,-.506f),.06f,new Color(.12f,.14f,.15f));
                index++;
            }
            CargoVisualCount=index;
            Sign("INBOUND 01  /  VISUAL SAMPLE",g,new Vector3(-7,4.0f,9.3f),.16f);
        }
        private GameObject Parcel(string name, Transform g, Vector3 position, Vector3 size)
        {
            var p=Box(name,g,position,size,"Paper");
            // Child decoration uses normalized local coordinates on the reused box mesh.
            Box("Seal tape",p.transform,new Vector3(0,.503f,0),new Vector3(.18f,.008f,1.005f),"Tape",false);
            Box("Original blank shipping label",p.transform,new Vector3(0,0,-.503f),new Vector3(.70f,.50f,.006f),"White",false);
            return p;
        }
        private void BuildInspection()
        {
            var g=groups["Inspection"];
            var table=Box("Inspection bench",g,new Vector3(-7,1.0f,-3),new Vector3(3.5f,.18f,2.2f),"Blue"); Bind(table,"DEV-INSPECTION-TABLE-01","LOC-INSPECTION-01","InspectionTable");
            foreach (var x in new[]{-8.4f,-5.6f}) foreach(var z in new[]{-3.8f,-2.2f}) Box("Bench leg",g,new Vector3(x,.5f,z),new Vector3(.12f,1,.12f),"Metal");
            var parcel=Parcel("Inspection display proxy - not a parcel transfer",g,new Vector3(-7,1.7f,-3),new Vector3(1.3f,1.2f,1.2f)); InspectionParcel=parcel.transform;
            var labelObject=new GameObject("Original inspection shipping label"); labelObject.transform.SetParent(parcel.transform,false); labelObject.transform.localPosition=new Vector3(0,0,-.512f);
            inspectionLabel=labelObject.AddComponent<TextMesh>(); inspectionLabel.font=signFont; inspectionLabel.fontSize=64; inspectionLabel.characterSize=.04f; inspectionLabel.anchor=TextAnchor.MiddleCenter; inspectionLabel.alignment=TextAlignment.Center; inspectionLabel.color=new Color(.12f,.14f,.15f);
            if(signFont!=null) labelObject.GetComponent<MeshRenderer>().sharedMaterial=signFont.material;
            SetInspectionLabel("NO SELECTION","-","-","-",0);
            Bind(parcel,"DEV-INSPECTION-DISPLAY","LOC-INSPECTION-01","InspectionDisplayOnly");
            Box("Scale tool",g,new Vector3(-8.1f,1.2f,-2.3f),new Vector3(.65f,.25f,.7f),"Metal");
            Box("Rule tablet",g,new Vector3(-5.6f,1.7f,-2.4f),new Vector3(.65f,.8f,.12f),"Rubber");
            Sign("INSPECTION  /  DISPLAY ONLY",g,new Vector3(-7,2.9f,-1.9f),.15f,new Color(.14f,.22f,.27f));
        }
        private void BuildExport()
        {
            var g=groups["Export"];
            var modes=new[]{"AIR","ROAD","RAIL","MARITIME"};
            for(var i=0;i<modes.Length;i++)
            {
                var x=1.2f+i*2.65f;
                var gateRoot=Child(modes[i]+" gate assembly",g); gateRoot.position=new Vector3(x,1.7f,8.7f);
                Box(modes[i]+" gate back",g,new Vector3(x,1.7f,8.7f),new Vector3(2.35f,3.4f,.22f),i==0?"Blue":"Metal");
                if(i==0) Bind(gateRoot.gameObject,"DEV-GATE-AIR-01","LOC-EXPORT-AIR-01","ExportGate");
                Box("Gate capacity monitor",g,new Vector3(x,2.7f,8.5f),new Vector3(2.0f,.7f,.08f),"Rubber",false);
                Sign(modes[i]+"  /  "+(i==0?"DEMO GATE":"NOT IMPLEMENTED"),g,new Vector3(x,2.8f,8.42f),.11f);
                for(var k=0;k<=7;k++) Box("Guard fence",g,new Vector3(x-1.15f+k*.33f,.65f,7.0f),new Vector3(.035f,1.3f,.035f),"Metal");
                Box("Guard top rail",g,new Vector3(x,1.3f,7),new Vector3(2.35f,.06f,.06f),"Metal");
                Box("Export approach",g,new Vector3(x,.012f,5.5f),new Vector3(2.2f,.015f,.08f),"Yellow",false);
                var gateParts = new List<Transform>();
                foreach (Transform child in g) if(child!=gateRoot && child.name!="AIR gate assembly" && child.name!="ROAD gate assembly" && child.name!="RAIL gate assembly" && child.name!="MARITIME gate assembly") gateParts.Add(child);
                foreach(var part in gateParts) part.SetParent(gateRoot,true);
            }
            Parcel("Gate buffered parcel visual - separate from truck",g,new Vector3(1.2f,.75f,7.9f),new Vector3(1,.9f,1));
        }
        private void BuildConveyors()
        {
            var g=groups["Conveyors"];
            for(var i=0;i<1;i++)
            {
                var root=Child("Existing belt "+i,g); root.position=GridToWorld(i,0); root.localScale=new Vector3(.65f,1,.65f);
                BuildBelt(root);
                var actual=root.gameObject;
                Bind(actual,"DEV-BELT-"+i,"LOC-SORTING-01","Conveyor");
                PlacedEquipmentCount++;
            }
        }
        private void BuildBelt(Transform root)
        {
            Box("Belt surface",root,new Vector3(0,.80f,0),new Vector3(1.08f,.12f,1.45f),"Rubber");
            Box("Blue side frame L",root,new Vector3(-.61f,.75f,0),new Vector3(.16f,.26f,1.5f),"Blue");
            Box("Blue side frame R",root,new Vector3(.61f,.75f,0),new Vector3(.16f,.26f,1.5f),"Blue");
            foreach(var x in new[]{-.5f,.5f}) foreach(var z in new[]{-.5f,.5f}) Box("Support foot",root,new Vector3(x,.35f,z),new Vector3(.09f,.7f,.09f),"Metal");
            Box("IN to OUT shaft",root,new Vector3(0,.87f,0),new Vector3(.07f,.02f,.55f),"White",false);
            var head=Box("Direction arrow L",root,new Vector3(-.09f,.87f,.23f),new Vector3(.05f,.02f,.27f),"White",false); head.transform.localRotation=Quaternion.Euler(0,-42,0);
            head=Box("Direction arrow R",root,new Vector3(.09f,.87f,.23f),new Vector3(.05f,.02f,.27f),"White",false); head.transform.localRotation=Quaternion.Euler(0,42,0);
        }
        private void BuildStorage()
        {
            var g=groups["Storage"];
            var rack=Child("Rack 01",g); rack.position=new Vector3(8,0,0);
            var target=Box("Rack shelf",rack,new Vector3(0,1.3f,0),new Vector3(3,.15f,2),"Blue"); Bind(target,"DEV-RACK-01","LOC-STORAGE-01","Rack");
            foreach(var x in new[]{-1.4f,1.4f}) foreach(var z in new[]{-.9f,.9f}) Box("Rack upright",rack,new Vector3(x,1.5f,z),new Vector3(.12f,3,.12f),"Orange");
            Box("Upper shelf",rack,new Vector3(0,2.7f,0),new Vector3(3,.12f,2),"Blue");
            Sign("STORAGE  /  EMPTY SAMPLE",g,new Vector3(8,3.2f,-1.1f),.13f,new Color(.12f,.2f,.23f));
        }
        private void BuildYard()
        {
            var g=groups["Yard"];
            Box("Yard apron",g,new Vector3(0,-.2f,17),new Vector3(35,.3f,14),"Concrete");
            Box("Road",g,new Vector3(0,-.15f,27),new Vector3(60,.2f,8),"Rubber");
            for(var x=-25;x<=25;x+=5) Box("Road marking",g,new Vector3(x,-.04f,27),new Vector3(2.5f,.02f,.12f),"White",false);
            for(var x=-10;x<=-4;x+=3) Box("Bay guide",g,new Vector3(x,.01f,15),new Vector3(.08f,.02f,8),"Yellow",false);
            Sign("YARD  /  NO SCHEDULE IN S1",g,new Vector3(5,2.0f,15),.18f,new Color(.12f,.2f,.23f));
        }
        private void BuildGrid()
        {
            var g=groups["Grid"];
            for(var i=-6;i<=7;i++)
            {
                Box("Grid X",g,GridOrigin+new Vector3(i*GridCellSize-.5f,.025f,0),new Vector3(.018f,.012f,13),"White",false);
                Box("Grid Z",g,GridOrigin+new Vector3(0,.025f,i*GridCellSize-.5f),new Vector3(13,.012f,.018f),"White",false);
            }
        }
        private void BuildAnchors()
        {
            Anchor("CAM_OVERVIEW",new Vector3(14,17,-19),new Vector3(0,0,1),13,32,true,true);
            Anchor("CAM_INBOUND",new Vector3(-7,3.5f,5.7f),new Vector3(-7,2.2f,13),4,12,false,false);
            Anchor("CAM_INSPECTION",new Vector3(-7,3.0f,-7.2f),new Vector3(-7,1.6f,-3),2.5f,6,false,true);
            Anchor("CAM_EXPORT_AIR",new Vector3(5.1f,3.5f,1.1f),new Vector3(5.1f,1.6f,8),4,12,false,false);
            Anchor("CAM_SORTING",new Vector3(-4,4f,-5),new Vector3(0,0.8f,0),4,12,false,false);
            Anchor("CAM_STORAGE",new Vector3(4,3.5f,-5),new Vector3(8,1.5f,0),4,12,false,false);
            Anchor("CAM_YARD",new Vector3(15,12,32),new Vector3(-4,1.0f,17),8,25,false,false);
            Anchor("CAM_CONTRACT",new Vector3(16,9,23),new Vector3(1,1,16),8,25,false,false);
        }
        private void Anchor(string id,Vector3 position,Vector3 focus,float min,float max,bool pan,bool zoom)
        {
            var anchor=Child(id,transform); anchor.position=position;
            anchor.gameObject.AddComponent<StationCameraAnchor>().Configure(focus,min,max,pan,zoom); anchors.Add(id,anchor);
        }
        public Transform GetAnchor(string id) => id!=null && anchors.TryGetValue(id,out var anchor)?anchor:null;
        public bool TryGetEntity(string id,out SelectableEntityBinding binding)
        {
            binding=null; return id!=null && entities.TryGetValue(id,out binding) && binding!=null && binding.isActiveAndEnabled;
        }
        public void SetInspectionLabel(string id,string cargo,string country,string flag,int massGrams)
        {
            if(inspectionLabel!=null) inspectionLabel.text="S1 DISPLAY SAMPLE\n"+id+"\n"+cargo+" / "+country+"\n"+flag+" / "+massGrams+" g";
        }
        public void SetInspectionRotation(float degrees) { if(InspectionParcel!=null) InspectionParcel.localRotation=Quaternion.Euler(0,degrees,0); }
        public static Vector3 GridToWorld(int x,int z) => GridOrigin+new Vector3(x*GridCellSize,0,z*GridCellSize);
        public bool TryGetGridCell(Camera camera,Vector2 screen,out int x,out int z)
        {
            x=z=0; if(camera==null) return false;
            if(!new Plane(Vector3.up,Vector3.zero).Raycast(camera.ScreenPointToRay(screen),out var distance)) return false;
            var point=camera.ScreenPointToRay(screen).GetPoint(distance)-GridOrigin;
            x=Mathf.RoundToInt(point.x/GridCellSize); z=Mathf.RoundToInt(point.z/GridCellSize); return true;
        }
        public void ShowStation(string stationId)
        {
            var overview=stationId=="LOC-WAREHOUSE-OVERVIEW" || stationId=="LOC-BUILD-01" || stationId=="LOC-SHOP" || stationId=="LOC-ECONOMY";
            foreach(var pair in sectionRenderers)
            {
                var key=pair.Key;
                var visible=key=="Shell" || (key!="Yard" && key!="Grid" && overview) ||
                    (key=="Inbound" && (stationId=="LOC-INBOUND-01" || stationId=="LOC-YARD-01")) ||
                    (key=="Inspection" && stationId=="LOC-INSPECTION-01") ||
                    (key=="Export" && stationId=="LOC-EXPORT-AIR-01") ||
                    (key=="Conveyors" && (stationId=="LOC-SORTING-01" || stationId=="LOC-EXPORT-AIR-01")) ||
                    (key=="Storage" && stationId=="LOC-STORAGE-01") ||
                    (key=="Yard" && stationId=="LOC-YARD-01") || (key=="Grid" && stationId=="LOC-BUILD-01");
                foreach(var renderer in pair.Value) if(renderer!=null) renderer.enabled=visible;
                foreach(var collider in sectionColliders[key]) if(collider!=null) collider.enabled=visible && !decorativeColliders.Contains(collider);
            }
        }
        private void OnDestroy()
        {
            foreach(var material in materials.Values) if(material!=null) Destroy(material);
            foreach(var texture in textures) if(texture!=null) Destroy(texture);
        }
    }
}
