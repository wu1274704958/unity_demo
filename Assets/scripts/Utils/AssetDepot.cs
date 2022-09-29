using UnityEngine;

public static class AssetDepot
{
    public static Sprite LoadSprite(string getImagePath)
    {
        return Resources.Load<Sprite>(getImagePath);
    }
}