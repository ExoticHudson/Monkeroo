using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class HorrorAIPlus : MonoBehaviourPunCallbacks, IPunObservable
{
// ==================== GENERAL SETTINGS ====================
[HideInInspector] public NavMeshAgent agent;
[HideInInspector] public string playerTag = "";

// ==================== AI MOVEMENT SETTINGS ====================
public enum MovementType { Waypoints, Random }

[HideInInspector] public MovementType movementType = MovementType.Waypoints;
[HideInInspector] public float WanderSpeed = 4f;
[HideInInspector] public float ChasingSpeed = 8f;

// ==================== WAYPOINT SETTINGS ====================
[HideInInspector] public List<Transform> waypoints = new List<Transform>();
[HideInInspector] public float minWaypointDistance = 5f;
[HideInInspector] public float maxWaypointDistance = 100f;

// ==================== RANDOM SETTINGS ====================
[HideInInspector] public float minRandomDistance = 10f;
[HideInInspector] public float maxRandomDistance = 30f;

// ==================== SMART AI SETTINGS ====================
[HideInInspector] public float positionHistoryDuration = 15f;
[HideInInspector] public float minDistanceFromRecentPositions = 8f;
[HideInInspector] public int maxRecentPositions = 5;
[HideInInspector] public float waypointCooldownTime = 30f;

// ==================== DETECTION SYSTEM ====================
public enum DetectionSystemType { Basic, Normal, WeepingAngel, Blind }
public enum DetectionShape { Cone, Box, Line }

[HideInInspector] public DetectionSystemType detectionSystem = DetectionSystemType.Basic;

// ==================== HEARING SETTINGS ====================
[HideInInspector] public bool enableHearing = true;
[HideInInspector] public float hearingRunRadius = 15f;
[HideInInspector] public float hearingWalkRadius = 30f;
[HideInInspector] public LayerMask hearingBlockLayers = 0;

// ==================== BASIC DETECTION SETTINGS ====================
[HideInInspector] public float basicDetectionRadius = 10f;
[HideInInspector] public bool detectThroughWalls = false;
[HideInInspector] public LayerMask basicWallLayers = ~0;

// ==================== NORMAL DETECTION SETTINGS ====================
[HideInInspector] public DetectionShape detectionShape = DetectionShape.Cone;
[HideInInspector] public float viewDistance = 15f;
[HideInInspector] public float fovAngle = 90f;
[HideInInspector] public Vector3 detectionOffset = Vector3.zero;
[HideInInspector] public Vector3 detectionRotationOffset = Vector3.zero;
[HideInInspector] public LayerMask wallLayers = ~0;
[HideInInspector] public float chaseDuration = 5f;

// ==================== CHASE GIVE UP SETTINGS ====================
[HideInInspector] public float chaseGiveUpTime = 15f;

// ==================== BOX DETECTION SETTINGS ====================
[HideInInspector] public Vector3 boxDetectionSize = new Vector3(5f, 3f, 10f);

// ==================== LINE DETECTION SETTINGS ====================
[HideInInspector] public float lineFovAngle = 60f;
[HideInInspector] public float lineViewDistance = 20f;

// ==================== WEEPING ANGEL SETTINGS ====================
[HideInInspector] public bool weepingAngelNetworked = true;
[HideInInspector] public float weepingAngelCameraCheckRadius = 50f;
[HideInInspector] public float weepingAngelPlayerDetectionRadius = 30f;
[HideInInspector] public LayerMask playerVisibilityLayers = ~0;
[HideInInspector] public Camera nonNetworkedCamera;

// ==================== NETWORK SOUNDS ====================
[HideInInspector] public bool networkSounds = true;

// ==================== FOOTSTEP SOUNDS ====================
[HideInInspector] public bool enableFootsteps = false;
[HideInInspector] public AudioSource footstepAudioSource;
[HideInInspector] public List<AudioClip> footstepSounds = new List<AudioClip>();
[HideInInspector] public float wanderFootstepInterval = 0.5f;
[HideInInspector] public float chaseFootstepInterval = 0.3f;
[HideInInspector] public float minSpeedForFootsteps = 0.5f;

// ==================== GROWL SOUNDS ====================
[HideInInspector] public bool enableGrowls = false;
[HideInInspector] public AudioSource growlAudioSource;
[HideInInspector] public List<AudioClip> growlSounds = new List<AudioClip>();
[HideInInspector] public float minGrowlInterval = 3f;
[HideInInspector] public float maxGrowlInterval = 5f;
[HideInInspector] public bool growlOnlyWhenWandering = false;

// ==================== DETECTION SOUNDS ====================
[HideInInspector] public bool enableDetectionSound = false;
[HideInInspector] public AudioSource detectionAudioSource;
[HideInInspector] public List<AudioClip> detectionSounds = new List<AudioClip>();
[HideInInspector] public float detectionSoundCooldown = 10f;

// ==================== CHASE SOUNDS ====================
[HideInInspector] public bool enableChaseLoop = false;
[HideInInspector] public AudioSource chaseLoopAudioSource;
[HideInInspector] public AudioClip chaseLoopSound;
[HideInInspector] public float chaseFadeInSpeed = 2f;
[HideInInspector] public float chaseFadeOutSpeed = 2f;

// ==================== DEBUG SETTINGS ====================
[HideInInspector] public bool showDebugInfo = true;
[HideInInspector] public bool showGizmos = true;
[HideInInspector] public bool showDetectionGizmos = true;
[HideInInspector] public bool showHearingGizmos = true;
[HideInInspector] public bool showMovementGizmos = true;
[HideInInspector] public bool showPathGizmos = true;
[HideInInspector] public bool showWaypointGizmos = true;
[HideInInspector] public bool showRecentPositionsGizmos = true;

// ==================== GIZMO COLORS ====================
[HideInInspector] public Color pathColor = Color.white;
[HideInInspector] public Color recentPositionsColor = Color.magenta;

[HideInInspector] public Color waypointValidColor = Color.green;
[HideInInspector] public Color waypointInvalidColor = Color.red;
[HideInInspector] public Color waypointCooldownColor = Color.gray;

[HideInInspector] public Color randomDistanceColor = Color.cyan;

[HideInInspector] public Color hearingRunColor = Color.yellow;
[HideInInspector] public Color hearingWalkColor = Color.green;

[HideInInspector] public Color basicDetectionColor = Color.red; 
[HideInInspector] public Color fovBoundaryColor = Color.blue;
[HideInInspector] public Color wallObstructionColor = Color.red; 
[HideInInspector] public Color lineTargetColor = Color.green;

[HideInInspector] public Color weepingAngelCameraZoneColor = Color.yellow;
[HideInInspector] public Color weepingAngelPlayerZoneColor = Color.magenta;
[HideInInspector] public Color beingWatchedColor = Color.red;


// ==================== RUNTIME VARIABLES ====================
[HideInInspector] public Transform currentTarget;
[HideInInspector] public float chaseTimer;
[HideInInspector] public int currentWaypointIndex = -1;
[HideInInspector] public bool isChasing = false;
[HideInInspector] public bool isInvestigating = false;
[HideInInspector] public bool isBeingWatched = false;
[HideInInspector] public Vector3 currentDestination;
[HideInInspector] public float currentSpeed;

// ==================== PRIVATE VARIABLES ====================
private float footstepTimer;
private float growlTimer;
private List<Vector3> recentPositions = new List<Vector3>();
private float lastPositionRecordTime;
private Transform lastDetectedTarget;
private float lastDetectionSoundTime;
private Dictionary<int, float> waypointCooldowns = new Dictionary<int, float>();
private Vector3 lastKnownTargetPosition;
private int stuckCheckCounter = 0;
private Vector3 lastPosition;
private float stuckCheckTimer = 0f;
private float currentChaseVolume = 0f;
private bool isManuallyStoppedMovement = false;
private List<Transform> watchingPlayers = new List<Transform>();
private List<Camera> playerCameras = new List<Camera>();
private Transform lineDetectedTarget;
private PhotonView pv;
private bool networkChaseLoopPlaying = false;

// ==================== CHASE GIVE UP TRACKING ====================
private float currentChaseDurationTimer = 0f; 
private int pathRetryCount = 0;
private int maxPathRetries = 10;
private float pathRetryTimer = 0f;
private float pathRetryInterval = 0.5f;
private bool isFleeing = false;
private Vector3 fleeDirection = Vector3.zero;
private float fleeTimer = 0f;
private float maxFleeDistance = 100f;

[HideInInspector] public int currentEditorTab = 0;

#region Unity

private void Awake()
{
    pv = GetComponent<PhotonView>();
    if (pv == null)
    {
        pv = gameObject.AddComponent<PhotonView>();
    }
}

private void Start()
{
    InitializeAI();
}

private void Update()
{
    if (isManuallyStoppedMovement)
        return;

    if (detectionSystem == DetectionSystemType.WeepingAngel)
    {
        if (weepingAngelNetworked)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                RunAI();
            }
        }
        else
        {
            RunAI();
        }
    }
    else if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
    {
        RunAI();
    }
    
    HandleChaseLoopSoundLocal();
}

