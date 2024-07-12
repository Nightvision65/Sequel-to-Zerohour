using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
/*
 * PlayerController
 * 角色控制器
 * 负责读取用户输入并且进行基础的处理，包括输入留存、多设备输入等。
 * 此后将输入信息传输给角色脚本。
 * 不包括判断用户输入的合法性。
 */

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
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public Transform character;
    [SerializeField] private Device device;  //输入设备，0为键鼠，1为手柄
    [SerializeField] private float preInputTime;    //预输入时间，规定单次按键的按下会持续生效多久
    Dictionary<string, KeyState> inputState = new Dictionary<string, KeyState>();   //输入状态，记录每个按键输入目前处于的状态
    Dictionary<string, Coroutine> inputCoroutine = new Dictionary<string, Coroutine>(); //输入协程，用于记录每个按键的输入留存
    CharacterScript mScript;    //绑定的角色脚本，向角色脚本传输数据。
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        Init();
    }
    
    void Update()
    {
        TranferTargetInfo();
        mScript.SetTargetFromInput(device);
    }

    //初始化，获得角色的组件
    void Init()
    {
        mScript = character.GetComponent<CharacterScript>();
    }

    //目标输入
    public void TranferTargetInfo()
    {
        if (device == Device.keyboard)
        {
            //键鼠的情况，传输鼠标位置
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mScript.deviceLockInput = mousePosition;
        }//手柄的情况，传输右摇杆指向方向（OnInputAim方法中）
    }

    //移动输入响应
    public void OnInputMove(InputAction.CallbackContext content)
    {
        if (content.performed)
        {
            Vector2 dir = content.ReadValue<Vector2>();
            //Debug.Log(dir);
            mScript.MoveStart(dir);
        }
        else
        {
            if (content.canceled)
            {
                mScript.MoveEnd();
            }
        }
    }

    //瞄准输入响应（手柄）
    public void OnInputAim(InputAction.CallbackContext content)
    {
        if (content.performed) 
        {
            Vector2 dir = content.ReadValue<Vector2>();
            //Debug.Log(dir);
            mScript.deviceLockInput = dir;
        }
        else
        {
            if (content.canceled)
            {
                mScript.deviceLockInput = Vector2.zero;
            }
        }
    }

    //动作输入响应
    //要加新的按键的话，记得在PlayerInput组件里绑定该事件
    public void OnInputAction(InputAction.CallbackContext content)
    {
        string input = content.action.name;
        //Debug.Log(input);
        if (input.Length == 0) return;
        if (content.performed)
        {
            //如果按下按键时该按键仍是输入留存状态，杀死之前的协程
            Coroutine t;
            if (inputCoroutine.TryGetValue(input, out t) && t != null)
            {
                StopCoroutine(t);
            }
            //按键按下后，开启时钟进行输入留存
            inputState[input] = KeyState.pressed;
            mScript.SetAction(input, KeyState.pressed);
            inputCoroutine[input] = StartCoroutine(KeySave(input, preInputTime));
        }
        else
        {
            //松开时，判断按钮状态，非长按情况下不置0
            if (content.canceled)
            {
                KeyState t;
                if (inputState.TryGetValue(input, out t))
                {
                    //通过了按键留存的时间后，再向角色逻辑发送松开信号
                    if (t == KeyState.held)
                    {
                        mScript.SetAction(input, KeyState.released);
                    }
                    //但是输入逻辑上是直接松开(和实际情况同步)，留存结束后检查该变量判断是否松开
                    inputState[input] = KeyState.released;
                }
            }
        }
    }

    //按键留存协程
    //玩家按下按键后，按键信息会留存一段时间（处于pressed状态），此后若玩家已松开按键，则变为released状态，否则处于held状态。
    private IEnumerator KeySave(string key, float time)
    {
        yield return new WaitForSeconds(time);
        KeyState t;
        if (inputState.TryGetValue(key, out t))
        {
            inputCoroutine[key] = null;
            if(t == KeyState.pressed)
            {
                //按键仍然被按着，转入held状态
                inputState[key] = KeyState.held;
                mScript.SetAction(key, KeyState.held);
            }
            else
            {
                //按键已经松开，转入released状态
                inputState[key] = KeyState.released;
                mScript.SetAction(key, KeyState.released);
            }
        }
    }

    //检测设备更换
    public void OnControlsChange(PlayerInput mInput)
    {
        if(mScript)
            mScript.deviceLockInput = Vector2.zero;
        if (mInput.currentControlScheme == "Controller")
        {
            device = Device.controller;
        }
        else
        {
            device = Device.keyboard;
        }
    }
    

}
