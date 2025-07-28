using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEditor.PackageManager;
using UnityEngine;
using System.IO.Compression;
using System.Data;
using SQLite4Unity3d;
using System.Linq;
//////////////////////////////////////////////////////////////////////

[Table("ChunkData")]
public class ChunkData
{
    
    [Column("posx"),Indexed(Name = "PK_ChunkData", Order = 1, Unique = true)]
    public int posx { get; set; }
    
    [Column("posz"),Indexed(Name = "PK_ChunkData", Order = 1, Unique = true)]
    public int posz { get; set; }
    
    [Column("data")]
    public byte[] data { get; set; }
}//////////////////////////////////////////////////////////////////
public class Chunk
{
    private int blockX;//起始坐标x
    private int blockZ;//起始坐标z
    private float[] heightData;//高度数据
    private bool BaseHightFlag;//是否已经生成了基础地形高度
    private bool ThermalErosionFlag;//是否进行了热力腐蚀
    private bool WaterErosionFlag;//是否进行了流水腐蚀
    public Chunk(Vector2Int pos)
    {
        //
        setOffset(pos);
        heightData = new float[(Config.ChunkSize + 1) * (Config.ChunkSize + 1)];
    }
    //获取指定索引的高度
    public float GetHeight(int x, int z)
    {
        return heightData[x * (Config.ChunkSize + 1) + z];
    }
    public void SetHeight(int x, int z, float h)
    {
        heightData[x * (Config.ChunkSize + 1) + z] = h;
    }
    //加载指定的chunk
    public void LoadChunk()
    {
        //如果数据库存在,那么直接从数据库读取
        if (IsExistInDataBase())
        {
            LoadTerrainFromDataBase();
            //从数据据库加载肯定是经过所有pass的
            SetAllFlag();
        }
        //否则新生成
        else
        {
            LoadByComputeShader();
            StoreIntoDatabase();
        }
    }
    //数据库是否存在这个区块
    private bool IsExistInDataBase()
    {
        bool flag;
        lock (Config.ChunkExist)
        {
            flag = Config.ChunkExist.Contains(new Vector2Int(blockX, blockZ));
        }
        return flag;
    }
    //从数据库加载这个区块
    private void LoadTerrainFromDataBase()
    {
        var conn = Config.database.connection;
        var query = conn.Query<ChunkData>($"select data from ChunkData where posx={blockX} and posz={blockZ}").ElementAt(0);
        DecompressHeightByGzip(query.data);
    }
    //着色器生成该区块
    private void LoadByComputeShader()
    {
        GenerateTerrainHeight();
        //ThermalErosionProcess();//默认不生成，需要指定生成
        //
    }
    //生成基础地形高度
    private void GenerateTerrainHeight()
    {
        var computeShader = Config.terrainHeightGenerate;
        var kernelHandle = Config.terrainHeightGenerateKernelHandle;
        computeShader.SetBuffer(kernelHandle, "HeightBuffer", Config.terrainComputeBuffer);
        computeShader.SetInt("_ChunkSize", Config.ChunkSize);
        computeShader.SetFloat("_Frequency", Config.TerrainFrequency);
        computeShader.SetFloat("_HeightScale", Config.TerrainHeightScale);
        computeShader.SetVector("_Offset", new Vector2(blockX, blockZ));

        // 计算线程组数量
        int threadGroupsX = Mathf.CeilToInt((Config.ChunkSize + 1) / 16f);
        int threadGroupsZ = Mathf.CeilToInt((Config.ChunkSize + 1) / 16f);

        // 执行Compute Shader
        computeShader.Dispatch(kernelHandle, threadGroupsX, 1, threadGroupsZ);
        //保存初始高度图
        Config.terrainComputeBuffer.GetData(heightData);
    }
    //热力侵蚀
    public void ThermalErosionProcess()
    {
        //
        var computeShader = Config.terrainThermalErosion;
        var kernelHandle = Config.terrainThermalErosionHandle;
        ComputeBuffer height = Config.terrainComputeBuffer;
        var store = Config.ThermalErosionBuffer;
        //设置初始高度数据
        height.SetData(heightData);
        // 计算线程组数量
        int threadGroupsX = Mathf.CeilToInt((Config.ChunkSize + 1) / 16f);
        int threadGroupsZ = Mathf.CeilToInt((Config.ChunkSize + 1) / 16f);
        for (int i = 0; i < Config.ThermalErosionLoopCnt; ++i)
        {
            computeShader.SetBuffer(kernelHandle, "height", height);
            computeShader.SetBuffer(kernelHandle, "store", store);
            //
            computeShader.SetInt("_ChunkSize", Config.ChunkSize);
            computeShader.SetFloat("tanTheta", Config.ThermalErosionTanTheta);
            computeShader.SetFloat("factor0", Config.ThermalErosionFactor0);
            computeShader.SetFloat("factor1", Config.ThermalErosionFactor1);

            // 执行Compute Shader
            computeShader.Dispatch(kernelHandle, threadGroupsX, 1, threadGroupsZ);
            //
            var tmp = height;
            height = store;
            store = tmp;
        }
        //保存腐蚀过的高度图
        store.GetData(heightData);
        //
        ThermalErosionFlag = true;
    }
    public void setOffset(Vector2Int pos)
    {
        blockX = pos.x;
        blockZ = pos.y;
        //一旦重置高度,那么所有flag都是flase
        ResetAllFlag();
    }
    //将所有flag设为true
    private void SetAllFlag()
    {
        BaseHightFlag = true;
        ThermalErosionFlag = true;
        WaterErosionFlag = true;
    }
    //将所有flag设为false
    private void ResetAllFlag()
    {
        BaseHightFlag = false;
        ThermalErosionFlag = false;
        WaterErosionFlag = false;
    }
    //
    public int getOffsetX()
    {
        return blockX;
    }
    public int getOffsetZ()
    {
        return blockZ;
    }
    public bool IsThermalErosion()
    {
        return ThermalErosionFlag;
    }
    //把侵蚀后的数据放入数据库
    private void StoreIntoDatabase()
    {
        var db = Config.database;
        var data = CompressHeightByGzip();
        //
        var conn = Config.database.connection;
        var info = new ChunkData();
        info.posx = blockX;
        info.posz = blockZ;
        info.data = data;
        //送到写队列
        lock (Config.database.Lock())
        {
            Config.ChunkDataWriteToDataBase.Enqueue(info);
        }
    }

    //压缩高度数据
    private byte[] CompressHeightByGzip()
    {
        var data = new byte[sizeof(float) / sizeof(byte) * heightData.Length];
        for (int i = 0, j = 0; i < heightData.Length; i++)
        {
            float value = heightData[i];
            byte[] bytes = System.BitConverter.GetBytes(value);
            foreach(var v in bytes) {
                data[j++] = v;
            }
        }
        using (var ms = new System.IO.MemoryStream())
        {
            using (var gzs = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }
    }
    //解压缩数据
    private void DecompressHeightByGzip(byte[] compressedData)
    {
        using (var ms = new System.IO.MemoryStream(compressedData))
        using (var gzs = new GZipStream(ms, CompressionMode.Decompress))
        using (var outStream = new System.IO.MemoryStream())
        {
            gzs.CopyTo(outStream);
            var bytes = outStream.ToArray();
            //
            for (int i = 0,j=0; i < bytes.Length; i += 4,++j)
            {
                heightData[j]=System.BitConverter.ToSingle(bytes, i);
            }
            //
        }
    }
}
