using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LevelSuccess : MonoBehaviour
{
    public List<GameObject> List = new List<GameObject>();
    
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