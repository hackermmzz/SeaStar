using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Threading;
using System;
using TMPro;
using UnityEditor.Rendering;
public class CameraController : MonoBehaviour
{
    public GameObject player;
    public Camera camera;
    public float cameraMinDis = 1.0f;
    public float cameraMaxDis = 5.0f;
    public float cameraToPlayerDis = 5.0f;
    public float scrollSensitivity = 1.0f;

    public Vector3 center = new Vector3(0.0f, 0.8f, 0.0f);
    public float cameraEluerX = 0.0f;
    public float cameraEluerY = 0.0f;
    public float cameraEluerYMin = -90.0f;
    public float cameraEluerYMax = 90.0f;
    public float rotateSensitivity = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // Update is called once per frame
    void LateUpdate()
    {
        //判断是否隐藏鼠标
        if (Cursor.visible == true && Input.GetMouseButtonDown(0))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        //只有鼠标隐藏的时候才可以操作摄像机
        if (Cursor.visible == true)return;
        //设置视角远近
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cameraToPlayerDis = Math.Clamp(cameraToPlayerDis - scroll * scrollSensitivity, cameraMinDis, cameraMaxDis);
        //设置相机旋转
        Vector3 centerPos = player.transform.TransformPoint(center);
        float deltaX = Input.GetAxis("Mouse X") * rotateSensitivity;
        float deltaY = -Input.GetAxis("Mouse Y") * rotateSensitivity;
        cameraEluerX = (cameraEluerX+deltaX)%360.0f;
        cameraEluerY = Math.Clamp(cameraEluerY + deltaY, cameraEluerYMin, cameraEluerYMax);
        Quaternion rotation = Quaternion.Euler(cameraEluerY, cameraEluerX, 0.0f);
        Vector3 newpos = centerPos - (rotation * Vector3.forward * cameraToPlayerDis);
        camera.transform.position = newpos;
        camera.transform.LookAt(centerPos);

        //根据碰撞动态生成摄像机位置
        RaycastHit hit;
        if (Physics.Linecast(centerPos, camera.transform.position, out hit))
        {
            float dis = Mathf.Clamp(hit.distance, cameraMinDis, cameraMaxDis);
            Vector3 dir = (camera.transform.position - centerPos).normalized;
            camera.transform.position = centerPos + dir * dis;
        }
        //控制鼠标可见性
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
    }
}
