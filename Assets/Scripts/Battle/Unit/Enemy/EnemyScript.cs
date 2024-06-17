using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Processors;
using static UnityEngine.ParticleSystem;
/*
 * EnemyScript
 * 敌人脚本
 * 敌人的父类，负责所有敌人都能做的事情，以及提供一些接口
 * 设定敌人的基础AI动作，敌人AI的作用和角色一样是给动画器提供信号（除了移动部分）
 */

public class EnemyScript : SerializedMonoBehaviour, IHitable, IAttackable
{
    [SerializeField] protected float maxHealth; //生命值
    [SerializeField] protected float maxPoise;  //韧性值
    protected float nowHealth;
    protected float nowPoise;
    [SerializeField] protected float speed;
    public Dictionary<string, ActionData> actionData;    //保存关于角色的动作数据(比如攻击动作值、削韧等)
    public Dictionary<string, ShiftDataSO> shiftData;    //保存关于角色的动作数据(比如攻击动作值、削韧等)
    public List<DamageScript> dScripts; //保存直接相关的DamageScript
    public Transform target;  //敌人目标
    public Vector2 faceDirection;    //朝向
    [SerializeField] protected float vanishTime;  //死后多久消失
    protected bool enableAI = true;
    protected Coroutine isHitFreezing;  //敌人正在顿帧（存放顿帧的协程）
    protected float savedAnimSpeed; //临时存放的动画速度，用来顿帧
    protected Vector2 knockDir;   //存放上次被击退时的方向
    protected Vector2 moveDirection;    //移动方向
    protected List<SpriteRenderer> _sprites;
    protected Transform _transform;
    protected Rigidbody2D _rigidbody;
    protected Animator _animator;
    protected TeamScript _team;
    protected FlashEffectScript _flashEffect;
    protected StateMachineManager _stateMachine;
    private float vanishTimer;
    private Vector2 freezedVelocity;
    private float freezedAngularVelocity;
    private bool isDead = false;
    protected void Start()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>(true).ToList();
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _transform = transform;
        _team = GetComponent<TeamScript>();
        _flashEffect = GetComponent<FlashEffectScript>();
        _stateMachine = GetComponent<StateMachineManager>();
        _stateMachine.StateTransition += OnAnimStateChange;
        _stateMachine.TagTransition += OnAnimTagChange;
        nowHealth = maxHealth;
        nowPoise = maxPoise;
    }
    protected void Update()
    {
        if (isHitFreezing != null)
        {
            //冻结状态下不会进行物理判定，但是会积攒这期间受到的动量
            if(_rigidbody.velocity != Vector2.zero)
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
        if (isDead)
        {
            AfterDeath();
        }
    }

    //写入伤害判定类型（传递给伤害脚本）
    public void AttackSet(string action, int index)
    {
        dScripts[index].SetAction(actionData[action]);
    }
    #region [函数组]AI行为
    //移动行为
    //AI：巡逻
    protected void AI_Patrol()
    {
        
    }
    //AI：追逐
    protected void AI_Chase(Transform tar)
    {
        moveDirection = (tar.position - _transform.position).normalized;
        _rigidbody.AddForce(moveDirection * speed);
    }
    //AI：对峙（绕着单位旋转）
    //direction: 对峙时移动方向，zero往后，positive往左，negative往右
    protected void AI_Standoff(Transform tar, float speedModifier, Ternary direction)
    {
        Vector2 tdir = (tar.position - _transform.position).normalized;
        switch (direction)
        {
            case Ternary.zero:
                moveDirection = new Vector2(-tdir.x, -tdir.y);
                break;
            case Ternary.positive:
                moveDirection = new Vector2(-tdir.y, tdir.x);
                break;
            case Ternary.negative:
                moveDirection = new Vector2(tdir.y, -tdir.x);
                break;
        }
        _rigidbody.AddForce(moveDirection * speed * speedModifier);
    }


    //攻击行为
    //AI：攻击
    protected void AI_Attack()
    {
        //Debug.Log("Triggered");
        _animator.SetTrigger("Attack");
    }


    //寻敌行为
    //AI：找到距离目标的距离
    //tar: 目标
    protected float AI_Get_Target_Distance(Transform tar)
    {
        return Vector2.Distance(tar.position, _transform.position);
    }
    //AI：找到最近敌对单位
    //range: 最大距离
    protected Transform AI_Find_Target_Closest(float range)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_transform.position, range);
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;
        foreach (Collider2D collider in colliders)
        {
            TeamScript colliderTeam = collider.GetComponentInParent<TeamScript>();
            if (colliderTeam != null && _team.IsEnemy(colliderTeam))
            {
                float distance = Vector2.Distance(_transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestTarget = collider.transform;
                    closestDistance = distance;
                }
            }
        }
        return closestTarget;
    }
    #endregion

    #region [函数组]动画器事件
    protected virtual void OnAnimStateChange(int exitState, int enterState) { }
    protected virtual void OnAnimTagChange(int exitTag, int enterTag) { }
    #endregion


    //开启位移动作
    public Coroutine SetShift(string key)
    {
        ShiftDataSO data = shiftData[key];
        return StartCoroutine(Shift(data.force, data.duration, data.angle, data.followDir));
    }

    //位移协程
    public IEnumerator Shift(float force, float duration, float angle, bool followDir)
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * GetFaceDir();
        int time = Mathf.Max(1, Mathf.RoundToInt(duration / Time.fixedDeltaTime));
        while (time > 0)
        {
            time--;
            if (followDir)
            {
                dir = Quaternion.Euler(0, 0, angle) * GetFaceDir();
            }
            _rigidbody.AddForce(force * dir);
            yield return new WaitForFixedUpdate();
        }
    }

    //敌人死亡后处理
    public void AfterDeath()
    {
        vanishTimer -= Time.deltaTime;
        float alpha = Mathf.Lerp(0f, 2f, vanishTimer / vanishTime);
        if(alpha < 0.01f)
        {
            Destroy(gameObject);
            return;
        }
        foreach (SpriteRenderer sprite in _sprites)
        {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);
        }
    }

    //获取朝向
    public Vector2 GetFaceDir() => faceDirection.normalized;

    //敌人受击
    public void GetHit(ref UnitHitEvent hit)
    {
        Debug.Log(_transform.name + ": 受到" + hit.hitData.damage + "伤害, " + hit.hitData.impact + "韧性伤害。");
        knockDir = Vector2.zero;
        nowHealth -= hit.hitData.damage;
        nowPoise -= hit.hitData.impact;
        Knockback(hit.hitData.knockback);//命中敌人顿帧
        if (hit.actionData.baseData.hitFreezeTime > 0)
        {
            HitFreeze(hit.actionData.baseData.hitFreezeTime);
        }
        else
        {
            _flashEffect.SetFlash("enemyHit");
        }
        //血量归零
        if (nowHealth <= 0)
        {
            _animator.Play("Death", 0, 0f);
            enableAI = false;
            isDead = true;
            //消除单位的物理和逻辑碰撞判定
            List<Collider2D> colliders = new List<Collider2D>();
            _rigidbody.GetAttachedColliders(colliders);
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = false;
            }
            //_rigidbody.Sleep();
            vanishTimer = vanishTime;
            foreach (SpriteRenderer sprite in _sprites)
            {
                sprite.sortingOrder -= 3;
            }
            return;
        }
        //韧性归零
        if (nowPoise <= 0)
        {
            _animator.Play("Stagger", 0, 0f);
            //enableAI = false;
            nowPoise = maxPoise;
        }
    }
    public void LandHit(UnitHitEvent hit)
    {
    }


        //敌人击退
    protected void Knockback(Vector2 knockback)
    {
        _rigidbody.AddForce(knockback);
        knockDir = knockback.normalized;
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
            else
            {
                _flashEffect.SetFlashStatic("enemyHit");
                savedAnimSpeed = _animator.speed;
                _animator.speed = 0;
            }
            isHitFreezing = StartCoroutine(HitFreezeDisable(time));
        }
    }

    //命中顿帧结束
    private IEnumerator HitFreezeDisable(float time)
    {
        yield return new WaitForSeconds(time);
        _animator.speed = savedAnimSpeed;
        isHitFreezing = null;
        _flashEffect.SetFlash("enemyHit");
        //恢复动能
        _rigidbody.velocity = freezedVelocity;
        _rigidbody.angularVelocity = freezedAngularVelocity;
        freezedVelocity = Vector2.zero;
        freezedAngularVelocity = 0;
    }

}