private void OnDrawGizmos()
{
    if (!showGizmos) return;
    DrawAllGizmos();
}

private void OnDrawGizmosSelected()
{
    if (!showGizmos) return;
    DrawSelectedGizmos();
}

#endregion

#region Photon

public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (stream.IsWriting)
    {
        stream.SendNext(isChasing);
        stream.SendNext(isInvestigating);
        stream.SendNext(currentSpeed);
        stream.SendNext(networkChaseLoopPlaying);
    }
    else
    {
        isChasing = (bool)stream.ReceiveNext();
        isInvestigating = (bool)stream.ReceiveNext();
        currentSpeed = (float)stream.ReceiveNext();
        networkChaseLoopPlaying = (bool)stream.ReceiveNext();
    }
}

#endregion

#region Initialization

void InitializeAI()
{
    if (agent == null)
        agent = GetComponent<NavMeshAgent>();

    if (agent != null)
    {
        agent.enabled = true;
        agent.speed = WanderSpeed;
    }

    footstepTimer = wanderFootstepInterval;
    growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);

    lastPosition = transform.position;

    if (detectionSystem == DetectionSystemType.WeepingAngel && weepingAngelNetworked)
    {
        RefreshPlayerCameras();
    }

    Wander();
}

void RefreshPlayerCameras()
{
    playerCameras.Clear();

    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
        if (obj.name == "Player(Clone)")
        {
            Camera[] cameras = obj.GetComponentsInChildren<Camera>();
            playerCameras.AddRange(cameras);
        }
    }
}

#endregion

#region AI

void RunAI()
{
    if (agent == null) return;

    if (!agent.enabled) agent.enabled = true;
    
    currentSpeed = agent.velocity.magnitude;

    HandleFootstepSounds();
    HandleGrowlSounds();
    HandleChaseLoopSound();

    bool visualContactMade = false;

    if (isFleeing)
    {
        fleeTimer += Time.deltaTime;
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            FleeToRandomPoint();
        }
        return;
    }

    switch (detectionSystem)
    {
        case DetectionSystemType.Basic:
            visualContactMade = RunBasicDetection();
            break;
            
        case DetectionSystemType.Normal:
            visualContactMade = RunNormalDetection();
            break;
            
        case DetectionSystemType.Blind:
            visualContactMade = false;
            break;
            
        case DetectionSystemType.WeepingAngel:
            RunWeepingAngelDetection();
            return;
    }

    if (!visualContactMade && (enableHearing || detectionSystem == DetectionSystemType.Blind))
    {
        HandleHearingZones();
    }

    if (!isChasing && !isInvestigating && !isFleeing)
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            agent.speed = WanderSpeed;
            Wander();
        }
    }

    HandleChaseGiveUp();
    RecordPositionHistory();
    HandleStuckDetection();
    UpdateWaypointCooldowns();
}

#endregion

#region Chase Give Up

void HandleChaseGiveUp()
{
    if (!isChasing || currentTarget == null)
    {
        currentChaseDurationTimer = 0f;
        pathRetryCount = 0;
        isFleeing = false;
        return;
    }

    if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
    {
        pathRetryTimer -= Time.deltaTime;
        
        if (pathRetryTimer <= 0f)
        {
            pathRetryCount++;
            pathRetryTimer = pathRetryInterval;
            
            if (pathRetryCount < maxPathRetries)
            {
                Vector3 alternativeTarget = FindAlternativePathPoint(currentTarget.position, pathRetryCount);
                if (alternativeTarget != Vector3.zero)
                {
                    agent.SetDestination(alternativeTarget);
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                GiveUpChaseAndFlee("Path Blocked - Too Many Retries");
                return;
            }
        }
        return;
    }
    else
    {
        pathRetryCount = 0;
        pathRetryTimer = 0f;
    }

    currentChaseDurationTimer += Time.deltaTime;
}

Vector3 FindAlternativePathPoint(Vector3 targetPosition, int attemptNumber)
{
    int attempts = 12;
    float searchRadius = 20f;
    
    float angleOffset = attemptNumber * 30f;
    
    for (int i = 0; i < attempts; i++)
    {
        float angle = (float)i / attempts * 360f + angleOffset;
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * searchRadius;
        Vector3 testPoint = targetPosition + offset;
        
        if (NavMesh.SamplePosition(testPoint, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            NavMeshPath testPath = new NavMeshPath();
            if (agent.CalculatePath(hit.position, testPath) && testPath.status == NavMeshPathStatus.PathComplete)
            {
                return hit.position;
            }
        }
    }
    
    return Vector3.zero;
}

void GiveUpChaseAndFlee(string reason = "")
{
    currentTarget = null;
    isChasing = false;
    chaseTimer = 0f;
    currentChaseDurationTimer = 0f;
    pathRetryCount = 0;
    
    if (agent != null)
    {
        agent.speed = ChasingSpeed;
    }
    
    isFleeing = true;
    fleeTimer = 0f;
    
    Vector3 targetPos = (lastKnownTargetPosition != Vector3.zero) ? lastKnownTargetPosition : currentDestination;
    fleeDirection = (transform.position - targetPos).normalized;
    if (fleeDirection == Vector3.zero) fleeDirection = transform.forward;
    
    FleeToRandomPoint();
}

void FleeToRandomPoint()
{
    Vector3 fleeTarget = transform.position + fleeDirection * maxFleeDistance;
    
    if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, maxFleeDistance, NavMesh.AllAreas))
    {
        agent.SetDestination(hit.position);
        currentDestination = hit.position;
    }
    else
    {
        fleeTarget = transform.position + fleeDirection * 30f;
        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit2, 30f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit2.position);
            currentDestination = hit2.position;
        }
        else
        {
            isFleeing = false;
            Wander();
        }
    }
}

#endregion

#region Blind Detection / Hearing Zones

void HandleHearingZones()
{
    if (isChasing && detectionSystem != DetectionSystemType.Blind) return;

    GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(playerTag);
    Transform closestHearingTarget = null;
    float closestDist = float.MaxValue;

    foreach (GameObject target in potentialTargets)
    {
        if (target == null) continue;
        float dist = Vector3.Distance(transform.position, target.transform.position);
        
        if (dist < closestDist)
        {
            closestDist = dist;
            closestHearingTarget = target.transform;
        }
    }

    bool isInHearingRange = false;

    if (closestHearingTarget != null)
    {
        bool canHear = true;
        if (hearingBlockLayers != 0)
        {
            Vector3 dir = closestHearingTarget.position - transform.position;
            if (Physics.Raycast(transform.position, dir.normalized, dir.magnitude, hearingBlockLayers))
            {
                canHear = false;
            }
        }

        if (canHear)
        {
            if (closestDist <= hearingRunRadius)
            {
                agent.SetDestination(closestHearingTarget.position);
                agent.speed = ChasingSpeed;
                isInvestigating = true;
                isInHearingRange = true;
            }
            else if (closestDist <= hearingWalkRadius)
            {
                agent.SetDestination(closestHearingTarget.position);
                agent.speed = WanderSpeed;
                isInvestigating = true;
                isInHearingRange = true;
            }
        }
    }

    if (!isInHearingRange && isInvestigating)
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            isInvestigating = false;
            agent.speed = WanderSpeed;
            Wander();
        }
    }
}

#endregion

#region Basic Detection

