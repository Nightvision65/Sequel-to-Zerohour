using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
/*
 * ObjectPoolManager
 * 对象池管理器
 * 用于管理全局对象池，控制同一Prefab只建立一个对象池
 * 经常创建/销毁的内容都进入对象池提升效率
 */
public interface IPoolObject
{
    //记录自身的prefab
    GameObject prefab { get; set; }
    //返回到对象池时重置自身状态
    void OnRelease();
}//脚本附着的物体使用对象池

public class ObjectPoolManager : SerializedMonoBehaviour
{
    public static ObjectPoolManager instance;
    [SerializeField] private int maxCapacity; //对象池默认最大容量
    [SerializeField] private Dictionary<GameObject, ObjectPool<GameObject>> pool = new Dictionary<GameObject, ObjectPool<GameObject>>();    //通过Prefab访问的对象池
    void Awake()
    {
        instance = this;
    }

    //创建对象池
    public ObjectPool<GameObject> Create(GameObject prefab)
    {
        ObjectPool<GameObject> tpool;
        if (!pool.TryGetValue(prefab, out tpool))
        {
            tpool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab), // 创建方法
            actionOnGet: (obj) => obj.SetActive(true),  // 从池中取出时激活对象
            actionOnRelease: (obj) => obj.SetActive(false), // 返回池中时禁用对象
            actionOnDestroy: (obj) => Destroy(obj)); // 池销毁对象时的处理方法
            pool.Add(prefab, tpool);
        }
        return tpool;
    }

    //从对象池中获取对象
    public GameObject Get(GameObject prefab)
    {
        ObjectPool<GameObject> tpool;
        //如果没有该对象的对象池，创建一个
        if (!pool.TryGetValue(prefab, out tpool))
            tpool = Create(prefab);
        GameObject obj = tpool.Get();
        //使对象记录自身的prefab，便于释放
        IPoolObject script = obj.GetComponent<IPoolObject>();
        script.prefab = prefab;
        return obj;
    }

    //将对象释放回对象池（对象自身调用）
    public void Release(IPoolObject obj)
    {
        obj.OnRelease();
        pool[obj.prefab].Release((obj as MonoBehaviour).gameObject);
    }
}