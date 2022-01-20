using DynamicLoopScroll;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ItemUIFinder<D>
{
    D find(Transform root);
}

public interface ItemRender<U,D>
{
    void render(U u, D d);
    void init(U u);
    void EvtListener(U u,bool isAdd);
}

public class CommItem<U, D, F, R> : LoopScrollRectItem
    where F : ItemUIFinder<U>, new()
    where R : ItemRender<U, D>, new()
{
    protected F f;
    protected R r;
    protected U ui;

    public U UI { get => ui; }
    public R Render { get => r; }
    public F Finder { get => f; }

    public CommItem()
    {
        this.f = new F();
        this.r = new R();
    }

    public override void Init()
    {
        ui = f.find(transform);
        if (ui != null)
        {
            r.init(ui);
            r.EvtListener(ui,true);
        }
    }

    protected override void OnRender()
    {
        if (Data != null && Data is D d)
        {
            r.render(ui, d);
        }
    }

    public override void Dispose() {
        r.EvtListener(ui,false);
    }

    

}

public abstract class ItemData<CFG, ID, Ext>
        where ID : struct
        where CFG : ITableBase
        where Ext : class
{
    public CFG cfg { get; private set; }
    public Ext ext { get; private set; }
    public ID id { get => getID(); }

    public abstract ID getID();

    public string path
    {
        get
        {
            return getPicPath();
        }
    }

    public virtual string getPicPath()
    {
        var c = libcore.PicturePath_PP_Frames.GetLine((int)((object)id));
        return c?.HeroSPath;
    }
    public ItemData(CFG cfg)
    {
        this.cfg = cfg;
        ext = null;
    }
    public ItemData(CFG cfg, Ext ext)
    {
        this.cfg = cfg;
        this.ext = ext;
    }

}

public abstract class ItemDataNoTable< ID, Ext>
        where ID : struct
        where Ext : class
{
    public Ext ext { get; private set; }
    public ID id { get => getID(); }

    public abstract ID getID();

    public string path
    {
        get
        {
            return getPicPath();
        }
    }

    public virtual string getPicPath()
    {
        var c = libcore.PicturePath_PP_Frames.GetLine((int)((object)id));
        return c?.HeroSPath;
    }
    public ItemDataNoTable()
    {
        ext = null;
    }
    public ItemDataNoTable( Ext ext)
    {
        this.ext = ext;
    }

}