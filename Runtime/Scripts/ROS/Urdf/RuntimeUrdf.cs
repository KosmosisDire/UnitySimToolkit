using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RuntimeUrdf: RosBehaviour
{
    public string robotDescription = "robot_description";
    public bool enableColliders = true;
    public bool enableVisuals = true;
    private List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();

    void Awake()
    {
        // disable all other components
        foreach (var component in GetComponents<MonoBehaviour>())
        {
            if (component is RuntimeUrdf)
                continue;

            component.enabled = false;
            disabledComponents.Add(component);
        }
    }

    public async override Task Init()
    {
        var description = await ParamServer.GetParam(robotDescription);
        var robot = await URDFBuilder.BuildRuntime(description, gameObject);
        if (!enableColliders)
        {
            robot.DisableColliders();
        }
        if (!enableVisuals)
        {
            robot.DisableVisuals();
        }

        // re-enable all other components
        foreach (var component in disabledComponents)
        {
            component.enabled = true;
        }

        Destroy(this);
    }
}