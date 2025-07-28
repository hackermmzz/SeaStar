using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameObject player;
    public float playerSpeed = 1.0f;
    public float rotateSpeed = 10.0f;
    private Animator animator;
    private Vector3 position;

    // Start is called before the first frame update
    void Start()
    {
        //
        animator = this.gameObject.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // 计算移动方向
        Vector3 moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.z += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.z -= 1;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1;
        // 处理移动和旋转
        if (moveInput != Vector3.zero)
        {
            ProcessMove(moveInput);
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            ProcessJump();
        }
        else
        {
            ProcessIdle();
        }
        //
        position = transform.position;
        //计算人物当前位置
        ResetPlayerPosition();

    }
    public void ProcessMove(Vector3 move)
    {
        // 获取相机水平方向向量
        Vector3 cameraToPlayer = player.transform.position - Config.camera.transform.position;
        Vector3 cameraForward = new Vector3(cameraToPlayer.x, 0, cameraToPlayer.z).normalized;
        // 计算基于相机的移动方向
        Vector3 moveDirection = Quaternion.LookRotation(cameraForward) * move.normalized;

        // 平滑旋转到移动方向
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        // 沿角色自身前方移动
        player.transform.Translate(Vector3.forward * playerSpeed * Time.deltaTime, Space.Self);
        animator.Play("run");
    }
    public void ProcessJump()
    {
        //animator.Play("jump");
    }
    public void ProcessIdle()
    {
        animator.Play("idle");
    }
    public void ResetPlayerPosition()
    {
        var pos = transform.position;
        transform.position = new Vector3(pos.x, Config.map.GetTerrainHeightAtWorldPosition(pos), pos.z);
    }
}
