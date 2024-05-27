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

public class ProjectileScript : MonoBehaviour, IPoolObject, IAttackable
{
    [SerializeField] private int penetration;   //穿透几个对象
    [SerializeField] private float lifeDuration;    //多久后自动消失
    [SerializeField] private float freezeTime;  //失效后过多久停止运动
    [SerializeField] private float vanishTime;  //失效后过多久后消失
    public GameObject prefab { set; get; }
    private Coroutine isHitFreezing;  //存放顿帧的协程
    private Vector2 freezedVelocity;
    private float freezedAngularVelocity;
    private bool isDestroyed;
    private int peneCount;  //能够穿刺的单位数量
    private float lifeTimer;    //最长存在时间
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;
    private Collider2D[] _colliders = new Collider2D[] { };
    private TrailRenderer[] _trailRenderers;
    private DamageScript _dScript;
    private Transform _transform;
    void Awake()
    {
        _transform = transform;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _dScript = GetComponent<DamageScript>();
        _rigidbody.GetAttachedColliders(_colliders);
        _trailRenderers = GetComponentsInChildren<TrailRenderer>();
        lifeTimer = lifeDuration;
        peneCount = penetration;
    }

    void Update()
    {
        if (isHitFreezing != null)
        {
            //冻结状态下不会进行物理判定，但是会积攒这期间受到的动量
            if (_rigidbody.velocity != Vector2.zero)
            {
                freezedVelocity += _rigidbody.velocity;
                _rigidbody.velocity = Vector3.zero;
            }
            if (_rigidbody.angularVelocity != 0)
            {
                freezedAngularVelocity += _rigidbody.angularVelocity;
                _rigidbody.angularVelocity = 0;
            }
        }
        if (Global.IsTriggered(ref lifeTimer) && !isDestroyed)
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
        _spriteRenderer.enabled = true;
        _rigidbody.velocity = Vector2.zero;
        lifeTimer = lifeDuration;
        peneCount = penetration;
        _dScript.ResetState();
    }

    //设置下挂的DamageScript信息（一般由创建者调用）
    //source: 攻击来源
    //data: 攻击数据
    public void SetDScriptData(IAttackable source, ActionData data)
    {
        if (isDestroyed) return;
        _dScript.SetHeadAgent(source);
        _dScript.SetAction(data);
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

    //获取朝向
    public Vector2 GetFaceDir()
    {
        return _rigidbody.velocity.normalized;
    }

    //命中敌人时触发
    public void LandHit(UnitHitEvent hit)
    {
        //被闪避时不执行
        if (isDestroyed || hit.hitData.damage < 0) return;
        peneCount--;
        //HitFreeze(hit.actionData.baseData.hitFreezeTime);
        if (peneCount < 0)
        {
            StartCoroutine(DestroySelf(freezeTime, vanishTime));
        }
    }    
    
    //命中顿帧
    //time: 顿帧时长
    public void HitFreeze(float time)
    {
        if (time > 0)
        {
            if (isHitFreezing != null)
            {
                StopCoroutine(isHitFreezing);
            }
            isHitFreezing = StartCoroutine(HitFreezeDisable(time));
        }
    }

    //命中顿帧结束
    private IEnumerator HitFreezeDisable(float time)
    {
        yield return new WaitForSeconds(time);
        isHitFreezing = null;
        //恢复动能
        _rigidbody.velocity = freezedVelocity;
        _rigidbody.angularVelocity = freezedAngularVelocity;
        freezedVelocity = Vector2.zero;
        freezedAngularVelocity = 0;
    }

    //分阶段自毁
    private IEnumerator DestroySelf(float freezeDelay, float vanishDelay)
    {
        isDestroyed = true;
        foreach (Collider2D collider in _colliders)
        {
            collider.enabled = false;
        }
        _spriteRenderer.enabled = false;
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

/*
* ProjectileBuilder
* 飞行物建造者
* 用于构建飞行道具
* 实现建造者模式（Builder Pattern）
*/
public class ProjectileBuilder
{
    GameObject prefab;
    IAttackable source;
    ActionData data;
    int penetration;
    float lifeDuration;
    float launchForce;
    public ProjectileBuilder WithPrefab(GameObject prefab)
    {
        this.prefab = prefab;
        return this;
    }

    public ProjectileBuilder WithSource(IAttackable source)
    {
        this.source = source;
        return this;
    }

    public ProjectileBuilder WithActionData(ActionData data)
    {
        this.data = data;
        return this;
    }

    public ProjectileBuilder WithPenetration(int penetration)
    {
        this.penetration = penetration;
        return this;
    }

    public ProjectileBuilder WithLifeDuration(float lifeDuration)
    {
        this.lifeDuration = lifeDuration;
        return this;
    }

    public ProjectileBuilder WithLaunchForce(float launchForce)
    {
        this.launchForce = launchForce;
        return this;
    }
    public GameObject Build(Transform parent)
    {

        GameObject projectile = ObjectPoolManager.instance.Get(prefab);
        ProjectileScript script = projectile.GetComponent<ProjectileScript>();
        script.SetTransform(parent);
        script.SetDScriptData(source, data);
        script.LaunchForward(launchForce);
        return projectile;
    }
}