using System;
using System.Collections;
using System.Collections.Generic;
using lg;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace lt
{
    public class KnobFragment : Image
    {
        public float Angle = 60.0f;
        public int PointNum = 30;
        public int Mode = 0;
        public int LineWeight = 40;
        public float LineWeightAngle = 1.0f;
        public Vector2[] LastVertextTop, LastVertextBott,LastVertextMidt,LastVertextMidb;
        //private MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            GenerateSimpleSprite(vh,false);
        }
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera,
                out var local);
            if (LastVertextBott == null || LastVertextTop == null) return false;
            for(int i = 0;i < LastVertextTop.Length;i += 2)
            {
                if (Isinside(local, LastVertextTop[i], LastVertextBott[i], LastVertextTop[i + 1]))
                    return true;
                if (Isinside(local, LastVertextTop[i + 1], LastVertextBott[i], LastVertextBott[i + 1]))
                    return true;
                if (i + 2 < LastVertextTop.Length)
                {
                    if (Isinside(local, LastVertextTop[i + 1], LastVertextBott[i + 1], LastVertextTop[i + 2]))
                        return true;
                    if (Isinside(local, LastVertextTop[i + 2], LastVertextBott[i + 1], LastVertextBott[i + 2]))
                        return true;
                }
            }
            return false;
        }

        private void Update()
        {
             //SetVerticesDirty();
        }

        /// GetEdgePoints(out var lt,out var rt,out var lb,out var rb);
        /// vh.AddVert(lt, Color.red, new Vector2(0,0));
        /// vh.AddVert(rt, Color.cyan, new Vector2(1, 0));
        /// vh.AddVert(lb, Color.yellow, new Vector2(0, 1));
        /// vh.AddVert(rb, Color.clear, new Vector2(1, 1));
        /// vh.AddTriangle(0, 1, 2);
        /// vh.AddTriangle(2, 1, 3);
        void GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)
        {
            vh.Clear();
            GenerateArc(vh,0);
            return;
            var color32 = color;
            
            vh.AddVert(new Vector3(0,0), color32, new Vector2(0.5f,0.5f));
            vh.AddVert(new Vector3(50f,50f), color32, new Vector2(1, 1));
            vh.AddVert(new Vector3(-50f, 50f), color32, new Vector2(0, 1));
            vh.AddTriangle(0, 1, 2);
        }

        void GenerateArc(VertexHelper vh, float r)
        {
            var color32 = color;
            var o = new Vector2(0, GetOriginY());
            var far = Mode == 0 ? o * -10000.0f : new Vector2( 0,-10000.0f);
            var farr = Quaternion.Euler(0, 0, Angle * 0.5f) * far;
            var farl = new Vector2(-farr.x, farr.y);
            GetEdgePoints(out var lt,out var rt,out var lb,out var rb);
            
            var lt_inter = Inter(o, farl, lt, rt);
            var rt_inter = Inter(o, farr, lt, rt);

            var lb_inter = Inter(o, farl, lt, lb);
            var rb_inter = Inter(o, farr, rt, rb);
            
            // vh.AddVert(lt_inter, Color.red, new Vector2(0,0));
            // vh.AddVert(rt_inter, Color.cyan, new Vector2(1, 0));
            // vh.AddVert(lb_inter, Color.yellow, new Vector2(0, 1));
            // vh.AddTriangle(0, 1, 2);
            // vh.AddVert(rb_inter, Color.blue, new Vector2(1, 1));
            // vh.AddTriangle(2, 1, 3);

            var ldir = (lt_inter - o).normalized;
            var half = ((lb_inter - o).magnitude - (lt_inter - o).magnitude) / 2;

            LastVertextTop = GenerateArcPoint(o,lt_inter,rt_inter,PointNum);
            LastVertextBott = GenerateArcPoint(o,lb_inter,rb_inter,PointNum);
            var lmt = ldir * ((lt_inter - o).magnitude + (half - LineWeight));
            var lmb = ldir * ((lt_inter - o).magnitude + (half + LineWeight));
            LastVertextMidt = GenerateArcPointEx(o,lmt,LineWeightAngle,PointNum);
            LastVertextMidb = GenerateArcPointEx(o,lmb,LineWeightAngle,PointNum);

            //var uv1 = o;//GetUVFromLocalPos(o);
            //var normal = new Vector3(((lt_inter) - uv1).magnitude, ((lb_inter) - uv1).magnitude, Angle);
            
            //material.SetVector("_Origin", new Vector4(o.x,o.y,Angle,0));
            //material.SetVector("_Point",new Vector4(lt_inter.x,lt_inter.y,lb_inter.x,lb_inter.y));
#if WDBG
            //TmpLog.log($"KnobFragment uv1 = {uv1} normal = {normal}");
#endif
            //var center = o + (Vector2)(Quaternion.Euler(0, 0, Angle * 0.5f) * (lm - o));
            var idx = 0;
            var last = Vector3Int.zero;
            var w = 0;
            var one = Vector2.one;//+ new Vector2(0.2f,0.2f);
            var zero = Vector2.zero;
            //var mid = (LastVertextTop.Length / 2) % 2 == 0 ? (LastVertextTop.Length / 2) : (LastVertextTop.Length / 2) - 1;
            for(int i = 0;i < LastVertextTop.Length;i += 2)
            {
                AddVert(vh,LastVertextTop[i], color32,     one); // 0
                AddVert(vh,LastVertextBott[i], color32,    one); // 1
                AddVert(vh,LastVertextTop[i + 1], color32, one); // 2
                AddVert(vh,LastVertextBott[i + 1], color32,one);// 3
                AddVert(vh,LastVertextMidt[i], color32, i == 0 ? one : zero);// 4
                AddVert(vh,LastVertextMidt[i + 1], color32, i == LastVertextTop.Length - 2 ? one : zero);// 5
                AddVert(vh,LastVertextMidb[i], color32, i == 0 ? one : zero);// 6
                AddVert(vh,LastVertextMidb[i + 1], color32, i == LastVertextTop.Length - 2 ? one : zero);// 7
                var offset = 0;
                if (i + 2 <= LastVertextTop.Length)
                {
                    vh.AddTriangle(last.x, last.y, idx + 0);
                    vh.AddTriangle(idx + 0, last.y, idx + 4);
                    vh.AddTriangle(last.y, last.z, idx + 4);
                    vh.AddTriangle(idx + 4, last.z, idx+6);
                    vh.AddTriangle(last.z, w, idx + 6);
                    vh.AddTriangle(idx + 6, w, idx + 1);
                }

                if (i == 0)
                {
                    vh.AddTriangle(idx + 0 + offset, idx + 5 + offset, idx + 2 + offset);
                    vh.AddTriangle(idx + 0 + offset, idx + 4 + offset, idx + 5 + offset);
                }
                else
                {
                    vh.AddTriangle(idx + 0 + offset, idx + 4 + offset, idx + 2 + offset);
                    vh.AddTriangle(idx + 2 + offset, idx + 4 + offset, idx + 5 + offset);
                }
                vh.AddTriangle(idx + 4 + offset, idx + 6 + offset, idx + 5 + offset);
                vh.AddTriangle(idx + 5 + offset, idx + 6 + offset, idx + 7 + offset);
                if (i == 0)
                {
                    vh.AddTriangle(idx + 6 + offset, idx + 1 + offset, idx + 7 + offset);
                    vh.AddTriangle(idx + 7 + offset, idx + 1 + offset, idx + 3 + offset);
                }
                else
                {
                    vh.AddTriangle(idx + 6 + offset, idx + 3 + offset, idx + 7 + offset);
                    vh.AddTriangle(idx + 6 + offset, idx + 1 + offset, idx + 3 + offset);
                }
                last.x = idx + 2 + offset;
                last.y = idx + 5 + offset;
                last.z = idx + 7 + offset;
                w = idx + 3;
                idx += 8 + offset;
            }
        }

        private Vector2 CalcUV1(Vector2 p,Vector2 c,float half)
        {
            var v = p - c;
            return v.normalized * (v.magnitude / half);
        }

        private void AddVert(VertexHelper vh,Vector2 pos,Color32 c,Vector2 uv1)
        {
            vh.AddVert((Vector3)pos, c, GetUVFromLocalPos(pos),uv1,Vector3.back, new Vector4(1.0f, 0.0f, 0.0f, -1.0f));
        }
        private float GetOriginY()
        {
            return Mode == 0 ? Mathf.Abs(((RectTransform)transform).anchoredPosition.y) : 0;
        }

        private Vector2 GetUVFromLocalPos(Vector2 p)
        {
            var rect = ((RectTransform)transform).rect;
            return new Vector2(p.x / rect.size.x + 0.5f,(p.y + GetOffset()) / rect.size.y + 0.5f) ;
        }

        private Vector2[] GenerateArcPoint(Vector2 o, Vector2 b, Vector2 e,int num)
        {
            var res = new Vector2[num];
            var curr = b - o;
            var q = Quaternion.Euler(0, 0, Angle / (num - 1));
            res[0] = b;
            for (int i = 1; i < num; ++i)
            {
                res[i] = o + (curr = (Vector2)(q * curr));
            }
            return res;
        }
        private Vector2[] GenerateArcPointEx(Vector2 o, Vector2 b, float w,int num)
        {
            var res = new Vector2[num];
            var curr = b - o;
            var q = Quaternion.Euler(0, 0, (Angle - (w * 2.0f)) / (num - 3));
            var q2 = Quaternion.Euler(0, 0, w);
            res[0] = b;
            for (int i = 1; i < num; ++i)
            {
                var tq = i == 1 || i == num - 1 ? q2 : q;
                res[i] = o + (curr = (Vector2)(tq * curr));
            }
            return res;
        }

        private void GetEdgePoints(out Vector2 lt,out Vector2 rt,out Vector2 lb,out Vector2 rb)
        {
            var rect = ((RectTransform)transform).rect;
            var offset = new Vector2( 0,GetOffset());
            lt = new Vector2(-rect.size.x * 0.5f, rect.size.y * 0.5f) - offset;
            rt = new Vector2(rect.size.x * 0.5f, rect.size.y * 0.5f)  - offset;
            lb = new Vector2(-rect.size.x * 0.5f, -rect.size.y * 0.5f) - offset;
            rb = new Vector2(rect.size.x * 0.5f, -rect.size.y * 0.5f) - offset;
        }

        private float GetOffset()
        {
            return Mode == 0 ? 0 : ((RectTransform)transform).GetPolarPos().x;
        }

        public static float Cross(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            return (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
        }
        public static float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Cross(p1, p2, p1, p3);
        }
        public static float fArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Mathf.Abs(Area(p1, p2, p3));
        }
        public static Vector2 Inter(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float s1 = fArea(p1, p2, p3), s2 = fArea(p1, p2, p4);
            return new Vector2((p4.x * s1 + p3.x * s2) / (s1 + s2), (p4.y * s1 + p3.y * s2) / (s1 + s2));
        }

        public static bool IsIntersection(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var crossA = Mathf.Sign(Vector3.Cross(d - c, a - c).y);
            var crossB = Mathf.Sign(Vector3.Cross(d - c, b - c).y);

            if (Mathf.Approximately(crossA, crossB)) return false;

            var crossC = Mathf.Sign(Vector3.Cross(b - a, c - a).y);
            var crossD = Mathf.Sign(Vector3.Cross(b - a, d - a).y);

            if (Mathf.Approximately(crossC, crossD)) return false;

            return true;
        }

        public static bool Isinside(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 pa = a - point;
            Vector3 pb = b - point;
            Vector3 pc = c - point;
            Vector3 pab = Vector3.Cross(pa, pb);
            Vector3 pbc = Vector3.Cross(pb, pc);
            Vector3 pca = Vector3.Cross(pc, pa);

            float d1 = Vector3.Dot(pab, pbc);
            float d2 = Vector3.Dot(pab, pca);
            float d3 = Vector3.Dot(pbc, pca);

            if (d1 >= 0 && d2 >= 0 && d3 >= 0) return true;
            return false;
        }
    }
}