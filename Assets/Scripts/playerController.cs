using UnityEngine;

public class SimplePlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public Transform playerMesh; // Assign in Inspector
    public float rotationSpeed = 10f;

    void Update()
	{
		float x = Input.GetAxisRaw("Horizontal");
		float z = Input.GetAxisRaw("Vertical");

		Vector3 move = new Vector3(x, 0f, z).normalized;
		transform.position += move * speed * Time.deltaTime;
	
		if (move != Vector3.zero)
        {
            RotateMesh(move);
        }
    }

    void RotateMesh(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        playerMesh.rotation = Quaternion.Slerp(
            playerMesh.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

}
