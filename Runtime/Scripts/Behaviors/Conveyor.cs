using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour
{
    public MaterialScroll linkedMaterialScroll;
    public float speed = 1f;
    public Vector2 direction = Vector2.up;
    public float materialScrollAngleOffset = 0;
    public float materialScrollSpeedMultiplier = 1;

    [Range(0, 1)]
    public float grip = 0.5f;

    public List<Rigidbody> objectsOnConveyor = new List<Rigidbody>();

    // Update is called once per frame
    void Update()
    {
        objectsOnConveyor.RemoveAll(rb => rb == null);

        foreach (Rigidbody rb in objectsOnConveyor)
        {
            if (rb == null)
            {
                continue;
            }
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, transform.TransformDirection(new Vector3(direction.x, 0, direction.y) * speed), grip * (Time.deltaTime * 100f));
        }


        if (linkedMaterialScroll != null)
        {
            linkedMaterialScroll.scrollSpeed = speed * materialScrollSpeedMultiplier;
            linkedMaterialScroll.direction = Quaternion.Euler(0, 0, materialScrollAngleOffset) * direction;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out Rigidbody rb) && !objectsOnConveyor.Contains(rb))
        {
            objectsOnConveyor.Add(rb);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.TryGetComponent(out Rigidbody rb) && objectsOnConveyor.Contains(rb))
        {
            objectsOnConveyor.Remove(rb);
        }
    }

}
