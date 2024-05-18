using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * DebugScript
 * 测试脚本
 * 下辖官方外挂和测试功能
 * 发布的时候一定要关掉
 */

public class DebugScript : MonoBehaviour
{
    public static DebugScript instance;
    public bool pauseGame;
    public bool lowfps;
    public int theLowfps;
    //public bool aimBot;
    private void Awake()
    {
        instance = this;
    }
    // Update is called once per frame
    void Update()
    {
        Time.timeScale = pauseGame ? 0.0f : 1.0f;
        Application.targetFrameRate = lowfps ? theLowfps : 0;
        //Global.options.aimBot = aimBot;
    }
}
