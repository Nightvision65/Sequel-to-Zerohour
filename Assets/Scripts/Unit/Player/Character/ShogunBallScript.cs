using Ara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
/*
 * ShogunBallScript
 * 幕玉脚本
 * 角色脚本的子类
 * 负责幕玉角色的相关实现
 */

[Serializable]
public class ShogunBallScript : CharacterScript
{
    [SerializeField] private AraTrail weaponTrail;
    [SerializeField] private int arrowNum;
    [SerializeField] private float arrowForce;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private ActionExtra DemonExtra;
    [SerializeField] private float deflectTime;
    private Vector2 attackDirection;
    private float animSpeed;    //记录攻击动作影响的动画速度
    private bool weaponWithin;  //是否收刀入鞘(用于控制刀的图层顺序)
    private Coroutine deflect;
    private bool deflectRunning;
    new void Update()
    {
        base.Update();
    }
    //订阅命中事件
    void OnEnable()
    {
        EventManager.instance.Subscribe<UnitHitEvent>(OnUnitHit, Global.P_E_Hit_ShogunDeflect);
    }

    void OnDisable()
    {
        EventManager.instance.Unsubscribe<UnitHitEvent>(OnUnitHit, Global.P_E_Hit_ShogunDeflect);
    }
    protected override void OnAnimStateChange(int exitState, int enterState)
    {
        if (mSMM.Equals("Iaido Out", enterState))
        {
            faceState = FaceState.lockedDir;
            FaceTarget(true);
            RemoveModifier(Modifier.move, "action", true);
        }
        if (mSMM.Equals("Guarding", enterState))
        {
            faceState = FaceState.targetDir;
        }
        if (mSMM.Equals("Guard Parry Left", enterState) || mSMM.Equals("Guard Parry Right", enterState))
        {
            mAnim.SetBool("Deflect", false);
            if (deflectRunning)
            {
                StopCoroutine(deflect);
                deflectRunning = false;
            }
        }
    }
    protected override void OnAnimTagChange(int exitTag, int enterTag)
    {
        //Debug.Log(exitTag.ToString() +" "+ enterTag.ToString());
        //先触发退出事件
        if (mSMM.Equals("Dodge", exitTag))
        {
            DodgeEnd();
        }
        if (mSMM.Equals("Attack", exitTag))
        {
            RemoveModifier(Modifier.move, "action", true);
        }
        if (mSMM.Equals("Skill1", exitTag))
        {
            RemoveModifier(Modifier.move, "action", true);
        }
        if (mSMM.Equals("Skill2", exitTag))
        {
            RemoveModifier(Modifier.move, "action", true);
            WeaponSetIn(0);
        }
        if (mSMM.Equals("Skill3", exitTag))
        {
            RemoveModifier(Modifier.move, "action", true);
        }
        //再触发进入事件
        if (mSMM.Equals("Dodge", enterTag))
        {
            DodgeStart();
        }
        if (mSMM.Equals("Idle", enterTag))
        {
            faceState = FaceState.moveDir;
        }
        if (mSMM.Equals("Attack", enterTag))
        {
            faceState = FaceState.lockedDir;
            SetModifier(Modifier.move, "action", 0.5f, true);
            FaceTarget(false);
        }
        if (mSMM.Equals("Skill1", enterTag))
        {
            faceState = FaceState.targetDir;
            SetModifier(Modifier.move, "action", 0.3f, true);
            FaceTarget(false);
        }
        if (mSMM.Equals("Skill2", enterTag))
        {
            faceState = FaceState.targetDir;
            SetModifier(Modifier.move, "action", 0.5f, true);
        }
        if (mSMM.Equals("Skill3", enterTag))
        {
            faceState = FaceState.targetDir;
            SetModifier(Modifier.move, "action", 0.5f, true);
            FaceTarget(false);
            mAnim.SetInteger("Arrow", arrowNum);
        }
        //最后触发混合事件
    }
    //重置状态
    protected override void ResetStatus()
    {
        weaponTrail.emit = false;
        mAnim.speed = 1f;
        faceState = FaceState.moveDir;
        RemoveModifier(Modifier.move, "action", true);
        RemoveModifier(Modifier.defense, "skill1", true);
        WeaponSetIn(0);
    }
    private void FaceTarget(bool now)
    {
        attackDirection = (targetPosition - (Vector2)mTransform.position).normalized;
        ball.CharacterFace(attackDirection, now);
    }

