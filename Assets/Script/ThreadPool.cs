using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using TaskType=pair<ThreadPoolCallBack,System.Object>;
public delegate void ThreadPoolCallBack(System.Object obj);
public class pair<T1, T2> {
    public T1 first { set; get; }
    public T2 second { set; get; }
}

public class ThreadPool
{
    
    private Mutex mutex;
    private Queue<TaskType> tasks;
    private List<Thread> threads;
    public ThreadPool(int threadCnt = 12)
    {
        //初始化互斥锁
        mutex = new Mutex();
        //初始化任务队列
        tasks = new Queue<TaskType>();
        //初始化线程数量
        threads = new List<Thread>();
        for (int i = 0; i < threadCnt; ++i)
        {
            Thread thread = new Thread(DealWithTask);
            threads.Add(thread);
            thread.Start();
        }
    }
    public void AddTask(ThreadPoolCallBack task, System.Object argument)
    {
        mutex.WaitOne();
        {
            TaskType task_ = new TaskType();
            task_.first = task;
            task_.second = argument;
            tasks.Enqueue(task_);
        }
        mutex.ReleaseMutex();
    }
    //线程将调用的方法
    void DealWithTask() {
        while (true)
        {
            bool flag = mutex.WaitOne(0);
            if (flag)
            {
                if (tasks.Any())
                {
                    TaskType task = tasks.Dequeue();
                    var fun = task.first;
                    var arg = task.second;
                    fun(arg);
                }
                //释放锁
                mutex.ReleaseMutex();
            }
        }
    }
}
