
using System;
using System.Collections.Generic;
using DG.Tweening;
using lt;
using MT;
using UnityEngine;

namespace lg //little Game
{
     public struct KnobTranslateTag<I> : TickTag where I : TickTag{}
     public struct KnobRotateTag<I> : TickTag where I : TickTag{}
     public struct StateTag<I> : TickTag where I : TickTag{}
     public class KnobTranslateTick : KnobTick
     {
          protected List<RectTransform> Transforms;
          protected List<MTween> Tweens = new List<MTween>();
          public float Duration = 1.0f;
          public Ease Ease = Ease.Linear;
          public Action<float,float,float,bool,List<RectTransform>> onTouchBorder;
          protected int Direction = 0;
          public Action OnAddFunc,OnRmFunc;
          public KnobTranslateTick(List<RectTransform> trans)
          {
               Transforms = trans;
          }
          public KnobTranslateTick(List<RectTransform> trans,bool manual, bool hasAdsorption, float velocity) 
               : base(manual, hasAdsorption, velocity)
          {
               Transforms = trans;
          }
          /// <summary>
          /// 
          /// </summary>
          /// <param name="direction">
          /// dir = 1  0<-1<-2<-3
          /// dir = -1  0->1->2->3
          /// </param>
          public virtual void InitTweens(int direction = 1)
          {
               Direction = direction;
               Tweens.Clear();
               if (direction > 0)
               {
                    for (int i = Transforms.Count - 1; i > 0; --i)
                    {
                         Tweens.Add(Transforms[i].MDoSize(Transforms[i - 1].rect.size, Duration,Ease));
                         Tweens.Add(Transforms[i].MDoPolarPosFromPos(Transforms[i - 1].GetPosFromPolarPos(), Duration,Ease));
                    }
               }
               else if(direction < 0)
               {
                    for (int i = 0; i < Transforms.Count - 1; ++i)
                    {
                         Tweens.Add(Transforms[i].MDoSize(Transforms[i + 1].rect.size, Duration,Ease));
                         Tweens.Add(Transforms[i].MDoPolarPosFromPos(Transforms[i + 1].GetPosFromPolarPos(), Duration,Ease));
                    }
               }
          }
          
          public void ReInitTweens(List<RectTransform> ts,int direction = 1,float p = 0.0f)
          {
               Transforms = ts;
               InitTweens(direction);
               _Position = p;
               _Velocity = 0.0f;
          }

          protected override void OnChangePos(float position, bool manual)
          {
               base.OnChangePos(position, manual);
               foreach (var t in Tweens)
               {
                    t.position = position;
               }
          }

          protected override void OnTouchBorder(float velocity, float position, float over, bool manual)
          {
               base.OnTouchBorder(velocity, position, over,manual);
               onTouchBorder?.Invoke(velocity,position,over,manual,Transforms);
          }

          public override void OnAdd(TickGroup tg)
          {
               OnAddFunc?.Invoke();
          }

          public override void OnRemove()
          {
               OnRmFunc?.Invoke();
          }
     }

     public class KnobRotateTick : KnobTranslateTick
     {
          protected float Angle = 0;
          public KnobRotateTick(float angle,List<RectTransform> trans ) : base(trans)
          {
               Angle = angle;
          }
          public KnobRotateTick(float angle,List<RectTransform> trans, bool manual, bool hasAdsorption, float velocity) : base(trans, manual, hasAdsorption, velocity)
          {
               Angle = angle;
          }
          public override void InitTweens(int direction = 1)
          {
               Direction = direction;
               Tweens.Clear();
               for (int i = 0; i < Transforms.Count; ++i)
               {
                    Tweens.Add(Transforms[i].MDoRotateZ(Transforms[i].rotation.eulerAngles.z + (Angle * Direction), Duration,Ease));
               }
          }
     }
     
     public static class FragmentEx
     {
          public static void SetPolarPos(this RectTransform self, Vector2 pos)
          {
               self.pivot = new Vector2(0.5f,(pos.x + self.rect.height * 0.5f) / self.rect.height);
               self.anchoredPosition = new Vector2(0,0);
               self.localEulerAngles = new Vector3(0,0,pos.y);
          }
          public static void SetPolarPos(this RectTransform self, float y)
          {
               var angle = self.eulerAngles.z;
               self.pivot = new Vector2(0.5f,(y + self.rect.height * 0.5f) / self.rect.height);
               self.anchoredPosition = new Vector2(0,0);
               self.localEulerAngles = new Vector3(0,0,angle);
          }
          public static Vector2 GetPolarPos(this RectTransform self)
          {
               return new Vector2((self.pivot.y - 0.5f) * self.rect.height,self.eulerAngles.z);
          }
          public static Vector2 GetPosFromPolarPos(this RectTransform self)
          {
               var ppos = self.GetPolarPos();
               var res = Quaternion.Euler(0, 0,ppos.y) * new Vector3(0, 1,0);
               return -res * ppos.x;
          }

          public static void SetPolarPosFromPos(this RectTransform self,Vector2 pos)
          {
               var c = Vector2.Dot(new Vector2(0, -1),new Vector2(pos.x, pos.y).normalized);
               var t = pos.x < 0 ? Mathf.PI - Mathf.Acos(c) : Mathf.Acos(c);
               var cc =  (t / Mathf.Deg2Rad) + (pos.x < 0 ? 180.0f : 0.0f);
               self.SetPolarPos(new Vector2(pos.magnitude,cc));
          }

          public static ManualTween<RectTransform, Vector2> MDoPolarPosFromPos(this RectTransform self, Vector2 end, float dur,
               Ease ease = Ease.Linear)
          {
               var frag = self.GetComponent<KnobFragment>();
               return new ManualTween<RectTransform, Vector2>(
                    new Assignment<RectTransform, Vector2>(self,(rt,pos)=>
                    {
                         self.SetPolarPosFromPos(pos);
                         frag.SetVerticesDirty();
                    }),
                    self.GetPosFromPolarPos(),
                    end,
                    dur,
                    ease
               );
          }
          #region WDBG
          
          #endregion
          
     }
}