using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class Map : MonoBehaviour
{
    //
    private SortedDictionary<Vector2Int, Terrain> terrain;//生成基础地形
    //
    void Start()
    {
        terrain = new SortedDictionary<Vector2Int, Terrain>(Comparer<Vector2Int>.Create((a, b) =>
            {
                if (a.x == b.x) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            })
        );
        for (int x = 0; x < Config.ChunkCnt; ++x)
        {
            for (int z = 0; z < Config.ChunkCnt; ++z)
            {
                var pos = new Vector2Int(x * Config.ChunkSize, z * Config.ChunkSize);
                //
                var gameObject = new GameObject();
                gameObject.transform.SetParent(this.transform);
                gameObject.name = new string("terrain:" + pos);
                //
                var terrain_ = gameObject.AddComponent<Terrain>();
                terrain_.SetOffset(pos);
                terrain.Add(pos, terrain_);
            }
        }
    }

    //
    void Update()
    {
        //更新地形
        UpdateTerrain();
    }
    //判断当前位置属于哪个chunk
    public Vector2Int GetPositionBelongToChunkPos(Vector3 position)
    {
        int xx = Mathf.FloorToInt(position.x / Config.ChunkSize) * Config.ChunkSize;
        int zz = Mathf.FloorToInt(position.z / Config.ChunkSize) * Config.ChunkSize;
        return new Vector2Int(xx, zz);
    }
    //获取指定位置当前的地形高度
    public float GetTerrainHeightAtWorldPosition(Vector3 position)
    {
        //计算得到当前的所属的terrain
        Vector2Int pos = GetPositionBelongToChunkPos(position);
        var terrain = GetTerrain(pos);
        return terrain.GetTerrainHeightAtWorldPosition(position);
    }
    public Terrain GetTerrain(Vector2Int pos)
    {
        if (terrain.TryGetValue(pos, out Terrain terrain_))
        {
            return terrain_;
        }
        //不存在，这里视为一个bug，报错
        Debug.Log("Error when get terrian in pos:" + pos);
        return null;
    }
    //更新地形
    public void UpdateTerrain()
    {
        //(根据人物当前位置来动态更新地形)
        var playPos = Config.player.transform.position;
        var pos = GetPositionBelongToChunkPos(playPos);
        Queue<pair<Vector2Int, Terrain>> outBound = new Queue<pair<Vector2Int, Terrain>>();
        int half = Config.ChunkCnt / 2;
        //对已经超出范围的移除掉
        foreach (var key in terrain.Keys)
        {
            int x = (key.x - pos.x) / Config.ChunkSize;
            int z = (key.y - pos.y) / Config.ChunkSize;
            if (x >= half || x < -half || z >= half || z < -half)
            {
                pair<Vector2Int, Terrain> ele = new pair<Vector2Int, Terrain>();
                ele.first = key;
                ele.second = terrain[key];
                outBound.Enqueue(ele);
            }
        }
        //生成需要生成的区块
        for (int x = -half * Config.ChunkSize; x < half * Config.ChunkSize; x += Config.ChunkSize)
        {
            for (int z = -half * Config.ChunkSize; z < half * Config.ChunkSize; z += Config.ChunkSize)
            {
                var pos_ = pos + new Vector2Int(x, z);
                if (!terrain.ContainsKey(pos_))
                {
                    if (outBound.Count == 0)
                    {
                        Debug.Log("the outbound buffer must not be zero? why? this is a bug");
                    }
                    var ele = outBound.Dequeue();
                    //从字典里面移除该项
                    terrain.Remove(ele.first);
                    //更新区块
                    var terrain_ = ele.second;
                    terrain_.SetOffset(pos_);
                    terrain_.name = "terrain:" + pos_;
                    //加入到字典
                    terrain[pos_] = terrain_;
                }
            }
        }

        //对没有生成区块的terrian生成
        UpdateTerrainLoad();
    }

    //计算当前得Lod
    public int CanculateCurrentLod(Vector2 pos0, Vector2 pos1)
    {
        //计算二维欧式距离
        Vector2 p = pos0 - pos1;
        float dis = MathF.Sqrt(p.x * p.x + p.y * p.y);
        int idx = Array.BinarySearch(Config.Lod, dis);
        return idx >= 0 ? idx : ~idx;
    }
    //更新地形的产生，以及细节设置
    private void UpdateTerrainLoad()
    {
        var pos = GetPositionBelongToChunkPos(Config.player.transform.position);
        List<pair<int, Terrain>> AllNeedGenerate = new List<pair<int, Terrain>>();
        foreach (var key in terrain.Keys)
        {
            var terrain_ = terrain[key];
            //计算lod
            var lod = CanculateCurrentLod(pos, key);
            terrain_.SetLod(lod);
            //
            if (!terrain_.GetIsGenerate())
            {
                pair<int, Terrain> tmp = new pair<int, Terrain>();
                Vector2Int p = pos - key;
                tmp.first = p.x * p.x + p.y * p.y;
                tmp.second = terrain_;
                AllNeedGenerate.Add(tmp);
            }
        }
        //排序，按照离角色的远近排序
        AllNeedGenerate.Sort((a, b) => a.first.CompareTo(b.first));
        //
        int cnt = Math.Min(Config.TerrainMaxUpdateCntPerFrame, AllNeedGenerate.Count);
        //查看是否触发了强制加载
        if (cnt > 0&&CheckNeedForceLoad(Mathf.Sqrt(AllNeedGenerate[0].first)))
        {
            //强制加载就加载所有待加载的，目前不优化
            cnt = AllNeedGenerate.Count;
        }
        //根据限度每次产生最多指定个数
        for (int i = 0; i < cnt; ++i)
        {
            var terrain = AllNeedGenerate[i].second;
            terrain.GenerateTerrain();
        }
    }
    //判断是否强制加载区域
    private bool CheckNeedForceLoad(float dis)
    {
        float threshold = Config.ChunkForceLoadCnt * Config.ChunkSize / 2;
        return dis <= threshold;
    }
}
