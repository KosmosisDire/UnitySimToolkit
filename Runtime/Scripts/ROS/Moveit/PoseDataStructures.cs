using System;
using RosMessageTypes.Moveit;
using RosMessageTypes.Sensor;
using UnityEngine;
namespace SimToolkit.ROS.Moveit
{
public enum Planner
{
    ABITstar,
    AITstar,
    AnytimePathShortening,
    BFMT,
    BITstar,
    BKPIECE,
    BiEST,
    BiTRRT,
    EST,
    FMT,
    KPIECE,
    LBKPIECE,
    LBTRRT,
    LazyPRM,
    LazyPRMstar,
    PDST,
    PRMstar,
    ProjEST,
    RRTConnect,
    RRT,
    RRTstar,
    SBL,
    SPARS,
    SPARStwo,
    STRIDE,
    TRRT,
}

public enum TrajectoryStatus : byte
{
    Pending = 0,
    Active = 1,
    Preempted = 2,
    Succeeded = 3,
    Aborted = 4,
    Rejected = 5,
    Preempting = 6,
    Recalling = 7,
    Recalled = 8,
    Lost = 9,
    None = 255
}

[Serializable]
public class MoveitPlannerOptions
{
    public Planner planner = Planner.RRTConnect;
    public string PlannerID => planner.ToString();
    public string planRequestTopic = "/move_group/motion_plan_request";
    public string executeRequestTopic = "/move_group/goal";
    public string statusTopic = "/move_group/status";
    public string worldFrame;
    public float planningTime = 5f;
    public int attempts = 10;
    public bool replan = false;
    public int replanAttempts = 5;
    public float replanDelay = 0.5f;
    public float maxVelocityScaling = 1f;
    public float maxAccelerationScaling = 1f;

    public PlanningOptionsMsg ToPlanningOptionsMsg()
    {
        return new PlanningOptionsMsg
        {
            replan = replan,
            replan_attempts = replanAttempts,
            replan_delay = replanDelay,
        };
    }
}

[Serializable]
public class MoveitIKOptions
{
    public string computeIkService = "/compute_ik";
    public float planningTime = 0.1f;
    public int attempts = 10;
    public bool avoidCollisions = true;
}
}