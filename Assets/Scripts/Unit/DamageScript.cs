using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
/*
 * DamageScript
 * 伤害脚本
 * 管理不会同时出现的攻击判定区域，所有单位通用
 * 负责将所有来源的ActionData转化为Hitdata
 */

public class DamageScript : SerializedMonoBehaviour
{
    [SerializeField] private TeamScript ownerTeam;    //伤害所有者队伍
    [SerializeField] private IAttackable owner;     //伤害所有者
    private List<Collider2D> damageColliders;   //伤害判定区域
    private Rigidbody2D damageRbody; //绑定的Rigidbody2D
    private List<TeamScript> hitUnits = new List<TeamScript>();   //已经被本次攻击命中过的单位
    private string damageKey;  //伤害来源动作
    private Vector2 knockDirection;  //击退方向
    private bool isActive = false;  //有判定区域被激活
    private bool isHitEffect = false;  //卡帧震屏等击中效果一次攻击只触发一次
    private UnitHitEvent hitEvent = new UnitHitEvent(); //用于广播击中事件


    private void Start()
    {
        damageRbody = GetComponent<Rigidbody2D>();
        damageColliders = GetComponentsInChildren<Collider2D>().ToList();
    }

    private void FixedUpdate()
    {
        //处理判定区情况
        bool allOff = true;
        foreach (Collider2D damageCollider in damageColliders)
        {
            if (damageCollider.enabled)
            {
                allOff = false;
                if(!isActive)   //判定区域被打开
                {
                    isActive = true;
                    ResetState();
                    break;
                }
            }
        }
        if (allOff && isActive) //判定区域被关闭
        {
            isActive = false;
        }
    }

    //重置脚本状态
    public void ResetState()
    {
        isHitEffect = false;
        hitUnits.Clear();
    }
    //设置脚本的所有者
    public void SetOwner(IAttackable owner)
    {
        this.owner = owner;
        ownerTeam = (owner as MonoBehaviour).GetComponent<TeamScript>();
    }

    //设置脚本目前正处于的动作key
    public void SetAction(string key)
    {
        damageKey = key;
    }

    //获取动作数据
    private ActionData GetOwnerActionData()
    {
        if (owner is CharacterScript)
            return (owner as CharacterScript).chActionData[damageKey];
        if (owner is EnemyScript)
            return (owner as EnemyScript).actionData[damageKey];
        return null;
    }

    //类似这种需要耦合的方法后面要全部改掉
    //获取单位朝向
    private Vector2 GetFaceDirection(MonoBehaviour unit)
    {
        if (unit is CharacterScript)
            return (unit as CharacterScript)._ball.faceDirection.normalized;
        if (unit is EnemyScript)
            return (unit as EnemyScript).faceDirection.normalized;
        return Vector2.zero;
    }

    //判断敌人在本次判定中是否已经被击中过
    private bool unitIsHit(TeamScript unit)
    {
        foreach (TeamScript hitUnit in hitUnits)
        {
            if (hitUnit == unit)
                return true;
        }
        return false;
    }

    //攻击目标单位
    public void HitTargetUnit(IHitable hitTarget)
    {
        Transform target = (hitTarget as MonoBehaviour)?.transform;
        TeamScript teamScript = target.GetComponentInParent<TeamScript>();
        if (!ownerTeam.IsSameTeam(teamScript))
        {
            teamScript.SetAggro(ownerTeam.team, true);
            ActionData damageData = GetOwnerActionData();
            switch (damageData.knocktype)
            {
                case KnockType.aim:
                    knockDirection = GetFaceDirection(owner as MonoBehaviour);
                    break;
                case KnockType.velocity:
                    knockDirection = damageRbody.velocity.normalized;
                    break;
                case KnockType.recoil:
                    knockDirection = -GetFaceDirection(hitTarget as MonoBehaviour);
                    break;
            }
            float damage = damageData.damage;
            #region [功能]处理额外动作附件
            float critchance = 0;
            foreach (ActionExtra actionExtra in damageData.extra.Values)
            {
                switch (actionExtra.type)
                {
                    case ActionExtraType.critical:
                        float extra;
                        if (actionExtra.TryGetValue("value", out extra))
                        {
                            critchance += extra;
                        }
                        break;
                }
            }
            bool critical = (critchance > Random.value);
            if (critical)
            {
                Debug.Log("暴击！");
                damage *= 2f;
            }
            #endregion
            //造成伤害（使用接口，无需转换类型)
            HitData hit = new HitData(damage, damageData.impact, damageData.knockback * knockDirection);
            hitEvent.SetArgs(owner, hitTarget, this, damageData, hit);
            hitTarget.GetHit(hitEvent);
            //触发命中事件
            EventManager.instance.Publish(hitEvent);
            #region [功能]玩家角色命中特效
            if (owner is CharacterScript && !isHitEffect)
            {
                isHitEffect = true;
                //命中自己顿帧
                if (damageData.hitFreezeTime > 0)
                {
                    (owner as CharacterScript).HitFreeze(damageData.hitFreezeTime);
                }
                //命中相机震动
                if (damageData.hitCameraShake.intensity > 0)
                {
                    CameraManager.instance.CameraShake(damageData.hitCameraShake, Global.P_CS_DealHit);
                }
            }
            #endregion
            //命中对象为敌人类型
            if (hitTarget is EnemyScript)
            {
                //命中敌人顿帧
                if (damageData.hitFreezeTime > 0)
                {
                    (hitTarget as EnemyScript).HitFreeze(damageData.hitFreezeTime);
                }
            }
        }
    }

    //伤害判定
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IHitable hit = collision.GetComponentInParent<IHitable>();
        if (hit != null)
        {
            TeamScript teamScript = collision.GetComponentInParent<TeamScript>();
            if (!ownerTeam.IsSameTeam(teamScript) && !unitIsHit(teamScript))
            {
                hitUnits.Add(teamScript);
                HitTargetUnit(hit);
            }
        }
    }
}
