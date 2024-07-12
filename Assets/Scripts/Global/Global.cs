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
public interface IDirectable
{
    Vector2 GetFaceDir();
};//拥有朝向的单位

#endregion
#region 枚举
public enum Ternary
{
    zero,
    positive,
    negative = -1
};//通用的可选三值的数据类型
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
public enum FaceState
{
    lockedDir,  //锁定朝向（无法改变）
    moveDir,    //朝向移动方向
    targetDir   //朝向目标所在方向
};//角色的朝向状态

#endregion
#region 类与结构体
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
