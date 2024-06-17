using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
/*
 * Global
 * 全局
 * 存放全局的数据结构和需要全局调用的方法
 */

#region 接口
public interface IHitable : IDirectable
{
    void GetHit(ref UnitHitEvent hit);
}//可被DamageScript攻击
public interface IAttackable : IDirectable 
{
    void LandHit(UnitHitEvent hit);
}//可使用DamageScript进行攻击
public interface IDirectable
{
    Vector2 GetFaceDir();
}
public interface IPoolObject
{
    //记录自身的prefab
    GameObject prefab { get; set; }
    //返回到对象池时重置自身状态
    void OnRelease();
}//脚本附着的物体使用对象池

#endregion
#region 枚举
public enum Ternary
{
    zero,
    positive,
    negative = -1
};//通用的可选三值的数据类型
public enum Status
{
    none,
    burning,
    freezing,
};//状态
public enum Attribute
{
    strength,       //力量：影响玩家的整体攻击力
    agility,        //敏捷：影响玩家普通攻击的速度
    intelligence,   //智力：影响玩家技能的冷却时间和充能速度
    dexterity,      //灵巧：影响玩家的暴击率
    artifice,       //谋略：影响玩家施加的异常状态的持续时间
    constitution,   //体质：影响玩家的最大生命值
    armor,          //护甲：影响玩家的防御力
    perception,     //感知：影响玩家的闪避率
    speed          //速度：影响玩家的移动速度
};//角色属性
public enum ActionTag
{
    direct, //直接来源于单位
    basic, 
    skill,
    melee,
    projectile,
    dot
};//动作标签
public enum KnockType
{
    aim,        //向伤害来源的朝向方向击退
    spread,     //向伤害来源的反方向击退
    velocity,   //向飞行道具的动能方向击退
    recoil      //按照原来方向击退
};//击退方式
public enum FaceState
{
    lockedDir,  //锁定朝向（无法改变）
    moveDir,    //朝向移动方向
    targetDir   //朝向目标所在方向
};//角色的朝向状态
public enum AIState
{
    patrol, //巡逻
    chase,  //追逐
    attack  //攻击
};//敌人的AI状态机

public enum Device
{
    keyboard,   //键盘&鼠标
    controller  //控制器手柄
};//设备类型
public enum KeyState
{
    released,
    held,
    pressed,
};//按键状态
#endregion
#region 类与结构体
//角色某个动作携带的数据（包括临时的额外数据）
public class ActionData
{
    public ActionBaseSO baseData;   //基础动作数据
    public Dictionary<string, ActionExtra> extraData;  //额外动作附件
    public bool hasTag(ActionTag tag)
    {
        return baseData.tags.Contains(tag);
    }
};

//状态数据
public struct StatusData
{
    public Status status;   //状态类型
    public float duration;  //状态持续时间
    public float chance;    //状态触发几率
};

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

//武器精灵数据（存放角色武器相关的Transform和偏移数据等）
public struct WeaponSpriteData
{
    public Transform main;  //主对象（负责旋转）
    public Transform sub;   //副对象（负责旋转时调整距离）
    public SortingGroup sort;   //武器排序（负责控制深度）
    public float offsetDis; //默认偏移距离
    public float offsetDir; //默认偏移角度
};

public struct CameraShakeData
{
    public float intensity;    //镜头抖动程度
    public float duration;    //镜头抖动时间
}
public struct Options
{
    public bool aimBot;
};//玩家设置
#endregion
public class Global : SerializedMonoBehaviour
{
    #region 常量
    public const int P_E_Hit_ShogunDeflect = 1;
    public const int P_E_Hit_ProjectileHit = 2;
    public const int P_CS_GetHit = 1;
    public const int P_CS_DealHit = 2;
    #endregion
    public static Global instance;
    public static Dictionary<string, int> physicsStack;   //物理忽略栈，确保冲刺等行为造成的单位间忽略物理功能能够正常运行
    public Dictionary<string, CameraShakeData> cameraShakeData;
    public Dictionary<string, Type> actionExtraParameters;
    public Dictionary<string, FlashDataSO> flashEffectData;
    public static Options options;
    void Awake()
    {
        instance = this;
        physicsStack = new Dictionary<string, int>();
        //Time.timeScale = 0.1f;
    }
    #region 全局方法
    //定时触发器
    public static bool IsTriggered(ref float timer)
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = 0;
                return true;
            }
        }
        return false;
    }

    //将两个int组成一个唯一的string（可以用作两个物件关系的key）
    public static string GetUniqueKey(int key1, int key2)
    {
        return key1 < key2 ? $"{key1}_{key2}" : $"{key2}_{key1}";
    }

    //将bool转换为Ternary
    public static Ternary ToTernary(bool b)
    {
        return b ? Ternary.positive : Ternary.negative;
    }

    //将float转换为Ternary
    public static Ternary ToTernary(float f)
    {
        return (Ternary)Mathf.Sign(f);
    }


    //关闭/启用角色与其他战斗单位的物理交互
    public void SetCollisionIgnore(Transform target, bool ignore)
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("BattleUnit");
        foreach (GameObject unit in units)
        {
            string key = GetUniqueKey(target.gameObject.GetInstanceID(), unit.GetInstanceID());
            if (ignore)
            {
                //忽略物理时，直接往栈里push
                if (!physicsStack.TryAdd(key, 1))
                {
                    physicsStack[key]++;
                }
            }
            else
            {
                //启用物理时，根据栈内元素执行操作
                int stack;
                if (physicsStack.TryGetValue(key, out stack))
                {
                    if (stack > 1)
                    {
                        //栈大于1，说明关系的另一边也在忽略物理，把自己的栈出了后直接结束
                        physicsStack[key]--;
                        return;
                    }
                    else
                    {
                        //栈小于等于1，直接关了栈释放内存，正常运行忽略物理
                        physicsStack.Remove(key);
                    }
                }
            }
            Collider2D[] colliders = unit.GetComponents<Collider2D>();
            Collider2D[] colliders2 = target.GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                if (!collider.isTrigger)
                {
                    foreach (Collider2D collider2 in colliders2)
                    {
                        if (!collider2.isTrigger)
                            Physics2D.IgnoreCollision(collider2, collider, ignore);
                    }
                }
            }
        }
    }
    #endregion
}
