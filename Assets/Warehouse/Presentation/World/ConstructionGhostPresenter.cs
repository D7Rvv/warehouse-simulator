using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Warehouse.Domain;

namespace Warehouse.Presentation
{
    /// <summary>One reusable transparent proxy. This class has no purchase, graph or inventory writer.</summary>
    public sealed class ConstructionGhostPresenter : MonoBehaviour
    {
        private Transform proxy;
        private Transform direction;
        private Material validMaterial, invalidMaterial;
        private readonly List<Renderer> surfaces = new List<Renderer>();
        private TextMesh coordinateLabel;
        public bool IsVisible => proxy!=null && proxy.gameObject.activeSelf;
        public string LastReason { get; private set; }
        public Vector3 ArrowDirection => direction==null?Vector3.forward:direction.forward;
        public void Initialize(Transform parent)
        {
            if(proxy!=null) return;
            proxy=new GameObject("S1 preview only - no installed machine").transform;
            proxy.SetParent(parent,false);
            validMaterial=GhostMaterial("Valid preview",new Color(.12f,.90f,.50f,.44f));
            invalidMaterial=GhostMaterial("Blocked preview",new Color(.95f,.15f,.12f,.48f));
            Surface("Transparent belt",proxy,new Vector3(0,.80f,0),new Vector3(.72f,.14f,.96f));
            Surface("Preview side L",proxy,new Vector3(-.42f,.75f,0),new Vector3(.10f,.25f,.96f));
            Surface("Preview side R",proxy,new Vector3(.42f,.75f,0),new Vector3(.10f,.25f,.96f));
            foreach(var x in new[]{-.35f,.35f}) foreach(var z in new[]{-.35f,.35f}) Surface("Preview leg",proxy,new Vector3(x,.35f,z),new Vector3(.07f,.7f,.07f));
            direction=new GameObject("IN to OUT direction").transform; direction.SetParent(proxy,false);
            Surface("Direction shaft",direction,new Vector3(0,.92f,-.02f),new Vector3(.07f,.04f,.57f));
            var left=Surface("Arrow L",direction,new Vector3(-.08f,.92f,.21f),new Vector3(.06f,.04f,.24f)); left.localRotation=Quaternion.Euler(0,-42,0);
            var right=Surface("Arrow R",direction,new Vector3(.08f,.92f,.21f),new Vector3(.06f,.04f,.24f)); right.localRotation=Quaternion.Euler(0,42,0);
            var label=new GameObject("Preview coordinate and reason"); label.transform.SetParent(proxy,false); label.transform.localPosition=new Vector3(0,.08f,-.65f); label.transform.localRotation=Quaternion.Euler(65,0,0);
            coordinateLabel=label.AddComponent<TextMesh>(); coordinateLabel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); coordinateLabel.fontSize=48; coordinateLabel.characterSize=.08f; coordinateLabel.anchor=TextAnchor.MiddleCenter; coordinateLabel.alignment=TextAlignment.Center;
            if(coordinateLabel.font!=null) label.GetComponent<MeshRenderer>().sharedMaterial=coordinateLabel.font.material;
            proxy.gameObject.SetActive(false);
        }
        private static Material GhostMaterial(string name,Color color)
        {
            var template=Resources.Load<Material>("WarehouseMaterials/GhostRetention");
            if(template==null) throw new InvalidOperationException("Missing transparent URP ghost retention material.");
            var material=new Material(template) { name=name+" [S1 only]",color=color,renderQueue=(int)RenderQueue.Transparent,enableInstancing=true };
            if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Surface")) material.SetFloat("_Surface",1);
            if(material.HasProperty("_Blend")) material.SetFloat("_Blend",0);
            if(material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            if(material.HasProperty("_DstBlend")) material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            if(material.HasProperty("_ZWrite")) material.SetInt("_ZWrite",0);
            material.SetOverrideTag("RenderType","Transparent"); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }
        private Transform Surface(string name,Transform parent,Vector3 position,Vector3 size)
        {
            var surface=GameObject.CreatePrimitive(PrimitiveType.Cube); surface.name=name; surface.transform.SetParent(parent,false); surface.transform.localPosition=position; surface.transform.localScale=size;
            // Ghosts cannot receive selection raycasts or physically move any cargo.
            var collider=surface.GetComponent<Collider>(); collider.enabled=false;
            var renderer=surface.GetComponent<Renderer>(); renderer.sharedMaterial=validMaterial; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false; surfaces.Add(renderer);
            return surface.transform;
        }
        public void Refresh(PlacementPreview preview)
        {
            if(preview==null) { Cancel(); return; }
            Render(preview.X,preview.Z,preview.RotationQuarterTurns,preview.Flipped,preview.IsValid,preview.InvalidReason,preview.Active);
        }
        public void Render(int x,int z,int quarterTurns,bool flipped,bool valid,string reason,bool active=true)
        {
            if(proxy==null) Initialize(transform);
            proxy.gameObject.SetActive(active); LastReason=reason;
            proxy.position=WarehouseSceneBlockout.GridToWorld(x,z);
            proxy.rotation=Quaternion.Euler(0,((quarterTurns%4+4)%4)*90,0);
            direction.localRotation=Quaternion.Euler(0,flipped?180:0,0);
            var material=valid?validMaterial:invalidMaterial;
            foreach(var renderer in surfaces) renderer.sharedMaterial=material;
            coordinateLabel.text="["+x+", "+z+"]  "+(valid?"PREVIEW / IN > OUT":"BLOCKED / SEE ACTION PANEL");
            coordinateLabel.color=valid?new Color(.12f,.95f,.5f):new Color(1f,.2f,.15f);
        }
        public void Cancel() { if(proxy!=null) proxy.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            if(validMaterial!=null) Destroy(validMaterial); if(invalidMaterial!=null) Destroy(invalidMaterial);
            if(proxy!=null) Destroy(proxy.gameObject);
        }
    }
}
