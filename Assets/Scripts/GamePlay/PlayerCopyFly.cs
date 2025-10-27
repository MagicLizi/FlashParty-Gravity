using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerCopyFly : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Rect> TileRects = new List<Rect>();

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Clear()
    {
        TileRects.Clear();
    }

    public void CheckFly()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Rect playerAABB = getPlayerAABB(col);
        int dir = 1;
        Debug.Log("playerAABB: " + playerAABB);
        bool hit = false;
        for (int i = 0; i < TileRects.Count; i++)
        {
            Rect tileRect = TileRects[i];
            hit = RectsOverlap(playerAABB, tileRect);
            if (hit)
            {
                if (playerAABB.center.x < tileRect.center.x)
                {
                    dir = -1;
                }
                else
                {
                    dir = 1;
                }
                break;
            }
        }
        if (hit)
        {
            Debug.Log($"Player 被击飞, dir: {dir}");
            col.isTrigger = true;
            Player p = GetComponent<Player>();
            p.GetLaunched(new Vector2(dir * 30, 20), 1f);
            DOVirtual.DelayedCall(0.2f, () =>
            {
                col.isTrigger = false;
            });
        }
        else
        {
            Debug.Log("Player 未被击飞");
        }
    }

    Rect getPlayerAABB(BoxCollider2D col)
    {
        return new Rect(transform.position.x + col.offset.x, transform.position.y + col.offset.y, col.size.x, col.size.y);
    }

    bool RectsOverlap(Rect a, Rect b)
    {
        return a.xMin <= b.xMax && a.xMax >= b.xMin && a.yMin <= b.yMax && a.yMax >= b.yMin;
    }
}
