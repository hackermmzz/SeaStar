using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using SQLite4Unity3d;

public class Database
{
    //
    public SQLiteConnection connection{ get; set; }
    private List<IDbCommand> commands;//存储所有编译好的命令
    private System.Object lockObj;//辅助上锁
    //
    public Database(string path)
    {
        //
        lockObj = new System.Object();
        //
        connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
    }
    //
    public System.Object Lock()
    {
        return lockObj;
    }
    //
    ~Database()
    {
        if (connection != null)
        {
            connection.Close();
        }
    }
};