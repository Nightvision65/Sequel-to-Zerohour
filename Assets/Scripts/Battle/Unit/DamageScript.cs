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

public interface IHitable : IDirectable
{
    void GetHit(ref UnitHitEvent hit);
}//可被DamageScript攻击

public interface IAttackable : IDirectable
{
    void LandHit(UnitHitEvent hit);
}//可使用DamageScript进行攻击

public enum KnockType
{
    aim,        //向伤害来源的朝向方向击退
    spread,     //向伤害来源的反方向击退
    velocity,   //向飞行道具的动能方向击退
    recoil      //按照原来方向击退
};//击退方式

//命中数据（只存放必要的数据，想要更多数据去订阅命中事件）
public class HitData
{
    public float damage;    //伤害
    public float impact;    //削韧
    public Vector2 knockback;   //击退
    public List<StatusData> statusData;   //状态数据
    #region 构造函数
    public HitData(float damage = 0, float impact = 0, Vector2 knockback = new Vector2(), List<StatusData> statusData = null)
    {
        this.damage = damage;
        this.impact = impact;
        this.knockback = knockback;
        this.statusData = statusData ?? new List<StatusData>();
    }
    #endregion
};

public class DamageScript : SerializedMonoBehaviour
{
    [SerializeField] private TeamScript agentTeam;    //伤害来源队伍
    [SerializeField] private List<IAttackable> agents;     //伤害施加相关者
    private List<IHitable> hitUnits = new List<IHitable>();   //已经被本次攻击命中过的单位
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

    //获取本次开启判定的命中数
    public int GetHitCount()
    {
        return hitUnits.Count;
    }


    //判断敌人在本次判定中是否已经被击中过
    private bool UnitIsHit(IHitable unit)
    {
        foreach (IHitable hitUnit in hitUnits)
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
            Debug.Log(hitEvent.hitData.damage);
            //触发命中事件
            foreach (IAttackable agent in agents)
            {
                agent.LandHit(hitEvent);
            }
            //广播伤害事件
            if (!damageData.hasTag(ActionTag.dot))
            {
                EventManager.instance.Publish(hitEvent);
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
            if (!agentTeam.IsSameTeam(teamScript) && !UnitIsHit(hit))
            {
                hitUnits.Add(hit);
                HitTargetUnit(hit);
            }
        }
    }
}
