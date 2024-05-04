using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/*
 * DamageScript
 * 伤害脚本
 * 管理不会同时出现的攻击判定区域，所有单位通用
 * 负责将所有来源的ActionData转化为Hitdata
 */

public class DamageScript : SerializedMonoBehaviour
{
    [SerializeField] private TeamScript agentTeam;    //伤害来源队伍
    [SerializeField] private List<IAttackable> agents;     //伤害施加相关者
    private List<TeamScript> hitUnits = new List<TeamScript>();   //已经被本次攻击命中过的单位
    private ActionData damageData;  //伤害来源数据
    private Vector2 knockDirection;  //击退方向
    private bool isActive = false;  //有判定区域被激活
    private UnitHitEvent hitEvent = new UnitHitEvent(); //用于广播击中事件
    private List<Collider2D> damageColliders;   //伤害判定区域
    private Rigidbody2D damageRbody; //绑定的Rigidbody2D


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
        hitUnits.Clear();
    }
    //设置伤害最终来源（以及队伍）
    public void SetHeadAgent(IAttackable head)
    {
        agents.Insert(0, head);
        agentTeam = (head as MonoBehaviour).GetComponent<TeamScript>();
    }

    //添加伤害施加者
    public void AddAgent(IAttackable agent)
    {
        agents.Add(agent);
    }

    //设置脚本目前正处于的动作key
    public void SetAction(ActionData data)
    {
        damageData = data;
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
        if (!agentTeam.IsSameTeam(teamScript))
        {
            teamScript.SetAggro(agentTeam.team, true);
            switch (damageData.baseData.knocktype)
            {
                case KnockType.aim:
                    knockDirection = agents[0].GetFaceDir();
                    break;
                case KnockType.velocity:
                    knockDirection = agents.Last().GetFaceDir();
                    break;
                case KnockType.recoil:
                    knockDirection = -hitTarget.GetFaceDir();
                    break;
            }
            float damage = damageData.baseData.damage;
            #region [功能]处理额外动作附件
            float critchance = 0;
            foreach (ActionExtra actionExtra in damageData.extraData.Values)
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
            HitData hit = new HitData(damage, damageData.baseData.impact, damageData.baseData.knockback * knockDirection);
            hitEvent.SetArgs(agents[0], hitTarget, this, damageData, hit);
            hitTarget.GetHit(ref hitEvent);
            //触发命中事件
            foreach (IAttackable agent in agents)
            {
                agent.LandHit(hitEvent);
            }
            EventManager.instance.Publish(hitEvent);
        }
    }

    //伤害判定
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IHitable hit = collision.GetComponentInParent<IHitable>();
        if (hit != null)
        {
            TeamScript teamScript = collision.GetComponentInParent<TeamScript>();
            if (!agentTeam.IsSameTeam(teamScript) && !unitIsHit(teamScript))
            {
                hitUnits.Add(teamScript);
                HitTargetUnit(hit);
            }
        }
    }
}