bool RunBasicDetection()
{
    if (DetectTargetBasic())
    {
        agent.speed = ChasingSpeed;
        chaseTimer = chaseDuration;
        isChasing = true;
        isInvestigating = false;
        lastKnownTargetPosition = currentTarget.position;
        return true;
    }
    else if (currentTarget != null && chaseTimer > 0)
    {
        chaseTimer -= Time.deltaTime;
        Chase(currentTarget);
        isChasing = true;
        return true;
    }
    else
    {
        if (isChasing)
        {
            currentTarget = null;
            isChasing = false;
            agent.speed = WanderSpeed;
            currentChaseDurationTimer = 0f;
        }
        return false;
    }
}

bool DetectTargetBasic()
{
    bool targetDetected = false;
    Vector3 detectionOrigin = transform.position;

    GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(playerTag);

    foreach (GameObject targetObj in potentialTargets)
    {
        if (targetObj == null) continue;

        Vector3 targetPosition = targetObj.transform.position;
        float distanceToTarget = Vector3.Distance(detectionOrigin, targetPosition);

        if (distanceToTarget <= basicDetectionRadius)
        {
            bool canDetect = true;

            if (!detectThroughWalls)
            {
                Vector3 direction = targetPosition - detectionOrigin;
                if (Physics.Raycast(detectionOrigin, direction.normalized, out RaycastHit hit, distanceToTarget, basicWallLayers))
                {
                    if (!hit.collider.CompareTag(playerTag))
                    {
                        canDetect = false;
                    }
                }
            }

            if (canDetect)
            {
                PlayDetectionSound(targetObj.transform);
                lastDetectedTarget = targetObj.transform;
                currentTarget = targetObj.transform;
                Chase(currentTarget);
                targetDetected = true;
                break;
            }
        }
    }

    return targetDetected;
}

#endregion

#region Normal Detection

bool RunNormalDetection()
{
    if (DetectTargetNormal())
    {
        agent.speed = ChasingSpeed;
        chaseTimer = chaseDuration;
        isChasing = true;
        isInvestigating = false;
        lastKnownTargetPosition = currentTarget.position;
        return true;
    }
    else if (currentTarget != null && chaseTimer > 0)
    {
        chaseTimer -= Time.deltaTime;
        Chase(currentTarget);
        isChasing = true;
        return true;
    }
    else
    {
        if (isChasing)
        {
            currentTarget = null;
            isChasing = false;
            agent.speed = WanderSpeed;
            currentChaseDurationTimer = 0f;
        }
        return false;
    }
}

bool DetectTargetNormal()
{
    bool targetDetected = false;
    Vector3 detectionOrigin = GetDetectionOrigin();
    Quaternion detectionRotation = GetDetectionRotation();

    lineDetectedTarget = null;

    switch (detectionShape)
    {
        case DetectionShape.Cone:
            targetDetected = DetectWithCone(detectionOrigin, detectionRotation);
            break;
        case DetectionShape.Box:
            targetDetected = DetectWithBox(detectionOrigin, detectionRotation);
            break;
        case DetectionShape.Line:
            targetDetected = DetectWithLine(detectionOrigin, detectionRotation);
            break;
    }

    return targetDetected;
}

bool DetectWithCone(Vector3 origin, Quaternion rotation)
{
    GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(playerTag);

    foreach (GameObject targetObj in potentialTargets)
    {
        if (targetObj == null) continue;

        Vector3 targetPosition = targetObj.transform.position;
        float distanceToTarget = Vector3.Distance(origin, targetPosition);

        if (IsInCone(origin, rotation, targetPosition, distanceToTarget))
        {
            if (HasLineOfSight(origin, targetPosition))
            {
                PlayDetectionSound(targetObj.transform);
                lastDetectedTarget = targetObj.transform;
                currentTarget = targetObj.transform;
                Chase(currentTarget);
                return true;
            }
        }
    }
    return false;
}

bool DetectWithBox(Vector3 origin, Quaternion rotation)
{
    GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(playerTag);

    foreach (GameObject targetObj in potentialTargets)
    {
        if (targetObj == null) continue;
        Vector3 targetPosition = targetObj.transform.position;

        if (IsInBox(origin, rotation, targetPosition))
        {
            if (HasLineOfSight(origin, targetPosition))
            {
                PlayDetectionSound(targetObj.transform);
                lastDetectedTarget = targetObj.transform;
                currentTarget = targetObj.transform;
                Chase(currentTarget);
                return true;
            }
        }
    }
    return false;
}

bool DetectWithLine(Vector3 origin, Quaternion rotation)
{
    GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(playerTag);
    foreach (GameObject targetObj in potentialTargets)
    {
        if (targetObj == null) continue;
        Vector3 targetPosition = targetObj.transform.position;
        float distanceToTarget = Vector3.Distance(origin, targetPosition);

        if (distanceToTarget > lineViewDistance) continue;

        Vector3 forward = rotation * Vector3.forward;
        Vector3 directionToTarget = (targetPosition - origin).normalized;
        float angle = Vector3.Angle(forward, directionToTarget);

        if (angle <= lineFovAngle / 2f)
        {
            if (HasLineOfSight(origin, targetPosition))
            {
                PlayDetectionSound(targetObj.transform);
                lastDetectedTarget = targetObj.transform;
                currentTarget = targetObj.transform;
                lineDetectedTarget = targetObj.transform;
                Chase(currentTarget);
                return true;
            }
        }
    }
    return false;
}

Vector3 GetDetectionOrigin() { return transform.position + transform.TransformDirection(detectionOffset); }
Quaternion GetDetectionRotation() { return transform.rotation * Quaternion.Euler(detectionRotationOffset); }

bool IsInCone(Vector3 origin, Quaternion rotation, Vector3 targetPosition, float distance)
{
    if (distance > viewDistance) return false;
    Vector3 forward = rotation * Vector3.forward;
    Vector3 directionToTarget = (targetPosition - origin).normalized;
    float angle = Vector3.Angle(forward, directionToTarget);
    return angle <= fovAngle / 2f;
}

bool IsInBox(Vector3 origin, Quaternion rotation, Vector3 targetPosition)
{
    Vector3 localTarget = Quaternion.Inverse(rotation) * (targetPosition - origin);
    Vector3 halfSize = boxDetectionSize / 2f;
    return Mathf.Abs(localTarget.x) <= halfSize.x &&
           Mathf.Abs(localTarget.y) <= halfSize.y &&
           localTarget.z >= 0 && localTarget.z <= boxDetectionSize.z;
}

bool HasLineOfSight(Vector3 origin, Vector3 targetPosition)
{
    Vector3 direction = targetPosition - origin;
    if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, wallLayers))
    {
        if (hit.collider.CompareTag(playerTag)) return true;
        return false;
    }
    return true;
}

#endregion

#region Weeping Angel Detection

void RunWeepingAngelDetection()
{
    isBeingWatched = CheckIfBeingWatched();

    if (isBeingWatched)
    {
        agent.velocity = Vector3.zero;
        agent.isStopped = true;
    }
    else
    {
        agent.isStopped = false;
        agent.speed = ChasingSpeed;

        Transform nearestPlayer = FindNearestPlayerInRange();
        if (nearestPlayer != null)
        {
            agent.SetDestination(nearestPlayer.position);
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            agent.speed = WanderSpeed;
            Wander();
        }
    }
}

bool CheckIfBeingWatched()
{
    watchingPlayers.Clear();
    if (weepingAngelNetworked)
    {
        if (Time.frameCount % 60 == 0) RefreshPlayerCameras();
        foreach (Camera cam in playerCameras)
        {
            if (cam == null) continue;
            if (IsCameraLookingAtMe(cam)) return true;
        }
    }
    else
    {
        if (nonNetworkedCamera != null) return IsCameraLookingAtMe(nonNetworkedCamera);
    }
    return false;
}

bool IsCameraLookingAtMe(Camera cam)
{
    float distanceToCamera = Vector3.Distance(transform.position, cam.transform.position);
    if (distanceToCamera > weepingAngelCameraCheckRadius) return false;

    Vector3 cameraPosition = cam.transform.position;
    Vector3 cameraForward = cam.transform.forward;
    Vector3 directionToAI = (transform.position - cameraPosition).normalized;
    float angle = Vector3.Angle(cameraForward, directionToAI);
    float halfFOV = cam.fieldOfView / 2f;

    if (angle < halfFOV)
    {
        if (Physics.Raycast(cameraPosition, directionToAI, out RaycastHit hit, distanceToCamera, playerVisibilityLayers))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return true;
        }
        else return true;
    }
    return false;
}

