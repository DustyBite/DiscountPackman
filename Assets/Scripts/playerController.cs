using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public Transform playerMesh;
    public float rotationSpeed = 10f;

    [Header("Personality")]
    public float scary = 4f;
    public float bravey = 5f;

    void Update()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;

        Vector3 move = new Vector3(x, 0f, z).normalized;

        // Move
        transform.position += move * speed * Time.deltaTime;

        // Rotate mesh toward movement
        if (move != Vector3.zero && playerMesh != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            playerMesh.rotation = Quaternion.Slerp(playerMesh.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}
