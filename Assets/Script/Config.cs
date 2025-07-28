using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class Config:MonoBehaviour
{
    //一个区块的大小
    public static readonly int ChunkSize = 32;
    //一个块的尺寸
    public static readonly float BlockLength = 1.0f;
    //加载的区块个数
    public static readonly int ChunkCnt = 32;
    //地形高度生成着色器
    public static readonly int TerrainFrequency = 25*ChunkSize;//
    public static readonly float TerrainHeightScale = 100.0f;//
    public static readonly string TerrainMaterial = new string("Material/land");
    public static readonly string terrainHeightGenerateShaderPath = "Shader/TerrainGenerate";
    public static ComputeShader terrainHeightGenerate;//地形高度生成着色器
    public static int terrainHeightGenerateKernelHandle;
    public static ComputeBuffer terrainComputeBuffer;//缓存生成的高度数据
    //地形热力侵蚀着色器
    public static readonly int ThermalErosionLoopCnt = 20;//迭代5次热力侵蚀
    public static readonly float ThermalErosionTanTheta = (float)System.Math.Tan(60.0 * System.Math.PI / 180.0);
    public static readonly float ThermalErosionFactor0 = 0.5f;
    public static readonly float ThermalErosionFactor1 = 0.5f;
    public static readonly string TerrainThermalErosionShaderPath = "Shader/Thermal Erosion";
    public static ComputeShader terrainThermalErosion;
    public static int terrainThermalErosionHandle;
    public static ComputeBuffer ThermalErosionBuffer;//缓存生成的热力侵蚀数据
    //Lod分层
    public static readonly float[] Lod = new float[] { ChunkSize * 1.2f, ChunkSize * 3, ChunkSize * 9 };
    //
    public static PlayerController player; //主角色
    public static Map map;//大世界
    public static Camera camera;//主摄像机
    public static Database database;//数据库
    public static Queue<ChunkData> ChunkDataWriteToDataBase;//向数据库ChunkData写数据的任务队列
    public static SortedSet<Vector2Int> ChunkExist;//包含所有已经存在的chunk的坐标
    public static readonly int TerrainMaxUpdateCntPerFrame = 5;//每一帧最多加载5个区块
    public static readonly int ChunkForceLoadCnt = ChunkSize / 2;//当目前加载的区块尺寸小于该值时，触发强制加载
    public static ThreadPool threadPool;//线程池
    //初始化所有配置
    public static void ConfigInit()
    {
        //初始化线程池
        threadPool = new ThreadPool();
        //初始化地形高度生成着色器
        terrainHeightGenerate = Resources.Load<ComputeShader>(terrainHeightGenerateShaderPath);
        terrainHeightGenerateKernelHandle = terrainHeightGenerate.FindKernel("CSMain");
        if (terrainHeightGenerateKernelHandle == -1)
        {
            Debug.Log("地形高度生成着色器初始化错误!");
        }
        terrainComputeBuffer = new ComputeBuffer((ChunkSize + 1) * (ChunkSize + 1), sizeof(float));//预分配一个buffer存储计算着色器生成的高度数据,实际生成会多一层
        //初始化地形热力侵蚀着色器
        terrainThermalErosion = Resources.Load<ComputeShader>(TerrainThermalErosionShaderPath);
        terrainThermalErosionHandle = terrainThermalErosion.FindKernel("CSMain");
        if (terrainThermalErosionHandle == -1)
        {
            Debug.Log("地形热力侵蚀着色器初始化错误!");
        }
        ThermalErosionBuffer = new ComputeBuffer((ChunkSize + 1) * (ChunkSize + 1), sizeof(float));
        //初始化数据库
        database = new Database(Application.streamingAssetsPath + "/SeaStar.db");
        //读取所有已经存在的chunk坐标
        ChunkExist = new SortedSet<Vector2Int>(Comparer<Vector2Int>.Create((a, b) =>
            {
                if (a.x == b.x) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            }));
        var conn = database.connection;
        var ret = conn.Query<ChunkData>("select posx,posz from ChunkData");
        foreach (var element in ret)
        {
            ChunkExist.Add(new Vector2Int(element.posx, element.posz));
        }
        //在子线程开辟一个任务用于写数据库
        ChunkDataWriteToDataBase = new Queue<ChunkData>();
        threadPool.AddTask((obj) =>
        {
            while (true)
            {
                if (ChunkDataWriteToDataBase.Count != 0)
                {
                    ChunkData cd;
                    lock (database.Lock())
                    {
                        cd = ChunkDataWriteToDataBase.Dequeue();
                    }
                    database.connection.Insert(cd);

                    //向全局记录增加一条
                    lock (ChunkExist)
                    {
                        ChunkExist.Add(new Vector2Int(cd.posx,cd.posz));
                    }
                }   
            }
        },null);
    }
}