Transform FindNearestPlayerInRange()
{
    GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
    Transform nearest = null;
    float nearestDistance = float.MaxValue;

    foreach (GameObject playerObj in players)
    {
        if (playerObj == null) continue;
        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
        if (distance <= weepingAngelPlayerDetectionRadius && distance < nearestDistance)
        {
            nearestDistance = distance;
            nearest = playerObj.transform;
        }
    }
    return nearest;
}

#endregion

#region Networked Sound

void HandleFootstepSounds()
{
    if (!enableFootsteps) return;
    if (footstepAudioSource == null || footstepSounds == null || footstepSounds.Count == 0) return;
    if (detectionSystem == DetectionSystemType.WeepingAngel && isBeingWatched) return;

    if (agent.velocity.magnitude > minSpeedForFootsteps)
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            int soundIndex = Random.Range(0, footstepSounds.Count);
            PlayNetworkedSound(SoundType.Footstep, soundIndex);
            footstepTimer = isChasing ? chaseFootstepInterval : wanderFootstepInterval;
        }
    }
}

void HandleGrowlSounds()
{
    if (!enableGrowls) return;
    if (growlAudioSource == null || growlSounds == null || growlSounds.Count == 0) return;
    if (detectionSystem == DetectionSystemType.WeepingAngel && isBeingWatched) return;
    if (growlOnlyWhenWandering && isChasing) return;

    growlTimer -= Time.deltaTime;
    if (growlTimer <= 0f)
    {
        int soundIndex = Random.Range(0, growlSounds.Count);
        PlayNetworkedSound(SoundType.Growl, soundIndex);
        growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
    }
}

void HandleChaseLoopSound()
{
    if (!enableChaseLoop) return;
    if (chaseLoopAudioSource == null || chaseLoopSound == null) return;

    bool shouldPlay = isChasing;
    
    if (isInvestigating && currentSpeed > WanderSpeed + 0.1f) shouldPlay = true;
    if (detectionSystem == DetectionSystemType.WeepingAngel && isBeingWatched) shouldPlay = false;

    if (shouldPlay != networkChaseLoopPlaying)
    {
        networkChaseLoopPlaying = shouldPlay;
    }
}

void HandleChaseLoopSoundLocal()
{
    if (!enableChaseLoop) return;
    if (chaseLoopAudioSource == null || chaseLoopSound == null) return;

    if (networkChaseLoopPlaying)
    {
        if (!chaseLoopAudioSource.isPlaying)
        {
            chaseLoopAudioSource.clip = chaseLoopSound;
            chaseLoopAudioSource.loop = true;
            chaseLoopAudioSource.volume = 0f;
            currentChaseVolume = 0f;
            chaseLoopAudioSource.Play();
        }
        currentChaseVolume = Mathf.MoveTowards(currentChaseVolume, 1f, chaseFadeInSpeed * Time.deltaTime);
        chaseLoopAudioSource.volume = currentChaseVolume;
    }
    else
    {
        if (chaseLoopAudioSource.isPlaying)
        {
            currentChaseVolume = Mathf.MoveTowards(currentChaseVolume, 0f, chaseFadeOutSpeed * Time.deltaTime);
            chaseLoopAudioSource.volume = currentChaseVolume;
            if (currentChaseVolume <= 0f) chaseLoopAudioSource.Stop();
        }
    }
}

void PlayDetectionSound(Transform newTarget)
{
    if (!enableDetectionSound) return;
    if (detectionAudioSource == null || detectionSounds == null || detectionSounds.Count == 0) return;
    if (Time.time - lastDetectionSoundTime < detectionSoundCooldown) return;

    if (newTarget == null || lastDetectedTarget != newTarget)
    {
        int soundIndex = Random.Range(0, detectionSounds.Count);
        PlayNetworkedSound(SoundType.Detection, soundIndex);
        lastDetectionSoundTime = Time.time;
    }
}

public enum SoundType { Footstep, Growl, Detection }

void PlayNetworkedSound(SoundType type, int soundIndex)
{
    if (PhotonNetwork.IsConnected)
    {
        pv.RPC("RPC_PlaySound", RpcTarget.All, (int)type, soundIndex);
    }
    else
    {
        PlaySoundLocally(type, soundIndex);
    }
}

[PunRPC]
void RPC_PlaySound(int typeInt, int soundIndex)
{
    SoundType type = (SoundType)typeInt;
    PlaySoundLocally(type, soundIndex);
}

void PlaySoundLocally(SoundType type, int soundIndex)
{
    AudioSource source = null;
    List<AudioClip> clips = null;

    switch (type)
    {
        case SoundType.Footstep:
            source = footstepAudioSource;
            clips = footstepSounds;
            break;
        case SoundType.Growl:
            source = growlAudioSource;
            clips = growlSounds;
            break;
        case SoundType.Detection:
            source = detectionAudioSource;
            clips = detectionSounds;
            break;
    }

    if (source == null || clips == null || clips.Count == 0) return;
    if (soundIndex < 0 || soundIndex >= clips.Count) return;
    if (clips[soundIndex] == null) return;

    source.PlayOneShot(clips[soundIndex]);
}

void StopAllMonsterSounds()
{
    if (chaseLoopAudioSource != null && chaseLoopAudioSource.isPlaying) chaseLoopAudioSource.Stop();
}

#endregion

#region Position & Stuck Things

void RecordPositionHistory()
{
    float recordInterval = positionHistoryDuration / Mathf.Max(1, maxRecentPositions);
    if (Time.time - lastPositionRecordTime > recordInterval)
    {
        recentPositions.Add(transform.position);
        if (recentPositions.Count > maxRecentPositions) recentPositions.RemoveAt(0);
        lastPositionRecordTime = Time.time;
    }
}

void HandleStuckDetection()
{
    if (detectionSystem == DetectionSystemType.WeepingAngel && isBeingWatched) return;
    stuckCheckTimer += Time.deltaTime;
    if (stuckCheckTimer >= 2f)
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < 0.5f && agent.hasPath && !agent.pathPending)
        {
            stuckCheckCounter++;
            if (stuckCheckCounter >= 3)
            {
                stuckCheckCounter = 0;
                recentPositions.Clear();
                Wander();
            }
        }
        else stuckCheckCounter = 0;
        lastPosition = transform.position;
        stuckCheckTimer = 0f;
    }
}

void UpdateWaypointCooldowns()
{
    List<int> keysToRemove = new List<int>();
    foreach (var kvp in waypointCooldowns) if (Time.time > kvp.Value) keysToRemove.Add(kvp.Key);
    foreach (int key in keysToRemove) waypointCooldowns.Remove(key);
}

#endregion

#region Movement

void Chase(Transform target)
{
    if (target != null && agent != null)
    {
        agent.SetDestination(target.position);
        currentDestination = target.position;
        lastKnownTargetPosition = target.position;
    }
}

void Wander()
{
    if (agent == null || !agent.enabled)
        return;
        
    switch (movementType)
    {
        case MovementType.Waypoints: WanderToWaypoint(); break;
        case MovementType.Random: WanderRandom(); break;
    }
}

void WanderToWaypoint()
{
    if (waypoints == null || waypoints.Count == 0) 
    {
        WanderRandom();
        return;
    }
    
    List<int> validWaypointIndices = new List<int>();

    for (int i = 0; i < waypoints.Count; i++) if (IsWaypointValid(i, true, true, true)) validWaypointIndices.Add(i);
    if (validWaypointIndices.Count == 0) for (int i = 0; i < waypoints.Count; i++) if (IsWaypointValid(i, true, true, false)) validWaypointIndices.Add(i);
    if (validWaypointIndices.Count == 0) for (int i = 0; i < waypoints.Count; i++) if (IsWaypointValid(i, true, false, false)) validWaypointIndices.Add(i);
    if (validWaypointIndices.Count == 0) for (int i = 0; i < waypoints.Count; i++) if (IsWaypointValid(i, false, false, false)) validWaypointIndices.Add(i);
    if (validWaypointIndices.Count == 0) for (int i = 0; i < waypoints.Count; i++) if (waypoints[i] != null) validWaypointIndices.Add(i);
    if (validWaypointIndices.Count == 0) 
    {
        WanderRandom();
        return;
    }

    int selectedIndex = validWaypointIndices[Random.Range(0, validWaypointIndices.Count)];
    if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count) waypointCooldowns[currentWaypointIndex] = Time.time + waypointCooldownTime;
    
    currentWaypointIndex = selectedIndex;
    if (agent != null && waypoints[selectedIndex] != null)
    {
        agent.SetDestination(waypoints[selectedIndex].position);
        currentDestination = waypoints[selectedIndex].position;
    }
}

