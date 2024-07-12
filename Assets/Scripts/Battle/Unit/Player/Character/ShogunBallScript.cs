using Ara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;
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
    [SerializeField] private DamageScript meleeDScript;
    [SerializeField] private AraTrail weaponTrail;
    [SerializeField] private int arrowNum;
    [SerializeField] private float arrowForce;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private ActionExtra DemonExtra;
    [SerializeField] private float deflectTime;
    [SerializeField] private float unsheathBlockTime;
    private Vector2 attackDirection;
    private float animSpeed;    //记录攻击动作影响的动画速度
    private bool weaponWithin;  //是否收刀入鞘(用于控制刀的图层顺序)
    private Coroutine deflect;
    private bool deflectRunning;
    new void Update()
    {
        base.Update();
    }

    protected override void OnAnimStateChange(int exitState, int enterState)
    {
        if (_stateMachine.Equals("Unsheath Out", enterState))
        {
            faceState = FaceState.lockedDir;
            FaceTarget(true);
            _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
        }
        if (_stateMachine.Equals("Guarding", enterState))
        {
            faceState = FaceState.targetDir;
        }
        if (_stateMachine.Equals("Guard Parry Left", enterState) || _stateMachine.Equals("Guard Parry Right", enterState))
        {
            _animator.SetBool("Deflect", false);
            if (deflectRunning)
            {
                StopCoroutine(deflect);
                deflectRunning = false;
            }
        }
        if (_stateMachine.Equals("Attack1", enterState) || _stateMachine.Equals("Attack2", enterState) || _stateMachine.Equals("Attack3", enterState))
        {
            _animator.SetBool("Attack", false);
        }
    }
    protected override void OnAnimTagChange(int exitTag, int enterTag)
    {
        //Debug.Log(exitTag.ToString() +" "+ enterTag.ToString());
        //先触发退出事件
        if (_stateMachine.Equals("Dodge", exitTag))
        {
            DodgeEnd();
        }
        if (_stateMachine.Equals("Attack", exitTag))
        {
            _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
        }
        if (_stateMachine.Equals("Skill1", exitTag))
        {
            _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
        }
        if (_stateMachine.Equals("Skill2", exitTag))
        {
            _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
            _modifier.RemoveModifier("defense", "skill2", ModifierType.MultiIndie);
            WeaponSetIn(false);
        }
        if (_stateMachine.Equals("Skill3", exitTag))
        {
            _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
        }
        //再触发进入事件
        if (_stateMachine.Equals("Dodge", enterTag))
        {
            _animator.SetBool("Dodge", false);
            DodgeStart();
        }
        if (_stateMachine.Equals("Idle", enterTag))
        {
            faceState = FaceState.moveDir;
        }
        if (_stateMachine.Equals("Attack", enterTag))
        {
            faceState = FaceState.lockedDir;
            _modifier.SetModifier("move", "action", 0.5f, ModifierType.MultiIndie);
            FaceTarget(false);
        }
        if (_stateMachine.Equals("Skill1", enterTag))
        {
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.5f, ModifierType.MultiIndie);
            FaceTarget(false);
        }
        if (_stateMachine.Equals("Skill2", enterTag))
        {
            _animator.SetBool("Skill2", false);
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.5f, ModifierType.MultiIndie);
            _modifier.SetModifier("defense", "skill2", 0f, ModifierType.MultiIndie);
        }
        if (_stateMachine.Equals("Skill3", enterTag))
        {
            _animator.SetBool("Skill3", false);
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.5f, ModifierType.MultiIndie);
            FaceTarget(false);
            _animator.SetInteger("Arrow", arrowNum);
        }
        //最后触发混合事件
    }
    protected override void OnAnimEvent(string[] paras)
    {
        switch (paras[0])
        {
            //进行位移
            case "SetShift":
                SetShift(paras[1]);
                break;
            //设置攻击
            case "AttackStart":
                MeleeAttackDeal(paras[1]);
                break;
            //激活攻击判定
            case "ActivateCollider":
                _colliderAnim.ActivateCollider(int.Parse(paras[1]), float.Parse(paras[2]));
                break;
            //攻击结束
            case "AttackEnd":
                MeleeAttackDealEnd();
                break;
            //武器收出鞘
            case "SetWeaponIn":
                WeaponSetIn(bool.Parse(paras[1]));
                break;
            //居合格挡
            case "UnsheathBlock":
                UnsheathBlock();
                break;
            //射箭
            case "ShotArrow":
                ArrowShot();
                break;
        }
    }

    //重置状态
    protected override void ResetStatus()
    {
        weaponTrail.emit = false;
        _animator.speed = 1f;
        faceState = FaceState.moveDir;
        _modifier.RemoveModifier("move", "action", ModifierType.MultiIndie);
        _modifier.RemoveModifier("defense", "skill1", ModifierType.MultiIndie);
        _modifier.RemoveModifier("defense", "skill2", ModifierType.MultiIndie);
        WeaponSetIn(false);
    }
    private void FaceTarget(bool now)
    {
        attackDirection = (targetPosition - (Vector2)_transform.position).normalized;
        _ball.CharacterFace(attackDirection, now);
    }

    //近战攻击开始
    public void MeleeAttackDeal(string key)
    {
        _ball.BallSpriteProcess(true);
        _modifier.SetModifier("move", "action", 0f, ModifierType.MultiIndie);
        SetAttack(key, 0);
        TrailFix();
        animSpeed = _animator.speed;
        _animator.speed = 1f;
    }

    //近战攻击结束
    public void MeleeAttackDealEnd()
    {
        _animator.speed = animSpeed;
    }


    //[Skill1]防御弹反协程
    public IEnumerator GuardDeflectEnd(float time)
    {
        deflectRunning = true;
        yield return new WaitForSeconds(time);
        _animator.SetBool("Deflect", false);
        deflectRunning = false;
    }

    //[Skill2]改变收刀/出刀的刀图层
    public void WeaponSetIn(bool within)
    {
        if (within)
        {
            _ball.SetWeaponSort(Ternary.negative);
        }
        else
        {
            _ball.SetWeaponSort(Ternary.zero);
        }
    }

    //[Skill2]居合格挡
    public void UnsheathBlock()
    {
        _modifier.SetModifier("defense", "skill2", 0f, ModifierType.MultiIndie);
        StartCoroutine(UnsheathBlockEnd(unsheathBlockTime));
    }

    //[Skill1]居合格挡协程
    public IEnumerator UnsheathBlockEnd(float time)
    {
        yield return new WaitForSeconds(time);
        _modifier.RemoveModifier("defense", "skill2", ModifierType.MultiIndie);
    }

    //[Skill3]弓箭射击
    public void ArrowShot()
    {
        FaceTarget(true);
        _animator.SetInteger("Arrow", _animator.GetInteger("Arrow") - 1);
        new ProjectileBuilder()
            .WithSource(this)
            .WithPrefab(arrowPrefab)
            .WithActionData(chActionData["Arrow"])
            .WithLaunchForce(arrowForce)
            .Build(arrowTransform);
    }

    //修复武器拖尾绘制
    public void TrailFix()
    {
        if (_ball.chWeapon[0].main.localScale.y == 1)
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
            _animator.SetBool("Skill1", true);
        }
        //幕玉按下格挡的瞬间可以弹反，但是不能抖刀术哦
        if (actionName == "Skill1" && inputState == KeyState.pressed && !deflectRunning)
        {
            _animator.SetBool("Deflect", true);
            deflect = StartCoroutine(GuardDeflectEnd(deflectTime));
        }
    }

    public override void OnHitEnter(ref UnitHitEvent hit)
    {
        //[Skill1]格挡判定
        //在格挡状态且同时按下了格挡按键才算在格挡
        if (_stateMachine.IsTag("Skill1") && _animator.GetBool("Skill1"))
        {
            Debug.Log("幕玉挡住了伤害！");
            faceState = FaceState.lockedDir;
            _ball.CharacterFace(-hit.hitData.knockback, true);
            _animator.SetTrigger("Guarded");
            if (_animator.GetBool("Deflect"))
            {
                _modifier.SetModifier("defense", "skill1", 0f, ModifierType.MultiIndie);
            }
            else
            {
                _modifier.SetModifier("defense", "skill1", 0.3f, ModifierType.MultiIndie);
            }
        }
    }
    public override void OnHitExit(ref UnitHitEvent hit)
    {
        if (_modifier.GetModifier("defense", "skill1", ModifierType.MultiIndie) != float.MinValue)
        {
            _modifier.RemoveModifier("defense", "skill1", ModifierType.MultiIndie);
            //弹反时，施加额外效果
            if (_animator.GetBool("Deflect"))
            {
                //反弹飞行物
                if (hit.actionData.hasTag(ActionTag.projectile))
                {
                    ProjectileScript pScript = hit.script.GetComponentInParent<ProjectileScript>();
                    pScript.ReverseDir();
                    hit.script.SetHeadAgent(this);
                    hit.script.transform.rotation *= Quaternion.Euler(0, 0, 180);
                }
                //对近战削韧并上暴击标记
                if (hit.actionData.hasTag(ActionTag.melee))
                {
                    SetAttack("Deflect", 0);
                    meleeDScript.HitTargetUnit((IHitable)hit.agent);
                }
            }
        }
    }
}
