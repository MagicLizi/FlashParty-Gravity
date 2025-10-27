using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SnapCloneColliderHelper : MonoBehaviour
{
	public bool hasBoxCollider2D;
	public bool originalIsTrigger;
	private float knockbackHorizontalForce = 20f;
	private float knockbackVerticalForce = 12f;

	private BoxCollider2D _box2D;

	private bool _hasCheck = false;

	void Awake()
	{
		_box2D = GetComponent<BoxCollider2D>();
		hasBoxCollider2D = _box2D != null;
		if (_box2D != null)
		{
			originalIsTrigger = _box2D.isTrigger;
			_box2D.enabled = true;
			_box2D.isTrigger = true;
		}
		TriggerPlayer();
	}

	public void TriggerPlayer()
	{
		if (_hasCheck)
		{
			return;
		}
		if (_box2D == null)
		{
			Debug.Log("[SnapCloneColliderHelper] 无 BoxCollider2D，无法检测与 Player 的碰撞。");
			Destroy(this);
			return;
		}

		// 仅与场景上的 Player 对象的 BoxCollider2D 精确检测（考虑位置/旋转/缩放）
		GameObject playerGo = GameObject.Find("Player");
		bool hit = false;
		if (playerGo != null)
		{
			BoxCollider2D playerBox = playerGo.GetComponent<BoxCollider2D>();
			if (playerBox != null && playerBox.enabled)
			{
				Rect a = ComputeWorldAABB(_box2D);
				Rect b = ComputeWorldAABB(playerBox);
				hit = RectsOverlap(a, b);
				if (hit)
				{
					Debug.Log("[SnapCloneColliderHelper] 已与 Player 发生碰撞/交互。");
					_hasCheck = true;
					KnockbackPlayer(playerBox);
				}
				else
				{
					Debug.Log("[SnapCloneColliderHelper] 未检测到与 Player 的碰撞。");
				}
			}
			else
			{
				Debug.Log("[SnapCloneColliderHelper] Player 上未找到启用的 BoxCollider2D。");
			}
		}
		else
		{
			Debug.Log("[SnapCloneColliderHelper] 未找到 Player 对象。");
		}
		DOVirtual.DelayedCall(1f, () =>
		{
			_box2D.isTrigger = originalIsTrigger;
			Destroy(this);
		});
	}

	Rect ComputeWorldAABB(BoxCollider2D col)
	{
		Vector2 centerWorld = col.transform.TransformPoint(col.offset);
		Vector2 lossy = col.transform.lossyScale;
		lossy.x = Mathf.Abs(lossy.x);
		lossy.y = Mathf.Abs(lossy.y);
		Vector2 halfLocal = 0.5f * Vector2.Scale(col.size, lossy);
		float rad = col.transform.eulerAngles.z * Mathf.Deg2Rad;
		float c = Mathf.Cos(rad);
		float s = Mathf.Sin(rad);
		float ex = Mathf.Abs(c) * halfLocal.x + Mathf.Abs(s) * halfLocal.y;
		float ey = Mathf.Abs(s) * halfLocal.x + Mathf.Abs(c) * halfLocal.y;
		return Rect.MinMaxRect(centerWorld.x - ex, centerWorld.y - ey, centerWorld.x + ex, centerWorld.y + ey);
	}

	bool RectsOverlap(Rect a, Rect b)
	{
		return a.xMin <= b.xMax && a.xMax >= b.xMin && a.yMin <= b.yMax && a.yMax >= b.yMin;
	}

	void KnockbackPlayer(BoxCollider2D playerBox)
	{
		// 判断 player 和当前对象的相对位置，player 在左边为 -1，右边为 1
		int dir = 1;
		if (playerBox.transform.position.x < this._box2D.transform.position.x)
		{
			dir = -1;
		}
		else
		{
			dir = 1;
		}
		Vector2 launchForce = new Vector2(dir * 50, 20);
		float hitFlyDuration = 1.5f;
		playerBox.gameObject.GetComponent<Player>().GetLaunched(launchForce, hitFlyDuration);
	}

}


