using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * SpriteAnimFixer
 * 精灵动画修复器
 * 负责修复动画器上一些存在的不满足需求的问题。
 */

public class SpriteAnimFixer : MonoBehaviour
{
    Transform _transform;
    void Start()
    {
        _transform = transform;
    }
    void LateUpdate()
    {
        ScaleFix();
    }

    //修正动画过渡导致的纸片绕3D轴旋转问题
    private void ScaleFix()
    {
        //Debug.Log(_transform.localScale.x);
        if (Mathf.Abs(_transform.localScale.x) != 1)
        {
            float xScale = (_transform.localScale.x >= 0) ? 1 : -1;
            _transform.localScale = new Vector3(xScale, _transform.localScale.y, _transform.localScale.z);
        }
    }
}