bool IsWaypointValid(int index, bool checkNotCurrent, bool checkCooldown, bool checkRecentPositions)
{
    if (index < 0 || index >= waypoints.Count) return false;
    Transform wp = waypoints[index];
    if (wp == null) return false;
    float distance = Vector3.Distance(transform.position, wp.position);
    if (distance < minWaypointDistance || distance > maxWaypointDistance) return false;
    if (checkNotCurrent && index == currentWaypointIndex) return false;
    if (checkCooldown && waypointCooldowns.ContainsKey(index)) return false;
    if (checkRecentPositions)
    {
        foreach (Vector3 recentPos in recentPositions)
            if (Vector3.Distance(wp.position, recentPos) < minDistanceFromRecentPositions) return false;
    }
    return true;
}

void WanderRandom()
{
    if (agent == null || !agent.enabled)
        return;
        
    Vector3 bestPoint = Vector3.zero;
    bool foundValidPoint = false;
    float bestScore = float.MinValue;
    int attempts = 0;
    int maxAttemptsPerFrame = 100;

    while (!foundValidPoint && attempts < maxAttemptsPerFrame)
    {
        attempts++;
        Vector3 randomDirection = Random.insideUnitSphere * maxRandomDistance;
        randomDirection.y = 0;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, maxRandomDistance, NavMesh.AllAreas))
        {
            float distance = Vector3.Distance(transform.position, hit.position);
            if (distance < minRandomDistance || distance > maxRandomDistance) continue;
            float score = CalculatePointScore(hit.position, distance);
            if (score > bestScore && score > float.MinValue)
            {
                bestScore = score;
                bestPoint = hit.position;
                foundValidPoint = true;
            }
        }
    }
    
    if (!foundValidPoint)
    {
        attempts = 0;
        while (!foundValidPoint && attempts < maxAttemptsPerFrame)
        {
            attempts++;
            Vector3 randomDirection = Random.insideUnitSphere * maxRandomDistance;
            randomDirection.y = 0;
            randomDirection += transform.position;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, maxRandomDistance, NavMesh.AllAreas))
            {
                float distance = Vector3.Distance(transform.position, hit.position);
                if (distance >= minRandomDistance * 0.5f) { bestPoint = hit.position; foundValidPoint = true; }
            }
        }
    }

    if (foundValidPoint && agent != null && agent.enabled)
    {
        agent.SetDestination(bestPoint);
        currentDestination = bestPoint;
    }
}

float CalculatePointScore(Vector3 point, float distance)
{
    float score = 0f;
    float minDistFromRecent = float.MaxValue;
    foreach (Vector3 recentPos in recentPositions)
    {
        float distFromRecent = Vector3.Distance(point, recentPos);
        if (distFromRecent < minDistFromRecent) minDistFromRecent = distFromRecent;
    }
    if (minDistFromRecent < minDistanceFromRecentPositions) return float.MinValue;
    score += minDistFromRecent;
    if (recentPositions.Count > 0)
    {
        Vector3 recentDirection = (recentPositions[recentPositions.Count - 1] - transform.position).normalized;
        Vector3 newDirection = (point - transform.position).normalized;
        float directionDifference = 1f - Vector3.Dot(recentDirection, newDirection);
        score += directionDifference * 10f;
    }
    float optimalDistance = (minRandomDistance + maxRandomDistance) / 2f;
    float distanceScore = 1f - Mathf.Abs(distance - optimalDistance) / optimalDistance;
    score += distanceScore * 5f;
    return score;
}

#endregion

#region Gizmos

void DrawAllGizmos()
{
    Vector3 detectionOrigin = GetDetectionOrigin();
    Quaternion detectionRotation = GetDetectionRotation();

    if (showHearingGizmos && (enableHearing || detectionSystem == DetectionSystemType.Blind) && detectionSystem != DetectionSystemType.WeepingAngel)
    {
        Gizmos.color = hearingRunColor;
        Gizmos.DrawWireSphere(transform.position, hearingRunRadius);

        Gizmos.color = hearingWalkColor;
        Gizmos.DrawWireSphere(transform.position, hearingWalkRadius);
    }

    if (showDetectionGizmos && detectionSystem != DetectionSystemType.Blind)
    {
        switch (detectionSystem)
        {
            case DetectionSystemType.Basic:
                DrawBasicDetectionGizmos();
                break;
            case DetectionSystemType.Normal:
                DrawNormalDetectionGizmos(detectionOrigin, detectionRotation);
                break;
            case DetectionSystemType.WeepingAngel:
                DrawWeepingAngelGizmos();
                break;
        }
    }

    if (showMovementGizmos && movementType == MovementType.Random)
    {
        Gizmos.color = randomDistanceColor;
        DrawWireCircle(transform.position, minRandomDistance, 24);
        DrawWireCircle(transform.position, maxRandomDistance, 32);
    }

    if (showRecentPositionsGizmos && recentPositions != null && recentPositions.Count > 0)
    {
        Gizmos.color = recentPositionsColor;
        foreach (Vector3 pos in recentPositions) Gizmos.DrawWireSphere(pos, minDistanceFromRecentPositions * 0.5f);
        for (int i = 0; i < recentPositions.Count - 1; i++) Gizmos.DrawLine(recentPositions[i], recentPositions[i + 1]);
    }

    if (showPathGizmos && agent != null && agent.hasPath)
    {
        Gizmos.color = pathColor;
        Gizmos.DrawLine(transform.position, currentDestination);
        Gizmos.DrawWireSphere(currentDestination, 0.5f);
    }

    if (showWaypointGizmos && movementType == MovementType.Waypoints)
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        DrawWireCircle(transform.position, minWaypointDistance, 24);
        DrawWireCircle(transform.position, maxWaypointDistance, 32);
    }
}

void DrawBasicDetectionGizmos()
{
    Gizmos.color = basicDetectionColor;
    Gizmos.DrawWireSphere(transform.position, basicDetectionRadius);
    if (!detectThroughWalls)
    {
        Gizmos.color = wallObstructionColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (basicDetectionRadius + 0.5f), Vector3.one * 0.3f);
    }
}

void DrawNormalDetectionGizmos(Vector3 origin, Quaternion rotation)
{
    switch (detectionShape)
    {
        case DetectionShape.Cone: DrawConeGizmo(origin, rotation); break;
        case DetectionShape.Box: DrawBoxGizmo(origin, rotation); break;
        case DetectionShape.Line: DrawLineGizmo(origin, rotation); break;
    }
}

void DrawConeGizmo(Vector3 origin, Quaternion rotation)
{
    Vector3 forward = rotation * Vector3.forward;
    Vector3 right = rotation * Vector3.right;
    Gizmos.color = fovBoundaryColor;
    float halfAngle = fovAngle / 2f;
    int segments = 16;
    Vector3 previousPoint = Vector3.zero;
    for (int i = 0; i <= segments; i++)
    {
        float angle = (float)i / segments * 360f;
        Vector3 direction = Quaternion.AngleAxis(angle, forward) * (Quaternion.AngleAxis(halfAngle, right) * forward);
        Vector3 point = origin + direction * viewDistance;
        if (i > 0) Gizmos.DrawLine(previousPoint, point);
        Gizmos.DrawLine(origin, point);
        previousPoint = point;
    }
    Gizmos.color = basicDetectionColor;
    Gizmos.DrawLine(origin, origin + forward * viewDistance);
}

