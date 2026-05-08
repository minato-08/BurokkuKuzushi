using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody rb;

    void Start()
    {
        // コンポーネント「Rigidbody」を取得して、変数に代入
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float move = 0f;
        
        // Unity6から、InputSystemを使用する！！
        if (Keyboard.current != null)
        {
         //左を押されたとき、
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                move = -1f;
    　　　//右を押されたとき、
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                move = 1f;
        }

        // Rigidbodyの速度（velocity）を書き換えて移動させる
        rb.velocity = new Vector3(move * speed, 0f,0f);
    }
}