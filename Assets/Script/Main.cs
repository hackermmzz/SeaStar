using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    //
    public PlayerController player;
    public Map map;
    public Camera camera;
    //
    void Start()
    {
        //初始化配置
        InitConfig();

    }


    void Update()
    {

    }

    void InitConfig()
    {
        Config.ConfigInit();
        //
        Config.player = player;
        Config.map = map;
        Config.camera = camera;
    }
}