void DrawBoxGizmo(Vector3 origin, Quaternion rotation)
{
    Gizmos.color = fovBoundaryColor;
    Matrix4x4 oldMatrix = Gizmos.matrix;
    Gizmos.matrix = Matrix4x4.TRS(origin + rotation * new Vector3(0, 0, boxDetectionSize.z / 2f), rotation, Vector3.one);
    Gizmos.DrawWireCube(Vector3.zero, boxDetectionSize);
    Gizmos.matrix = oldMatrix;
}

void DrawLineGizmo(Vector3 origin, Quaternion rotation)
{
    Vector3 forward = rotation * Vector3.forward;
    Gizmos.color = fovBoundaryColor;
    Vector3 leftBoundary = rotation * Quaternion.Euler(0, -lineFovAngle / 2, 0) * Vector3.forward * lineViewDistance;
    Vector3 rightBoundary = rotation * Quaternion.Euler(0, lineFovAngle / 2, 0) * Vector3.forward * lineViewDistance;
    Gizmos.DrawRay(origin, leftBoundary);
    Gizmos.DrawRay(origin, rightBoundary);
    int arcSegments = 20;
    Vector3 previousPoint = origin + leftBoundary;
    for (int i = 1; i <= arcSegments; i++)
    {
        float t = (float)i / arcSegments;
        float angle = Mathf.Lerp(-lineFovAngle / 2, lineFovAngle / 2, t);
        Vector3 direction = rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * lineViewDistance;
        Vector3 point = origin + direction;
        Gizmos.DrawLine(previousPoint, point);
        previousPoint = point;
    }
    Gizmos.color = basicDetectionColor;
    Gizmos.DrawRay(origin, forward * lineViewDistance);
    if (lineDetectedTarget != null)
    {
        Gizmos.color = lineTargetColor;
        Gizmos.DrawLine(origin, lineDetectedTarget.position);
        Gizmos.DrawWireSphere(lineDetectedTarget.position, 0.5f);
    }
}

void DrawWeepingAngelGizmos()
{
    Gizmos.color = weepingAngelCameraZoneColor;
    Gizmos.DrawWireSphere(transform.position, weepingAngelCameraCheckRadius);
    Gizmos.color = weepingAngelPlayerZoneColor;
    Gizmos.DrawWireSphere(transform.position, weepingAngelPlayerDetectionRadius);
    if (isBeingWatched)
    {
        Gizmos.color = beingWatchedColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        foreach (Transform watcher in watchingPlayers) if (watcher != null) Gizmos.DrawLine(transform.position, watcher.position);
    }
    if (!weepingAngelNetworked && nonNetworkedCamera != null)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, nonNetworkedCamera.transform.position);
    }
}

void DrawSelectedGizmos()
{
    if (!showWaypointGizmos) return;
    if (waypoints == null || waypoints.Count == 0) return;
    for (int i = 0; i < waypoints.Count; i++)
    {
        Transform wp = waypoints[i];
        if (wp == null) continue;
        float distance = Vector3.Distance(transform.position, wp.position);
        bool isValid = distance >= minWaypointDistance && distance <= maxWaypointDistance;
        bool isOnCooldown = waypointCooldowns.ContainsKey(i);
        if (isOnCooldown) Gizmos.color = waypointCooldownColor;
        else if (isValid) Gizmos.color = waypointValidColor;
        else Gizmos.color = waypointInvalidColor;
        Gizmos.DrawWireSphere(wp.position, 1f);
        Gizmos.DrawLine(transform.position, wp.position);
    }
}

void DrawWireCircle(Vector3 center, float radius, int segments)
{
    float angleStep = 360f / segments;
    for (int i = 0; i < segments; i++)
    {
        float angle1 = i * angleStep * Mathf.Deg2Rad;
        float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
        Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
        Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
        Gizmos.DrawLine(point1, point2);
    }
}

#endregion

#region Publics

public void ForceWander()
{
    currentTarget = null;
    isChasing = false;
    isInvestigating = false;
    chaseTimer = 0f;
    currentChaseDurationTimer = 0f;
    if (agent != null) agent.speed = WanderSpeed;
    Wander();
}

public void ForceWaypoints()
{
    currentTarget = null;
    isChasing = false;
    isInvestigating = false;
    chaseTimer = 0f;
    currentChaseDurationTimer = 0f;
    if (agent != null) agent.speed = WanderSpeed;
    WanderToWaypoint();
}

public void ForceChase(Transform target)
{
    if (target != null && agent != null)
    {
        currentTarget = target;
        isChasing = true;
        chaseTimer = chaseDuration;
        currentChaseDurationTimer = 0f;
        agent.speed = ChasingSpeed;
        Chase(target);
    }
}

public void StopMovement()
{
    if (agent != null)
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.nextPosition = transform.position;
        
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }
    }
    
    currentTarget = null;
    isChasing = false;
    isInvestigating = false;
    chaseTimer = 0f;
    currentChaseDurationTimer = 0f;
    isFleeing = false;
    
    isManuallyStoppedMovement = true;
}

public void ResumeMovement()
{
    isManuallyStoppedMovement = false;
    if (agent != null) agent.isStopped = false;
    Wander();
}

#endregion
}

public interface IHorrorAIPlusEditor
{
    void OnInspectorGUI();
}

#region Editor

#if UNITY_EDITOR
[CustomEditor(typeof(HorrorAIPlus))]
[InitializeOnLoad]
public class HorrorAIPlusEditor : Editor, IHorrorAIPlusEditor
{
    #region CustomIcon
    static HorrorAIPlusEditor()
    {
        EditorApplication.delayCall += AssignCustomIcon;
    }

