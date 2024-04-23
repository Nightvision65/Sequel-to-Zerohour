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
    public float damage;
    [SerializeField] protected float speed;
    public Dictionary<string, ActionData> actionData;    //保存关于角色的动作数据(比如攻击动作值、削韧等)
    public Transform target;  //敌人目标
    public Vector2 faceDirection;    //朝向
    [SerializeField] protected float vanishTime;  //死后多久消失
    protected bool enableAI = true;
    protected Coroutine isHitFreezing;  //敌人正在顿帧（存放顿帧的协程）
    protected float savedAnimSpeed; //临时存放的动画速度，用来顿帧
    protected Vector2 knockDir;   //存放上次被击退时的方向
    protected Vector2 moveDirection;    //移动方向
    protected List<SpriteRenderer> mSprites;
    protected Transform mTransform;
    protected Rigidbody2D mRbody;
    protected Animator mAnim;
    protected TeamScript mTeam;
    protected FlashEffectScript mFlashEffect;
    protected StateMachineManager mSMM;
    private float vanishTimer;
    private Vector2 freezedVelocity;
    private float freezedAngularVelocity;
    private bool isDead = false;
    protected void Start()
    {
        mSprites = GetComponentsInChildren<SpriteRenderer>(true).ToList();
        mRbody = GetComponent<Rigidbody2D>();
        mAnim = GetComponent<Animator>();
        mTransform = transform;
        mTeam = GetComponent<TeamScript>();
        mFlashEffect = GetComponent<FlashEffectScript>();
        mSMM = GetComponent<StateMachineManager>();
        mSMM.StateTransition += OnAnimStateChange;
        mSMM.TagTransition += OnAnimTagChange;
        nowHealth = maxHealth;
        nowPoise = maxPoise;
    }
    protected void Update()
    {
        if (isHitFreezing != null)
        {
            //冻结状态下不会进行物理判定，但是会积攒这期间受到的动量
            if(mRbody.velocity != Vector2.zero)
            {
                freezedVelocity += mRbody.velocity;
                mRbody.velocity = Vector3.zero;
            }
            if (mRbody.angularVelocity != 0)
            {
                freezedAngularVelocity += mRbody.angularVelocity;
                mRbody.angularVelocity = 0;
            }
        }
        if (isDead)
        {
            AfterDeath();
        }
    }

    //写入伤害判定类型（传递给伤害脚本）
    public void AttackSet(string action)
    {
        actionData[action].damageScript.SetAction(action);
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
        moveDirection = (tar.position - mTransform.position).normalized;
        mRbody.AddForce(moveDirection * speed);
    }
    //AI：对峙（绕着单位旋转）
    //direction: 对峙时移动方向，zero往后，positive往左，negative往右
    protected void AI_Standoff(Transform tar, float speedModifier, Ternary direction)
    {
        Vector2 tdir = (tar.position - mTransform.position).normalized;
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
        mRbody.AddForce(moveDirection * speed * speedModifier);
    }


    //攻击行为
    //AI：攻击
    protected void AI_Attack()
    {
        //Debug.Log("Triggered");
        mAnim.SetTrigger("Attack");
    }


    //寻敌行为
    //AI：找到距离目标的距离
    //tar: 目标
    protected float AI_Get_Target_Distance(Transform tar)
    {
        return Vector2.Distance(tar.position, mTransform.position);
    }
    //AI：找到最近敌对单位
    //range: 最大距离
    protected Transform AI_Find_Target_Closest(float range)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(mTransform.position, range);
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;
        foreach (Collider2D collider in colliders)
        {
            if (collider.attachedRigidbody.tag == "BattleUnit")
            {
                TeamScript colliderTeam = collider.GetComponentInParent<TeamScript>();
                if (mTeam.IsEnemy(colliderTeam))
                {
                    float distance = Vector2.Distance(mTransform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestTarget = collider.transform;
                        closestDistance = distance;
                    }
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
        foreach (SpriteRenderer sprite in mSprites)
        {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);
        }
    }

    //敌人受击
    public void GetHit(HitData hit, IAttackable attacker = null)
    {
        Debug.Log(mTransform.name + ": 受到" + hit.damage + "伤害, " + hit.impact + "韧性伤害。");
        knockDir = Vector2.zero;
        nowHealth -= hit.damage;
        nowPoise -= hit.impact;
        Knockback(hit.knockback);
        mFlashEffect.SetFlash("enemyHit");
        //血量归零
        if (nowHealth <= 0)
        {
            mAnim.Play("Death", 0, 0f);
            enableAI = false;
            isDead = true;
            //消除单位的物理和逻辑碰撞判定
            List<Collider2D> colliders = new List<Collider2D>();
            mRbody.GetAttachedColliders(colliders);
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = false;
            }
            //mRbody.Sleep();
            vanishTimer = vanishTime;
            foreach (SpriteRenderer sprite in mSprites)
            {
                sprite.sortingOrder -= 3;
            }
            return;
        }
        //韧性归零
        if (nowPoise <= 0)
        {
            mAnim.Play("Stagger", 0, 0f);
            //enableAI = false;
            nowPoise = maxPoise;
        }
    }

    //敌人击退
    protected void Knockback(Vector2 knockback)
    {
        mRbody.AddForce(knockback);
        knockDir = knockback.normalized;
    }

    //命中顿帧
    //knockback: 击退向量（顿帧结束后再计算击退）
    //time: 顿帧时长
    public void HitFreeze(Vector2 knockback, float time)
    {
        if (isHitFreezing != null)
        {
            StopCoroutine(isHitFreezing);
        }
        else
        {
            mFlashEffect.SetFlashStatic("enemyHit");
            savedAnimSpeed = mAnim.speed;
            mAnim.speed = 0;
        }
        isHitFreezing = StartCoroutine(HitFreezeDisable(knockback, time));
    }

    //命中顿帧结束
    private IEnumerator HitFreezeDisable(Vector2 knockback, float time)
    {
        yield return new WaitForSeconds(time);
        mAnim.speed = savedAnimSpeed;
        isHitFreezing = null;
        mFlashEffect.SetFlash("enemyHit");
        //恢复动能
        mRbody.velocity = freezedVelocity;
        mRbody.angularVelocity = freezedAngularVelocity;
        freezedVelocity = Vector2.zero;
        freezedAngularVelocity = 0;
    }

}
