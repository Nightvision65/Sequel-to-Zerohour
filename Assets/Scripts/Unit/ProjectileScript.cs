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
    private Rigidbody2D _rigidbody;
    private Collider2D[] _colliders = new Collider2D[] { };
    private TrailRenderer[] _trailRenderers;
    private DamageScript mDscript;
    private bool isDestroyed;
    private Transform _transform;
    private int peneCount;
    private float lifeTimer;
    private bool ignoreHit;
    void Awake()
    {
        _transform = transform;
        mSRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        mDscript = GetComponent<DamageScript>();
        _rigidbody.GetAttachedColliders(_colliders);
        _trailRenderers = GetComponentsInChildren<TrailRenderer>();
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
        foreach (Collider2D collider in _colliders)
        {
            collider.enabled = true;
        }
        mSRenderer.enabled = true;
        _rigidbody.velocity = Vector2.zero;
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
        foreach (TrailRenderer trail in _trailRenderers)
        {
            trail.enabled = false;
            trail.emitting = false;
        }
        _transform.position = parent.position;
        _transform.rotation = parent.rotation;
        //开启所有特效
        foreach (TrailRenderer trail in _trailRenderers)
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
        _rigidbody.AddForce(_transform.rotation * Vector3.right * force);
    }
    //反转方向
    public void ReverseDir()
    {
        if (isDestroyed) return;
        _rigidbody.velocity = -_rigidbody.velocity;
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
        foreach (Collider2D collider in _colliders)
        {
            collider.enabled = false;
        }
        mSRenderer.enabled = false;
        yield return new WaitForSeconds(freezeDelay);
        _rigidbody.velocity = Vector2.zero;
        foreach (TrailRenderer trail in _trailRenderers)
        {
            trail.emitting = false;
        }
        yield return new WaitForSeconds(vanishDelay);
        ObjectPoolManager.instance.Release(this);
    }
}
