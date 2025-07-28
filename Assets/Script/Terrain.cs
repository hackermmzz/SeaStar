using UnityEngine;


public class Terrain : MonoBehaviour
{
    private Chunk chunk;
    private bool isGenerate;
    private static int[] MeshIndices = null;
    private static Material material;
    private Mesh mesh;
    private int expectLod;
    private int currentLod;
    void Start()
    {
        //初始化MeshIndices
        if (MeshIndices == null)
        {
            //加载三角索引
            MeshIndices = new int[Config.ChunkSize * Config.ChunkSize * 6];
            for (int x = 0, i = 0; x < Config.ChunkSize; ++x)
            {
                for (int z = 0; z < Config.ChunkSize; ++z, i += 6)
                {
                    MeshIndices[i + 0] = x + z * (Config.ChunkSize + 1);
                    MeshIndices[i + 1] = x + (z + 1) * (Config.ChunkSize + 1);
                    MeshIndices[i + 2] = (x + 1) + z * (Config.ChunkSize + 1);
                    MeshIndices[i + 3] = (x + 1) + z * (Config.ChunkSize + 1);
                    MeshIndices[i + 4] = x + (z + 1) * (Config.ChunkSize + 1);
                    MeshIndices[i + 5] = (x + 1) + (z + 1) * (Config.ChunkSize + 1);
                }
            }
            //加载地面材质
            material = (Material)Resources.Load(Config.TerrainMaterial, typeof(Material));
        }
        // 确保组件存在
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshCollider>();
        // 设置材质
        GetComponent<MeshRenderer>().material = material;
        //配置
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        //
        isGenerate = false;
        expectLod = currentLod = int.MaxValue;
    }
    //
    void Update()
    {
        //进行处理细节层次
        ProcessLod();
        //
    }

    //处理细节层次
    private void ProcessLod()
    {
        if (expectLod == currentLod) return;
        //如果小于当前细节，说明要进行更加细致得处理
        if (expectLod < currentLod)
        {
            LoadMoreDetail();
        }
        //否则预期大于当前细节，说明要减少部分细节
        else
        {
            UnLoadSeveralDetail();
        }
        //
        expectLod = currentLod;
    }
    //卸载部分细节
    private void UnLoadSeveralDetail()
    {
        
    }
    //加载更多细节
    private void LoadMoreDetail()
    {
        //目前就只处理前两层细节
        if (expectLod == 0)
        {
            if (currentLod != 0)
            {
                if (chunk.IsThermalErosion() == false)
                {
                    chunk.ThermalErosionProcess();
                    var vertices = GenerateVertices();
                    UpdateMesh(vertices);

                }
            }
        }
        else if (expectLod == 1)
        {

        }
        else
        {

        }
    }
    //是否已经生成
    public bool GetIsGenerate()
    {
        return isGenerate;
    }
    //设置terrain的偏移
    public void SetOffset(Vector2Int pos)
    {
        isGenerate = false;
        //生成chunk对象
        if (chunk == null)
        {
            chunk = new Chunk(pos);
        }
        //
        chunk.setOffset(pos);
    }
    //设置网格为
    private void UpdateMesh(Vector3[] vertices)
    {
        GetComponent<MeshFilter>().mesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
        mesh.triangles = null;
        mesh.vertices = null;
        mesh.vertices = vertices;
        mesh.triangles = MeshIndices;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        // 更新碰撞体
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshFilter>().mesh = mesh;
    }
    //生成地形
    public void GenerateTerrain()
    {
        //
        isGenerate = true;
        //生成基础的地形高度图
        GenerateBaseHeightMap();
        //生成三角
        var vertices=GenerateVertices();
        //更新网格
        UpdateMesh(vertices);
    }
    
    private Vector3[] GenerateVertices()
    {
        var vertices = new Vector3[(Config.ChunkSize + 1) * (Config.ChunkSize + 1)];

        for (int z = 0; z <= Config.ChunkSize; z++)
        {
            for (int x = 0; x <= Config.ChunkSize; x++)
            {
                vertices[z * (Config.ChunkSize + 1) + x] = new Vector3(x + chunk.getOffsetX(), chunk.GetHeight(x, z), z + chunk.getOffsetZ());
            }
        }
        return vertices;
    }
    //获取相对于该chunk的局部坐标
    public Vector3 GetLocalPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - new Vector3(chunk.getOffsetX(), 0.0f, chunk.getOffsetZ());
        return localPos;
    }
    //获取世界坐标系下该点高度
    public float GetTerrainHeightAtWorldPosition(Vector3 worldPosition)
    {
        // 将世界坐标转换为地形本地坐标
        Vector3 localPos = GetLocalPosition(worldPosition);
        // 确保坐标在地形范围内
        if (localPos.x < 0 || localPos.x > Config.ChunkSize || localPos.z < 0 || localPos.z > Config.ChunkSize)
        {
            Debug.Log("this is a bug");
            return worldPosition.y; // 超出范围则不修改高度
        }

        // 计算最近的四个顶点索引
        int x = Mathf.FloorToInt(localPos.x);
        int z = Mathf.FloorToInt(localPos.z);

        // 获取四个顶点的高度
        float h00 = GetVertexHeight(x, z);
        float h10 = GetVertexHeight(x + 1, z);
        float h01 = GetVertexHeight(x, z + 1);
        float h11 = GetVertexHeight(x + 1, z + 1);

        // 计算插值因子
        float fx = localPos.x - x;
        float fz = localPos.z - z;

        // 双线性插值计算精确高度
        float height = Mathf.Lerp(Mathf.Lerp(h00, h10, fx), Mathf.Lerp(h01, h11, fx), fz);

        return height;
    }

    //x z均为局部坐标
    private float GetVertexHeight(int x, int z)
    {
        return chunk.GetHeight(x, z);
    }

    void GenerateBaseHeightMap()
    {
        chunk.LoadChunk();
    }
    //设置Lod
    public void SetLod(int lod_)
    {
        expectLod = lod_;
    }
}