    //攻击判定开始
    public void MeleeAttackDeal(string key)
    {
        ball.BallSpriteProcess(true);
        SetModifier(Modifier.move, "action", 0f, true);
        mRbody.AddForce(attackDirection * chActionData[key].moveForce);
        activeRbody.AddForce(attackDirection * chActionData[key].moveForce);
        AttackSet(key);
        TrailFix();
        weaponTrail.emit = true;
        animSpeed = mAnim.speed;
        mAnim.speed = 1f;
    }

    //攻击判定结束
    public void MeleeAttackDealEnd()
    {
        weaponTrail.emit = false;
        mAnim.speed = animSpeed;
    }


    //[Skill1]防御弹反协程
    public IEnumerator GuardDeflectEnd(float time)
    {
        deflectRunning = true;
        yield return new WaitForSeconds(time);
        mAnim.SetBool("Deflect", false);
        deflectRunning = false;
    }

    //[Skill2]改变收刀/出刀的刀图层
    public void WeaponSetIn(int within)
    {
        weaponWithin = Convert.ToBoolean(within);
        if (weaponWithin)
        {
            ball.SetWeaponSort(Ternary.negative);
        }
        else
        {
            ball.SetWeaponSort(Ternary.zero);
        }
    }

    //[Skill3]弓箭射击
    public void ArrowShot()
    {
        FaceTarget(true);
        mAnim.SetInteger("Arrow", mAnim.GetInteger("Arrow") - 1);
        GameObject arrow = ObjectPoolManager.instance.Get(arrowPrefab);
        ProjectileScript arrowScript = arrow.GetComponent<ProjectileScript>();
        arrowScript.SetTransform(arrowTransform);
        arrowScript.SetDScriptData(this, "Arrow");
        arrowScript.LaunchForward(arrowForce);
    }

    //修复武器拖尾绘制
    public void TrailFix()
    {
        if (ball.chWeapon[0].main.localScale.y == 1)
        {
            weaponTrail.sorting = AraTrail.TrailSorting.OlderOnTop;
        }
        else
        {
            weaponTrail.sorting = AraTrail.TrailSorting.NewerOnTop;
        }
    }

    public override void SetActionSpecial(string actionName, KeyState inputState)
    {
        //幕玉的Skill1长按触发
        if (actionName == "Skill1" && inputState == KeyState.held)
        {
            mAnim.SetBool("Skill1", true);
        }
        //幕玉按下格挡的瞬间可以弹反，但是不能抖刀术哦
        if (actionName == "Skill1" && inputState == KeyState.pressed && !deflectRunning)
        {
            mAnim.SetBool("Deflect", true);
            deflect = StartCoroutine(GuardDeflectEnd(deflectTime));
        }
    }

    public override void OnHitEnter(HitData hit)
    {
        //[Skill1]格挡判定
        //在格挡状态且同时按下了格挡按键才算在格挡
        if (mSMM.IsTag("Skill1") && mAnim.GetBool("Skill1"))
        {
            Debug.Log("幕玉挡住了伤害！");
            faceState = FaceState.lockedDir;
            ball.CharacterFace(-hit.knockback, true);
            mAnim.SetTrigger("Guarded");
            SetModifier(Modifier.defense, "skill1", 0f, true);
        }
    }
    public override void OnHitExit(HitData hit)
    {
        if (GetModifier(Modifier.defense, "skill1", true) != float.MinValue)
        {
            RemoveModifier(Modifier.defense, "skill1", true);
        }
    }
    //命中事件委托
    private void OnUnitHit(UnitHitEvent hit)
    {
        //自身受击时
        if(hit.patient == (IHitable)this)
        {
            //弹反时，施加额外效果
            if (mSMM.IsTag("Skill1") && mAnim.GetBool("Skill1") && mAnim.GetBool("Deflect"))
            {
                //反弹飞行物
                if (hit.actionData.hasTag(ActionTag.projectile))
                {
                    ProjectileScript pScript = hit.script.GetComponentInParent<ProjectileScript>();
                    pScript.ReverseDir();
                    pScript.IgnoreNextHit();
                    hit.script.SetOwner(this);
                    hit.script.transform.rotation *= Quaternion.Euler(0, 0, 180);
                }
            }
        }
    }
}
