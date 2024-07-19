//using Colossal.Mathematics;
//using MoveIt.Tool;
//using QCommonLib;
//using System;
//using System.Collections.Generic;
//using Unity.Mathematics;
//using UnityEngine;

//namespace MoveIt.Overlays
//{
//    internal abstract class Widget : IDisposable
//    {
//        protected static readonly MIT _Tool = MIT.m_Instance;
//        internal string m_Name;
//        protected List<Polygon> _Polygons;

//        internal bool Active
//        {
//            get => _Polygons[0].Active;
//            set => _Polygons.ForEach(p => p.Active = value);
//        }

//        internal Widget(string name)
//        {
//            _Polygons = new();
//            m_Name = name;
//        }

//        internal virtual void SetPosition(float3 position)
//        {
//            foreach (var p in _Polygons)
//            {
//                p.Update(position);
//            }
//        }

//        internal static bool TryUnset(string name)
//        {
//            if (!_Tool.m_RenderSystem.TryGet(name, out Widget widget)) return false;
//            if (_Tool.m_OverlaySystem.m_DebugFreeze) return true; // Pretend we deleted it
//            widget.Dispose();
//            _Tool.m_RenderSystem.Remove(widget);
//            return true;
//        }

//        protected Material GenerateMaterial(Color c)
//        {
//            Material m = new(Shader.Find("HDRP/Lit"))
//            {
//                color = c,
//            };

//            return m;
//        }

//        public void Dispose()
//        {
//            _Polygons.ForEach(p => UnityEngine.Object.Destroy(p.GO));
//        }

//        ~Widget()
//        {
//            Dispose();
//        }
//    }

//    internal class Cuboid : Widget
//    {
//        private Bounds3 m_Bounds;

//        internal Cuboid(string name, Bounds3 bounds) : base(name)
//        {
//            Material m = GenerateMaterial(new(0f, 0f, 0.7f, 0.5f));
//            m_Bounds = bounds;
//            float3 position = bounds.Center();
//            float3 mag = bounds.max - bounds.min;
//            float3 scale = mag;

//            _Polygons.Add(new ShapeCuboid(name + "0", position, scale, m));
//        }

//        internal static void Set(string name, Bounds3 bounds)
//        {
//            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

//            if (!_Tool.m_RenderSystem.Has(name))
//            {
//                _Tool.m_RenderSystem.Add(new Cuboid(name, bounds));
//                return;
//            }

//            if (_Tool.m_RenderSystem.Get<Widget>(name) is not Cuboid c)
//            {
//                MIT.Log.Error($"Updating {name} but it is not a Cuboid");
//                return;
//            }

//            if (bounds.Equals(c.m_Bounds)) return;

//            c.SetPosition(bounds.Center());
//            c.m_Bounds = bounds;
//        }
//    }

//    internal class Cylinder : Widget
//    {
//        private Circle2 m_Circle;
//        private Bounds1 m_Bounds;

//        internal Cylinder(string name, Circle2 circle, Bounds1 bounds) : base(name)
//        {
//            Material m = GenerateMaterial(new(0f, 0f, 0.7f, 0.5f));
//            m_Circle = circle;
//            m_Bounds = bounds;
//            float3 scale = new(circle.radius * 2, (bounds.max - bounds.min) / 2, circle.radius * 2);

//            _Polygons.Add(new ShapeCylinder(name + "0", circle, bounds, scale, m));
//        }

//        internal static void Set(string name, Circle2 circle, Bounds1 bounds)
//        {
//            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

//            if (!_Tool.m_RenderSystem.Has(name))
//            {
//                _Tool.m_RenderSystem.Add(new Cylinder(name, circle, bounds));
//                return;
//            }

//            if (_Tool.m_RenderSystem.Get<Widget>(name) is not Cylinder c)
//            {
//                MIT.Log.Error($"Updating {name} but it is not a Cylinder");
//                return;
//            }

//            if (bounds.Equals(c.m_Bounds) && circle.Equals(c.m_Circle)) return;

//            c.SetPosition(new(circle.position.x, bounds.Center(), circle.position.y));
//            c.m_Bounds = bounds;
//            c.m_Circle = circle;
//        }
//    }

//    internal class Point : Widget
//    {
//        internal Point(string name, float3 position) : base(name)
//        {
//            Material m = GenerateMaterial(new(0.8f, 0f, 0f, 0.5f));

//            _Polygons.Add(new ShapeCuboid(name + "0", position, new(1.25f, 0.05f, 0.05f), m));
//            _Polygons.Add(new ShapeCuboid(name + "1", position, new(0.05f, 1.25f, 0.05f), m));
//            _Polygons.Add(new ShapeCuboid(name + "2", position, new(0.05f, 0.05f, 1.25f), m));
//        }

//        internal static void Set(string name, float3 position)
//        {
//            if (!_Tool.m_RenderSystem.Has(name))
//            {
//                _Tool.m_RenderSystem.Add(new Point(name, position));
//                return;
//            }

//            _Tool.m_RenderSystem.Update(name, position);
//        }
//    }


//    // Shapes

//    internal class Polygon : IDisposable
//    {
//        internal GameObject GO;

//        internal bool Active
//        {
//            get => GO.activeSelf;
//            set => GO.SetActive(value);
//        }

//        internal void Update(float3 position)
//        {
//            GO.transform.position = position;
//        }

//        public void Dispose() { }
//    }

//    internal class ShapeCuboid : Polygon
//    {
//        internal ShapeCuboid(string name, float3 position, float3 scale, Material m)
//        {
//            GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            GO.name = name;
//            GO.transform.position = position;
//            GO.transform.localScale = scale;
//            GO.transform.rotation = Quaternion.identity;

//            MeshRenderer meshRenderer = GO.GetComponent<MeshRenderer>();
//            meshRenderer.material = m;
//            meshRenderer.transform.localScale = scale;
//        }
//    }

//    internal class ShapeCylinder : Polygon
//    {
//        internal ShapeCylinder(string name, Circle2 circle, Bounds1 bounds, float3 scale, Material m)
//        {
//            Vector3 position = new(circle.position.x, bounds.Center(), circle.position.y);
//            GO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//            GO.name = name;
//            GO.transform.position = position;
//            GO.transform.localScale = scale;
//            GO.transform.rotation = Quaternion.identity;

//            MeshRenderer meshRenderer = GO.GetComponent<MeshRenderer>();
//            meshRenderer.material = m;
//            meshRenderer.transform.localScale = scale;
//            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
//        }
//    }
//}
