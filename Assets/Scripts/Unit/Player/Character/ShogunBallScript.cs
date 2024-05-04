using Ara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
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
        if (_stateMachine.Equals("Iaido Out", enterState))
        {
            faceState = FaceState.lockedDir;
            FaceTarget(true);
            _modifier.RemoveModifier("move", "action", true);
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
            _modifier.RemoveModifier("move", "action", true);
        }
        if (_stateMachine.Equals("Skill1", exitTag))
        {
            _modifier.RemoveModifier("move", "action", true);
        }
        if (_stateMachine.Equals("Skill2", exitTag))
        {
            _modifier.RemoveModifier("move", "action", true);
            WeaponSetIn(0);
        }
        if (_stateMachine.Equals("Skill3", exitTag))
        {
            _modifier.RemoveModifier("move", "action", true);
        }
        //再触发进入事件
        if (_stateMachine.Equals("Dodge", enterTag))
        {
            DodgeStart();
        }
        if (_stateMachine.Equals("Idle", enterTag))
        {
            faceState = FaceState.moveDir;
        }
        if (_stateMachine.Equals("Attack", enterTag))
        {
            faceState = FaceState.lockedDir;
            _modifier.SetModifier("move", "action", 0.5f, true);
            FaceTarget(false);
        }
        if (_stateMachine.Equals("Skill1", enterTag))
        {
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.3f, true);
            FaceTarget(false);
        }
        if (_stateMachine.Equals("Skill2", enterTag))
        {
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.5f, true);
        }
        if (_stateMachine.Equals("Skill3", enterTag))
        {
            skill3timer = skill3CD;
            faceState = FaceState.targetDir;
            _modifier.SetModifier("move", "action", 0.5f, true);
            FaceTarget(false);
            _animator.SetInteger("Arrow", arrowNum);
        }
        //最后触发混合事件
    }
    //重置状态
    protected override void ResetStatus()
    {
        weaponTrail.emit = false;
        _animator.speed = 1f;
        faceState = FaceState.moveDir;
        _modifier.RemoveModifier("move", "action", true);
        _modifier.RemoveModifier("defense", "skill1", true);
        WeaponSetIn(0);
    }
    private void FaceTarget(bool now)
    {
        attackDirection = (targetPosition - (Vector2)_transform.position).normalized;
        _ball.CharacterFace(attackDirection, now);
    }

    //攻击判定开始
    public void MeleeAttackDeal(string key)
    {
        _ball.BallSpriteProcess(true);
        _modifier.SetModifier("move", "action", 0f, true);
        _rigidbody.AddForce(attackDirection * chActionData[key].baseData.moveForce);
        activeRbody.AddForce(attackDirection * chActionData[key].baseData.moveForce);
        AttackSet(key, 0);
        TrailFix();
        weaponTrail.emit = true;
        animSpeed = _animator.speed;
        _animator.speed = 1f;
    }

    //攻击判定结束
    public void MeleeAttackDealEnd()
    {
        weaponTrail.emit = false;
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
    public void WeaponSetIn(int within)
    {
        weaponWithin = Convert.ToBoolean(within);
        if (weaponWithin)
        {
            _ball.SetWeaponSort(Ternary.negative);
        }
        else
        {
            _ball.SetWeaponSort(Ternary.zero);
        }
    }

    //[Skill3]弓箭射击
    public void ArrowShot()
    {
        FaceTarget(true);
        _animator.SetInteger("Arrow", _animator.GetInteger("Arrow") - 1);
        GameObject arrow = ObjectPoolManager.instance.Get(arrowPrefab);
        ProjectileScript arrowScript = arrow.GetComponent<ProjectileScript>();
        arrowScript.SetTransform(arrowTransform);
        arrowScript.SetDScriptData(this, chActionData["Arrow"]);
        arrowScript.LaunchForward(arrowForce);
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

    public override void OnHitEnter(UnitHitEvent hit)
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
                _modifier.SetModifier("defense", "skill1", 0f, true);
            }
            else
            {
                _modifier.SetModifier("defense", "skill1", 0.2f, true);
            }
        }
    }
    public override void OnHitExit(UnitHitEvent hit)
    {
        if (_modifier.GetModifier("defense", "skill1", true) != float.MinValue)
        {
            _modifier.RemoveModifier("defense", "skill1", true);
            //弹反时，施加额外效果
            if (_animator.GetBool("Deflect"))
            {
                //反弹飞行物
                if (hit.actionData.hasTag(ActionTag.projectile))
                {
                    ProjectileScript pScript = hit.script.GetComponentInParent<ProjectileScript>();
                    pScript.ReverseDir();
                    pScript.IgnoreNextHit();
                    hit.script.SetHeadAgent(this);
                    hit.script.transform.rotation *= Quaternion.Euler(0, 0, 180);
                }
                //对近战削韧并上暴击标记
                if (hit.actionData.hasTag(ActionTag.melee))
                {
                    AttackSet("Deflect", 0);
                    meleeDScript.HitTargetUnit((IHitable)hit.agent);
                }
            }
        }
    }
}