    private static void AssignCustomIcon()
    {
        string iconPath = "Assets/HorrorAIPlus/Logo/HorrorAIPlusIcon.png";
        string[] guids = AssetDatabase.FindAssets("HorrorAIPlus t:MonoScript");

        if (guids.Length > 0)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            MonoImporter importer = AssetImporter.GetAtPath(scriptPath) as MonoImporter;
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

            if (importer != null && icon != null && importer.GetIcon() != icon)
            {
                importer.SetIcon(icon);
                importer.SaveAndReimport();
            }
        }
    }
    #endregion
    private readonly string[] tabNames = { "General", "Movement", "AI", "Sounds", "Debug" };

    public override void OnInspectorGUI()
    {
        HorrorAIPlus ai = (HorrorAIPlus)target;

        serializedObject.Update();

        EditorGUILayout.Space(5);
        DrawHeader();
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < tabNames.Length; i++)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            if (ai.currentEditorTab == i)
            {
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.cyan;
            }
            if (GUILayout.Button(tabNames[i], style, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                ai.currentEditorTab = i;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        switch (ai.currentEditorTab)
        {
            case 0: DrawGeneralTab(ai); break;
            case 1: DrawMovementTab(ai); break;
            case 2: DrawAITab(ai); break;
            case 3: DrawSoundsTab(ai); break;
            case 4: DrawDebugTab(ai); break;
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(ai);
    }

    new void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HorrorAIPlus/Logo/HorrorAIPlusLogo.png");

        if (logo != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.MaxHeight(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(0f, 0.7f, 1f);
            EditorGUILayout.LabelField("Horror AI Plus", titleStyle);
        }

        EditorGUILayout.LabelField("An enhanced Horror AI System - Made by P1vr", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawGeneralTab(HorrorAIPlus ai)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("General Settings", headerStyle);
        EditorGUILayout.Space(5);
        ai.agent = (NavMeshAgent)EditorGUILayout.ObjectField("NavMesh Agent", ai.agent, typeof(NavMeshAgent), true);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Target Settings", EditorStyles.boldLabel);
        ai.playerTag = EditorGUILayout.TagField("Player Tag", ai.playerTag);
    }

    void DrawMovementTab(HorrorAIPlus ai)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Movement Settings", headerStyle);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Speed Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.WanderSpeed = EditorGUILayout.FloatField("Wander Speed", ai.WanderSpeed);
        ai.ChasingSpeed = EditorGUILayout.FloatField("Chasing Speed", ai.ChasingSpeed);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        ai.movementType = (HorrorAIPlus.MovementType)EditorGUILayout.EnumPopup("Movement Type", ai.movementType);
        EditorGUILayout.Space(10);

        if (ai.movementType == HorrorAIPlus.MovementType.Waypoints)
        {
            EditorGUILayout.LabelField("Waypoint Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");
            SerializedProperty waypointsProp = serializedObject.FindProperty("waypoints");
            EditorGUILayout.PropertyField(waypointsProp, new GUIContent("Waypoints"), true);
            EditorGUILayout.Space(5);
            ai.minWaypointDistance = EditorGUILayout.FloatField("Min Distance", ai.minWaypointDistance);
            ai.maxWaypointDistance = EditorGUILayout.FloatField("Max Distance", ai.maxWaypointDistance);
            EditorGUILayout.Space(5);
            ai.waypointCooldownTime = EditorGUILayout.FloatField("Waypoint Cooldown", ai.waypointCooldownTime);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.LabelField("Random Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");
            ai.minRandomDistance = EditorGUILayout.FloatField("Min Distance", ai.minRandomDistance);
            ai.maxRandomDistance = EditorGUILayout.FloatField("Max Distance", ai.maxRandomDistance);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Smart AI Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.positionHistoryDuration = EditorGUILayout.FloatField("History Duration", ai.positionHistoryDuration);
        ai.maxRecentPositions = EditorGUILayout.IntSlider("Max Recent Positions", ai.maxRecentPositions, 1, 20);
        ai.minDistanceFromRecentPositions = EditorGUILayout.FloatField("Min Distance From Recent", ai.minDistanceFromRecentPositions);
        EditorGUILayout.EndVertical();
    }

    void DrawAITab(HorrorAIPlus ai)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("AI Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("AI Type", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.detectionSystem = (HorrorAIPlus.DetectionSystemType)EditorGUILayout.EnumPopup("AI Type", ai.detectionSystem);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        if (ai.detectionSystem == HorrorAIPlus.DetectionSystemType.Blind)
        {
            DrawBlindSettings(ai, true);
        }
        else if (ai.detectionSystem != HorrorAIPlus.DetectionSystemType.WeepingAngel)
        {
            DrawBlindSettings(ai, false);
            EditorGUILayout.Space(10);

            if (ai.detectionSystem == HorrorAIPlus.DetectionSystemType.Basic)
                DrawBasicDetectionSettings(ai);
            else
                DrawNormalDetectionSettings(ai);
        }
        else
        {
            DrawWeepingAngelSettings(ai);
        }
    }

    void DrawBlindSettings(HorrorAIPlus ai, bool isBlindMode)
    {
        if (isBlindMode)
        {
            EditorGUILayout.LabelField("Blind Mode Settings", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Hearing", EditorStyles.boldLabel);
        }

        EditorGUILayout.BeginVertical("helpbox");

        if (!isBlindMode)
            ai.enableHearing = EditorGUILayout.Toggle("Enable Hearing", ai.enableHearing);

        if (ai.enableHearing || isBlindMode)
        {
            EditorGUILayout.Space(5);
            ai.hearingWalkRadius = EditorGUILayout.FloatField("Walk To Noise Radius", ai.hearingWalkRadius);
            ai.hearingRunRadius = EditorGUILayout.FloatField("Run To Noise Radius", ai.hearingRunRadius);
            EditorGUILayout.Space(5);
            ai.hearingBlockLayers = LayerMaskField("Sound Block Layers", ai.hearingBlockLayers);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawBasicDetectionSettings(HorrorAIPlus ai)
    {
        EditorGUILayout.LabelField("Basic Detection", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.basicDetectionRadius = EditorGUILayout.FloatField("Chase Radius", ai.basicDetectionRadius);
        EditorGUILayout.Space(5);
        ai.detectThroughWalls = EditorGUILayout.Toggle("Detect Through Walls", ai.detectThroughWalls);
        if (!ai.detectThroughWalls) ai.basicWallLayers = LayerMaskField("Wall Layers", ai.basicWallLayers);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        DrawChaseSettings(ai);
    }

    void DrawNormalDetectionSettings(HorrorAIPlus ai)
    {
        EditorGUILayout.LabelField("Normal Detection", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.detectionShape = (HorrorAIPlus.DetectionShape)EditorGUILayout.EnumPopup("Detection Shape", ai.detectionShape);
        EditorGUILayout.Space(5);
        switch (ai.detectionShape)
        {
            case HorrorAIPlus.DetectionShape.Cone:
                ai.viewDistance = EditorGUILayout.FloatField("View Distance", ai.viewDistance);
                ai.fovAngle = EditorGUILayout.Slider("FOV Angle", ai.fovAngle, 10f, 360f);
                break;
            case HorrorAIPlus.DetectionShape.Box:
                ai.boxDetectionSize = EditorGUILayout.Vector3Field("Box Size", ai.boxDetectionSize);
                break;
            case HorrorAIPlus.DetectionShape.Line:
                ai.lineViewDistance = EditorGUILayout.FloatField("View Distance", ai.lineViewDistance);
                ai.lineFovAngle = EditorGUILayout.Slider("FOV Angle", ai.lineFovAngle, 10f, 180f);
                break;
        }
        EditorGUILayout.Space(5);
        ai.detectionOffset = EditorGUILayout.Vector3Field("Position Offset", ai.detectionOffset);
        ai.detectionRotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", ai.detectionRotationOffset);
        EditorGUILayout.Space(5);
        ai.wallLayers = LayerMaskField("Wall Layers", ai.wallLayers);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        DrawChaseSettings(ai);
    }

    void DrawChaseSettings(HorrorAIPlus ai)
    {
        EditorGUILayout.LabelField("Chase Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.chaseDuration = EditorGUILayout.FloatField("Chase Duration", ai.chaseDuration);
        EditorGUILayout.Space(5);
        ai.chaseGiveUpTime = EditorGUILayout.FloatField("Max Chase Time", ai.chaseGiveUpTime);
        EditorGUILayout.EndVertical();
    }

    void DrawWeepingAngelSettings(HorrorAIPlus ai)
    {
        EditorGUILayout.LabelField("Weeping Angel Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.weepingAngelNetworked = EditorGUILayout.Toggle("Networked", ai.weepingAngelNetworked);
        EditorGUILayout.Space(5);
        if (ai.weepingAngelNetworked) EditorGUILayout.HelpBox("Networked: Finds all cameras from 'Player(Clone)' objects.", MessageType.None);
        else ai.nonNetworkedCamera = (Camera)EditorGUILayout.ObjectField("Camera", ai.nonNetworkedCamera, typeof(Camera), true);
        EditorGUILayout.Space(5);
        ai.weepingAngelCameraCheckRadius = EditorGUILayout.FloatField("Camera Detection Radius", ai.weepingAngelCameraCheckRadius);
        ai.weepingAngelPlayerDetectionRadius = EditorGUILayout.FloatField("Player Detection Radius", ai.weepingAngelPlayerDetectionRadius);
        EditorGUILayout.Space(5);
        ai.playerVisibilityLayers = LayerMaskField("Wall Layers", ai.playerVisibilityLayers);
        EditorGUILayout.EndVertical();
    }

    LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName)) { layers.Add(layerName); layerNumbers.Add(i); }
        }
        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++) if (((1 << layerNumbers[i]) & layerMask.value) != 0) maskWithoutEmpty |= (1 << i);
        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++) if ((maskWithoutEmpty & (1 << i)) != 0) mask |= (1 << layerNumbers[i]);
        return mask;
    }

    void DrawSoundsTab(HorrorAIPlus ai)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Sounds Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Footstep Sounds", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.enableFootsteps = EditorGUILayout.Toggle("Enable Footsteps", ai.enableFootsteps);
        if (ai.enableFootsteps)
        {
            ai.footstepAudioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", ai.footstepAudioSource, typeof(AudioSource), true);
            EditorGUILayout.Space(5);
            SerializedProperty footstepSoundsProp = serializedObject.FindProperty("footstepSounds");
            EditorGUILayout.PropertyField(footstepSoundsProp, new GUIContent("Footstep Sounds"), true);
            EditorGUILayout.Space(5);
            ai.wanderFootstepInterval = EditorGUILayout.FloatField("Wander Footstep Interval", ai.wanderFootstepInterval);
            ai.chaseFootstepInterval = EditorGUILayout.FloatField("Chase Footstep Interval", ai.chaseFootstepInterval);
            ai.minSpeedForFootsteps = EditorGUILayout.FloatField("Min Speed For Footsteps", ai.minSpeedForFootsteps);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Growl Sounds", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.enableGrowls = EditorGUILayout.Toggle("Enable Growls", ai.enableGrowls);
        if (ai.enableGrowls)
        {
            ai.growlAudioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", ai.growlAudioSource, typeof(AudioSource), true);
            EditorGUILayout.Space(5);
            SerializedProperty growlSoundsProp = serializedObject.FindProperty("growlSounds");
            EditorGUILayout.PropertyField(growlSoundsProp, new GUIContent("Growl Sounds"), true);
            EditorGUILayout.Space(5);
            ai.minGrowlInterval = EditorGUILayout.FloatField("Min Growl Interval", ai.minGrowlInterval);
            ai.maxGrowlInterval = EditorGUILayout.FloatField("Max Growl Interval", ai.maxGrowlInterval);
            ai.growlOnlyWhenWandering = EditorGUILayout.Toggle("Growl Only When Wandering", ai.growlOnlyWhenWandering);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Detection Sounds", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.enableDetectionSound = EditorGUILayout.Toggle("Enable Detection Sound", ai.enableDetectionSound);
        if (ai.enableDetectionSound)
        {
            ai.detectionAudioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", ai.detectionAudioSource, typeof(AudioSource), true);
            EditorGUILayout.Space(5);
            SerializedProperty detectionSoundsProp = serializedObject.FindProperty("detectionSounds");
            EditorGUILayout.PropertyField(detectionSoundsProp, new GUIContent("Detection Sounds"), true);
            EditorGUILayout.Space(5);
            ai.detectionSoundCooldown = EditorGUILayout.FloatField("Detection Sound Cooldown", ai.detectionSoundCooldown);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Chase Sounds", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.enableChaseLoop = EditorGUILayout.Toggle("Enable Chase Loop", ai.enableChaseLoop);
        if (ai.enableChaseLoop)
        {
            ai.chaseLoopAudioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", ai.chaseLoopAudioSource, typeof(AudioSource), true);
            EditorGUILayout.Space(5);
            ai.chaseLoopSound = (AudioClip)EditorGUILayout.ObjectField("Chase Loop Sound", ai.chaseLoopSound, typeof(AudioClip), false);
            EditorGUILayout.Space(5);
            ai.chaseFadeInSpeed = EditorGUILayout.FloatField("Chase Fade In Speed", ai.chaseFadeInSpeed);
            ai.chaseFadeOutSpeed = EditorGUILayout.FloatField("Chase Fade Out Speed", ai.chaseFadeOutSpeed);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawDebugTab(HorrorAIPlus ai)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Debug Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Gizmo Toggles", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        ai.showDebugInfo = EditorGUILayout.Toggle("Show Debug Info", ai.showDebugInfo);
        ai.showGizmos = EditorGUILayout.Toggle("Show Gizmos", ai.showGizmos);

        if (ai.showGizmos)
        {
            EditorGUI.indentLevel++;
            ai.showDetectionGizmos = EditorGUILayout.Toggle("Show Detection Gizmos", ai.showDetectionGizmos);

            if ((ai.enableHearing || ai.detectionSystem == HorrorAIPlus.DetectionSystemType.Blind) &&
                ai.detectionSystem != HorrorAIPlus.DetectionSystemType.WeepingAngel)
            {
                ai.showHearingGizmos = EditorGUILayout.Toggle("Show Hearing Gizmos", ai.showHearingGizmos);
            }

            ai.showMovementGizmos = EditorGUILayout.Toggle("Show Movement Gizmos", ai.showMovementGizmos);
            ai.showPathGizmos = EditorGUILayout.Toggle("Show Path Gizmos", ai.showPathGizmos);

            if (ai.movementType == HorrorAIPlus.MovementType.Waypoints)
            {
                ai.showWaypointGizmos = EditorGUILayout.Toggle("Show Waypoint Gizmos", ai.showWaypointGizmos);
            }

            ai.showRecentPositionsGizmos = EditorGUILayout.Toggle("Show Recent Positions Gizmos", ai.showRecentPositionsGizmos);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        if (!ai.showGizmos)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gizmo Colors", EditorStyles.boldLabel);

        if (ai.showPathGizmos)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Path", EditorStyles.miniBoldLabel);
            ai.pathColor = EditorGUILayout.ColorField("Path Color", ai.pathColor);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (ai.showRecentPositionsGizmos)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Recent Positions", EditorStyles.miniBoldLabel);
            ai.recentPositionsColor = EditorGUILayout.ColorField("Recent Positions Color", ai.recentPositionsColor);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (ai.showMovementGizmos || (ai.showWaypointGizmos && ai.movementType == HorrorAIPlus.MovementType.Waypoints))
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);

            if (ai.movementType == HorrorAIPlus.MovementType.Waypoints && ai.showWaypointGizmos)
            {
                ai.waypointValidColor = EditorGUILayout.ColorField("Waypoint Valid Color", ai.waypointValidColor);
                ai.waypointInvalidColor = EditorGUILayout.ColorField("Waypoint Invalid Color", ai.waypointInvalidColor);
                ai.waypointCooldownColor = EditorGUILayout.ColorField("Waypoint Cooldown Color", ai.waypointCooldownColor);
            }

            if (ai.movementType == HorrorAIPlus.MovementType.Random && ai.showMovementGizmos)
            {
                ai.randomDistanceColor = EditorGUILayout.ColorField("Random Distance Color", ai.randomDistanceColor);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        bool showHearingColors = ai.showHearingGizmos &&
                                 (ai.enableHearing || ai.detectionSystem == HorrorAIPlus.DetectionSystemType.Blind) &&
                                 ai.detectionSystem != HorrorAIPlus.DetectionSystemType.WeepingAngel;

        if (showHearingColors)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Hearing", EditorStyles.miniBoldLabel);
            ai.hearingRunColor = EditorGUILayout.ColorField("Hearing Run Color", ai.hearingRunColor);
            ai.hearingWalkColor = EditorGUILayout.ColorField("Hearing Walk Color", ai.hearingWalkColor);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (ai.showDetectionGizmos && ai.detectionSystem != HorrorAIPlus.DetectionSystemType.Blind)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Detection", EditorStyles.miniBoldLabel);

            switch (ai.detectionSystem)
            {
                case HorrorAIPlus.DetectionSystemType.Basic:
                    ai.basicDetectionColor = EditorGUILayout.ColorField("Detection Radius Color", ai.basicDetectionColor);
                    if (!ai.detectThroughWalls)
                    {
                        ai.wallObstructionColor = EditorGUILayout.ColorField("Wall Check Indicator Color", ai.wallObstructionColor);
                    }
                    break;

                case HorrorAIPlus.DetectionSystemType.Normal:
                    switch (ai.detectionShape)
                    {
                        case HorrorAIPlus.DetectionShape.Cone:
                            ai.basicDetectionColor = EditorGUILayout.ColorField("Center Line Color", ai.basicDetectionColor);
                            ai.fovBoundaryColor = EditorGUILayout.ColorField("FOV Boundary Color", ai.fovBoundaryColor);
                            break;

                        case HorrorAIPlus.DetectionShape.Box:
                            ai.fovBoundaryColor = EditorGUILayout.ColorField("Box Boundary Color", ai.fovBoundaryColor);
                            break;

                        case HorrorAIPlus.DetectionShape.Line:
                            ai.basicDetectionColor = EditorGUILayout.ColorField("Center Line Color", ai.basicDetectionColor);
                            ai.fovBoundaryColor = EditorGUILayout.ColorField("FOV Boundary Color", ai.fovBoundaryColor);
                            ai.lineTargetColor = EditorGUILayout.ColorField("Target Line Color", ai.lineTargetColor);
                            break;
                    }
                    break;

                case HorrorAIPlus.DetectionSystemType.WeepingAngel:
                    ai.weepingAngelCameraZoneColor = EditorGUILayout.ColorField("Camera Zone Color", ai.weepingAngelCameraZoneColor);
                    ai.weepingAngelPlayerZoneColor = EditorGUILayout.ColorField("Player Zone Color", ai.weepingAngelPlayerZoneColor);
                    ai.beingWatchedColor = EditorGUILayout.ColorField("Being Watched Color", ai.beingWatchedColor);
                    break;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

#endregion