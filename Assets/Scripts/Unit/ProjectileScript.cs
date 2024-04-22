using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;
/*
 * ProjectileScript
 * 飞行物脚本
 * 用于操控飞行道具行为
 */

public class ProjectileScript : MonoBehaviour, IPoolObject
{
    [SerializeField] private int penetration;   //穿透几个对象
    [SerializeField] private float lifeDuration;    //多久后自动消失
    [SerializeField] private float freezeTime;  //失效后过多久停止运动
    [SerializeField] private float vanishTime;  //失效后过多久后消失
    public GameObject prefab { set; get; }
    private SpriteRenderer mSRenderer;
    private Rigidbody2D mRbody;
    private Collider2D[] mColliders = new Collider2D[] { };
    private TrailRenderer[] mTrails;
    private DamageScript mDscript;
    private bool isDestroyed;
    private Transform mTransform;
    private int peneCount;
    private float lifeTimer;
    private bool ignoreHit;
    void Awake()
    {
        mTransform = transform;
        mSRenderer = GetComponent<SpriteRenderer>();
        mRbody = GetComponent<Rigidbody2D>();
        mDscript = GetComponent<DamageScript>();
        mRbody.GetAttachedColliders(mColliders);
        mTrails = GetComponentsInChildren<TrailRenderer>();
        lifeTimer = lifeDuration;
        peneCount = penetration;
    }
    //订阅命中事件
    void OnEnable()
    {
        EventManager.instance.Subscribe<UnitHitEvent>(OnUnitHit, Global.P_E_Hit_ProjectileHit);

    }

    void OnDisable()
    {
        EventManager.instance.Unsubscribe<UnitHitEvent>(OnUnitHit, Global.P_E_Hit_ProjectileHit);
    }
    void Update()
    {
        if(Global.IsTriggered(ref lifeTimer) && !isDestroyed)
            StartCoroutine(DestroySelf(freezeTime, vanishTime));
    }

    //回到对象池时重置自身状态
    public void OnRelease()
    {
        StopAllCoroutines();
        isDestroyed = false;
        foreach (Collider2D collider in mColliders)
        {
            collider.enabled = true;
        }
        mSRenderer.enabled = true;
        mRbody.velocity = Vector2.zero;
        lifeTimer = lifeDuration;
        peneCount = penetration;
        mDscript.ResetState();
    }

    //设置下挂的DamageScript信息（一般由创建者调用）
    public void SetDScriptData(IAttackable owner, string key)
    {
        if (isDestroyed) return;
        mDscript.SetOwner(owner);
        mDscript.SetAction(key);
    }

    //设置Transform
    public void SetTransform(Transform parent)
    {
        //关闭所有特效
        foreach (TrailRenderer trail in mTrails)
        {
            trail.enabled = false;
            trail.emitting = false;
        }
        mTransform.position = parent.position;
        mTransform.rotation = parent.rotation;
        //开启所有特效
        foreach (TrailRenderer trail in mTrails)
        {
            trail.enabled = true;
            trail.emitting = true;
        }
    }

    #region [函数组]飞行物运动
    //向前发射
    public void LaunchForward(float force)
    {
        if (isDestroyed) return;
        mRbody.AddForce(mTransform.rotation * Vector3.right * force);
    }
    //反转方向
    public void ReverseDir()
    {
        if (isDestroyed) return;
        mRbody.velocity = -mRbody.velocity;
    }
    #endregion

    //忽略下一次命中
    public void IgnoreNextHit()
    {
        ignoreHit = true;
    }

    //命中敌人时触发
    private void OnUnitHit(UnitHitEvent hit)
    {
        if (ignoreHit) 
        {
            ignoreHit = false;
            return;
        }
        if (hit.script == mDscript)
        {
            if (isDestroyed) return;
            peneCount--;
            if (peneCount < 0)
            {
                StartCoroutine(DestroySelf(freezeTime, vanishTime));
            }
        }
    }

    //分阶段自毁
    private IEnumerator DestroySelf(float freezeDelay, float vanishDelay)
    {
        isDestroyed = true;
        foreach (Collider2D collider in mColliders)
        {
            collider.enabled = false;
        }
        mSRenderer.enabled = false;
        yield return new WaitForSeconds(freezeDelay);
        mRbody.velocity = Vector2.zero;
        foreach (TrailRenderer trail in mTrails)
        {
            trail.emitting = false;
        }
        yield return new WaitForSeconds(vanishDelay);
        ObjectPoolManager.instance.Release(this);
    }
}
