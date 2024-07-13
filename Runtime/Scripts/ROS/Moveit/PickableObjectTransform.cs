using Toolkit;
using UnityEngine;
using UnityEngine.EventSystems;
namespace SimToolkit.ROS.Moveit
{
/// <summary>
/// Used to allow transformation and pick place operations on pickable objects
/// </summary>
[RequireComponent(typeof(CollisionObject))]
public class PickableObjectTransform : MonoBehaviour
{
    private CollisionObject collisionObject;
    public MoveitRobotPickPlace robot;
    public string moveGroup;

    bool isHovered = false;
    bool isSelected = false;
    TransformGizmo gizmo;
    Toolbar<CollisionObject> toolbar;

    void Start()
    {
        if (robot == null) 
            robot = GameObject.FindAnyObjectByType<MoveitRobotPickPlace>();

        collisionObject = GetComponent<CollisionObject>();
        var allRenderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in allRenderers)
        {
            renderer.material = Resources.Load<Material>("SelectionLines");
            renderer.material.SetColor("_Stripe_Color", new Color(0.06f, 0.06f, 0.06f));
            renderer.material.SetFloat("_Thickness", 0.2f);
        }

        toolbar = new Toolbar<CollisionObject>();
        var pick = new ToolbarItem<CollisionObject>(Icons.GetVectorIcon("play"), "Pick", async (obj) =>
        {
            transform.GetPositionAndRotation(out var targetPosition, out var targetRotation);
            if (robot != null && robot.placePositionOverride != null)
            {
                robot.placePositionOverride.GetPositionAndRotation(out targetPosition, out targetRotation);
            }

            SetSelected(false);
            await PlanningSceneManager.UpdateFromRemote();
            Debug.Log("Picking object " + obj.ID);
            
            robot.PickAndPlace(obj, targetPosition, targetRotation);
        });

        toolbar.AddItem(pick, collisionObject);
        toolbar.Create(UIManager.rootElement);
        toolbar.Follow(transform, new Vector2(0, -100));
        _ = toolbar.Hide(0);
    }

    void SetSelected(bool selected)
    {
        if (collisionObject.IsAttached) selected = false;
        
        collisionObject.isFrozen = selected;
        isSelected = selected;
        if (selected)
        {
            _ = toolbar.Show();
            gizmo = TransformGizmo.Create(transform);
            var allRenderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in allRenderers)
            {
                renderer.material.SetColor("_Stripe_Color", new Color(0.3f, 0.5f, 0.3f));
                renderer.material.SetFloat("_Thickness", 0.5f);
            
            }
        }
        else
        {
            _ = toolbar.Hide();
            if (gizmo)
            {
                Destroy(gizmo.gameObject);

                var allRenderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in allRenderers)
                {
                    renderer.material.SetColor("_Stripe_Color", new Color(0.06f, 0.06f, 0.06f));
                    renderer.material.SetFloat("_Thickness", 0.2f);
                }             
            }

            _ = PlanningSceneManager.UpdateFromRemote();
        }
    }

    void Update()
    {
        // if object is hovered add outline
        bool changedHover = false;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                if (!isHovered)
                {
                    isHovered = true;
                    changedHover = true;
                }
            }
            else
            {
                if (isHovered)
                {
                    isHovered = false;
                    changedHover = true;
                }
            }
        }
        else
        {
            if (isHovered)
            {
                isHovered = false;
                changedHover = true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isHovered && !isSelected)
            {
                SetSelected(true);
            }
            else if (isSelected && !gizmo.IsGrabbed && !gizmo.IsHovered && !EventSystem.current.IsPointerOverGameObject())
            {
                SetSelected(false);
            }
        }

        if (changedHover && !isSelected)
        {
            var allRenderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (isHovered)
                {
                    renderer.material.SetColor("_Stripe_Color", new Color(0.15f, 0.2f, 0.4f));
                    renderer.material.SetFloat("_Thickness", 0.5f);
                }
                else
                {
                    renderer.material.SetColor("_Stripe_Color", new Color(0.06f, 0.06f, 0.06f));
                    renderer.material.SetFloat("_Thickness", 0.2f);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (gizmo)
        {
            Destroy(gizmo.gameObject);
        }

        if (toolbar != null)
        {
            toolbar.Delete();
        }
    }
}

}