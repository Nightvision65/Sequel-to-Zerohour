using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * ColliderAnimator
 * 碰撞体动画器
 * 用于在AnimationClip中激活和关闭碰撞体
 * 确保开启至少一个物理帧，能够判定一次
 */
public class ColliderAnimator : MonoBehaviour
{
    public Collider2D[] colliders;
    private Dictionary<Collider2D, Coroutine> activeCoroutines = new Dictionary<Collider2D, Coroutine>();//跟踪每个Collider2D的活动协程


    //激活碰撞体（动画事件）
    public void ActivateCollider(string paras)
    {
        string[] para = paras.Split(';');
        ActivateCollider(int.Parse(para[0]), float.Parse(para[1]));
    }

    //激活碰撞体
    public void ActivateCollider(int index, float duration)
    {
        Collider2D collider = colliders[index];
        if (activeCoroutines.ContainsKey(collider))
        {
            StopCoroutine(activeCoroutines[collider]);
        }
        Coroutine newCoroutine = StartCoroutine(ActivateCoroutine(collider, duration));
        activeCoroutines[collider] = newCoroutine;
    }

    //碰撞体激活协程
    private IEnumerator ActivateCoroutine(Collider2D collider, float duration)
    {
        collider.enabled = true;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(duration);
        collider.enabled = false;
        activeCoroutines.Remove(collider);
    }
}
