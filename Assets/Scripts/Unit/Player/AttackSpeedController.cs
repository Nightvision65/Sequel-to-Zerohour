using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class AttackSpeedController : StateMachineBehaviour
{
    public void OnStateEnter(Animator animator)
    {
        animator.speed = animator.GetFloat("AttackSpeed");
    }

    public void OnStateExit(Animator animator)
    {
        animator.speed = 1.0f;
    }

}
