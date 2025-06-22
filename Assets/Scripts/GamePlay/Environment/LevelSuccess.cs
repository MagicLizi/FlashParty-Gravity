using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LevelSuccess : MonoBehaviour
{
    public List<GameObject> List = new List<GameObject>();

    private void Start()
    {
        // transform.localScale = Vector3.zero;
        // GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);

        // transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1, 1, 1, 0);
        GetComponent<SpriteRenderer>().DOFade(1f, 0.5f).SetEase(Ease.OutQuad);

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        InputManager.Instance.Enable(false);
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            foreach (var item in List)
            {
                item.SetActive(false);
            }
        }
    }
}