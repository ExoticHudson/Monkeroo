using UnityEngine;
using easyInputs;

public class KeosZiplineSystem : MonoBehaviour
{
    [Header("Player Stuff")]
    public Rigidbody Player;
    public EasyHand Hand = EasyHand.LeftHand;

    [Header("Layer and Settings")]
    public LayerMask ZiplineLayer;
    public float GrabableRange = 0.056f;

    [Header("Physics Settings")]
    public float MinimumSlideSpeed = 2f;
    public float MaxSlideSpeed = 15f;
    public float Friction = 0.98f;
    public float GravityMultiplier = 1.5f;

    [Header("Audio Settings")]
    public AudioSource ZiplineAudioSource;
    public float MinAudioPitch = 0.5f;
    public float MaxAudioPitch = 2.0f;

    [Header("Movement")]
    public bool FunMode = false;

    [Header("Debug Stuff")]
    public bool AutoGrab = false;
    public bool ShowDebugInfo = false;
    public bool ShowZiplineInfos = false;

    private Collider[] NearbyZiplineObjects;
    private Transform AttachmentPoint;
    private bool Grabbing;
    private bool OnZipline;
    private Collider ZiplineCollider;
    private ZiplineData CurrentZiplineData;
    private float CurrentZiplinePosition;
    private Vector3 LastZiplineWorldPosition;
    private float CurrentSlideVelocity;
    private Vector3 ZiplineDirection;

    private void Start()
    {
        OnZipline = false;
        CurrentSlideVelocity = 0f;
        
        if (ZiplineAudioSource != null)
        {
            ZiplineAudioSource.loop = true;
            ZiplineAudioSource.Stop();
        }
    }

    private void Update()
    {
        Grabbing = EasyInputs.GetGripButtonDown(Hand);
        NearbyZiplineObjects = Physics.OverlapSphere(transform.position, GrabableRange, ZiplineLayer);

        if (ShowDebugInfo && OnZipline)
        {
            Debug.Log($"Zipline Position: {CurrentZiplinePosition:F2}, Slide Velocity: {CurrentSlideVelocity:F2}, Direction Y: {ZiplineDirection.y:F2}");
        }

        UpdateAudio();
    }

    private void UpdateAudio()
    {
        if (ZiplineAudioSource == null) return;

        if (OnZipline && Mathf.Abs(CurrentSlideVelocity) > 0.1f)
        {
            if (!ZiplineAudioSource.isPlaying)
            {
                ZiplineAudioSource.Play();
            }

            float speedPercent = Mathf.Abs(CurrentSlideVelocity) / MaxSlideSpeed;
            speedPercent = Mathf.Clamp01(speedPercent);
            
            float targetPitch = Mathf.Lerp(MinAudioPitch, MaxAudioPitch, speedPercent);
            ZiplineAudioSource.pitch = Mathf.Lerp(ZiplineAudioSource.pitch, targetPitch, Time.deltaTime * 5f);
        }
        else
        {
            if (ZiplineAudioSource.isPlaying)
            {
                ZiplineAudioSource.Stop();
            }
        }
    }

    private void OnGUI()
    {
        if (!ShowZiplineInfos) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        if (Hand == EasyHand.LeftHand)
        {
            GUI.Label(new Rect(10, 10, 300, 200), GetZiplineInfo(), style);
        }
        else
        {
            GUI.Label(new Rect(screenWidth - 310, 10, 300, 200), GetZiplineInfo(), style);
        }
    }

    private string GetZiplineInfo()
    {
        string info = $"Hand: {Hand}\n";
        info += $"On Zipline: {OnZipline}\n";
        info += $"Grabbing: {Grabbing}\n";
        info += $"Nearby Objects: {NearbyZiplineObjects?.Length ?? 0}\n";
        
        if (OnZipline)
        {
            info += $"Position: {CurrentZiplinePosition:F2}\n";
            info += $"Velocity: {CurrentSlideVelocity:F2}\n";
            info += $"Direction Y: {ZiplineDirection.y:F2}\n";
            
            float actualSlope = GetActualSlope();
            info += $"Actual Slope: {actualSlope:F2}\n";
        }
        
        return info;
    }

    private void FixedUpdate()
    {
        if ((AutoGrab || Grabbing) && (OnZipline || NearbyZiplineObjects.Length > 0))
        {
            if (!OnZipline)
            {
                StartZiplining();
            }

            if (OnZipline)
            {
                HandleZiplineMovement();
            }
        }
        else if (OnZipline)
        {
            StopZiplining();
        }
    }

    private void StartZiplining()
    {
        if (AttachmentPoint != null)
        {
            Destroy(AttachmentPoint.gameObject);
        }

        ZiplineCollider = NearbyZiplineObjects[0];
        CurrentZiplineData = ZiplineCollider.GetComponentInParent<ZiplineData>();

        if (CurrentZiplineData == null)
        {
            Debug.LogWarning("No ZiplineData found on zipline object!");
            return;
        }

        CurrentZiplinePosition = FindClosestPointOnZipline(transform.position);

        Vector3 ziplinePoint = CurrentZiplineData.GetCurvePosition(CurrentZiplinePosition);
        AttachmentPoint = new GameObject("ZiplineAttachmentPoint").transform;
        AttachmentPoint.position = ziplinePoint;
        AttachmentPoint.parent = CurrentZiplineData.transform;

        LastZiplineWorldPosition = ziplinePoint;

        Vector3 playerVelocity = Player.velocity;
        ZiplineDirection = GetZiplineDirection(CurrentZiplinePosition);
        
        float projectedVelocity = Vector3.Dot(playerVelocity, ZiplineDirection);
        
        CurrentSlideVelocity = projectedVelocity;
        
        if (Mathf.Abs(CurrentSlideVelocity) < MinimumSlideSpeed)
        {
            float forwardSlope = GetSlopeInDirection(0.05f);
            float backwardSlope = GetSlopeInDirection(-0.05f);
            
            if (forwardSlope > 0.1f && forwardSlope >= backwardSlope)
            {
                CurrentSlideVelocity = MinimumSlideSpeed;
            }
            else if (backwardSlope > 0.1f && backwardSlope > forwardSlope)
            {
                CurrentSlideVelocity = -MinimumSlideSpeed;
            }
            else
            {
                Vector3 playerToZipline = ziplinePoint - transform.position;
                float dotProduct = Vector3.Dot(playerToZipline.normalized, ZiplineDirection);
                CurrentSlideVelocity = dotProduct > 0 ? MinimumSlideSpeed : -MinimumSlideSpeed;
            }
        }

        if (!FunMode)
        {
            CurrentSlideVelocity = Mathf.Clamp(CurrentSlideVelocity, -MaxSlideSpeed, MaxSlideSpeed);
        }

        if (ZiplineCollider != null)
        {
            ZiplineCollider.enabled = false;
        }

        OnZipline = true;
        
        if (ShowDebugInfo)
        {
            Debug.Log($"Started ziplining with velocity: {CurrentSlideVelocity:F2}");
        }
    }

    private void HandleZiplineMovement()
    {
        if (CurrentZiplineData == null || AttachmentPoint == null)
        {
            StopZiplining();
            return;
        }

        float actualSlope = GetActualSlope();

        if (actualSlope > 0.02f)
        {
            float gravityForce = Mathf.Abs(Physics.gravity.y) * actualSlope * GravityMultiplier;
            if (CurrentSlideVelocity >= 0)
            {
                CurrentSlideVelocity += gravityForce * Time.fixedDeltaTime;
            }
            else
            {
                CurrentSlideVelocity -= gravityForce * Time.fixedDeltaTime;
            }

            if (ShowDebugInfo)
            {
                Debug.Log($"Downhill - Slope: {actualSlope:F2}, Adding Gravity: {gravityForce:F2}");
            }
        }
        else if (actualSlope < -0.02f)
        {
            float gravityForce = Mathf.Abs(Physics.gravity.y) * Mathf.Abs(actualSlope) * GravityMultiplier;
            
            if (Mathf.Abs(CurrentSlideVelocity) > 0.5f)
            {
                float sign = Mathf.Sign(CurrentSlideVelocity);
                CurrentSlideVelocity -= gravityForce * Time.fixedDeltaTime * sign;
                
                if (Mathf.Sign(CurrentSlideVelocity) != sign)
                {
                    CurrentSlideVelocity = 0f;
                }
            }

            if (ShowDebugInfo)
            {
                Debug.Log($"Uphill - Slope: {actualSlope:F2}, Reducing by: {gravityForce:F2}");
            }
        }

        CurrentSlideVelocity *= Mathf.Pow(Friction, Time.fixedDeltaTime * 60f);

        if (Mathf.Abs(CurrentSlideVelocity) < 0.1f)
        {
            float forwardSlope = GetSlopeInDirection(0.05f);
            float backwardSlope = GetSlopeInDirection(-0.05f);

            if (forwardSlope > 0.15f || backwardSlope > 0.15f)
            {
                if (forwardSlope > backwardSlope)
                {
                    CurrentSlideVelocity = 1.0f;
                }
                else
                {
                    CurrentSlideVelocity = -1.0f;
                }
            }
        }

        if (!FunMode)
        {
            CurrentSlideVelocity = Mathf.Clamp(CurrentSlideVelocity, -MaxSlideSpeed, MaxSlideSpeed);
        }

        Vector3 currentZiplinePoint = CurrentZiplineData.GetCurvePosition(CurrentZiplinePosition);
        float distanceFromZipline = Vector3.Distance(transform.position, currentZiplinePoint);

        if (distanceFromZipline > 3f)
        {
            Player.velocity = Player.velocity;
            StopZiplining();
            return;
        }

        float segmentLength = GetSegmentLength(CurrentZiplinePosition);
        float normalizedSpeed = CurrentSlideVelocity / segmentLength;
        float positionDelta = normalizedSpeed * Time.fixedDeltaTime;

        CurrentZiplinePosition += positionDelta;

        if (CurrentZiplineData.loopZipline)
        {
            if (CurrentZiplinePosition >= 1f)
            {
                CurrentZiplinePosition = CurrentZiplinePosition - 1f;
            }
            else if (CurrentZiplinePosition < 0f)
            {
                CurrentZiplinePosition = 1f + CurrentZiplinePosition;
            }
        }
        else
        {
            if (CurrentZiplinePosition >= 1f || CurrentZiplinePosition <= 0f)
            {
                Vector3 exitDirection = CurrentSlideVelocity > 0 ? GetZiplineDirection(CurrentZiplinePosition) : -GetZiplineDirection(CurrentZiplinePosition);
                float exitSpeed = Mathf.Abs(CurrentSlideVelocity);

                Vector3 boostVelocity = exitDirection * exitSpeed;
                if (exitDirection.y > -0.3f)
                {
                    boostVelocity += Vector3.up * (exitSpeed * 0.3f);
                }

                Player.velocity = boostVelocity;
                StopZiplining();
                return;
            }

            CurrentZiplinePosition = Mathf.Clamp01(CurrentZiplinePosition);
        }

        Vector3 newZiplinePoint = CurrentZiplineData.GetCurvePosition(CurrentZiplinePosition);
        AttachmentPoint.position = newZiplinePoint;

        Vector3 toAttachment = AttachmentPoint.position - transform.position;
        float distanceToAttachment = toAttachment.magnitude;

        if (distanceToAttachment > 0.01f)
        {
            Vector3 currentDirection = GetZiplineDirection(CurrentZiplinePosition);
            Vector3 ziplineVelocity = currentDirection * CurrentSlideVelocity;
            Vector3 constraintVelocity = toAttachment.normalized * (distanceToAttachment / Time.fixedDeltaTime);
            Player.velocity = Vector3.Lerp(ziplineVelocity, constraintVelocity, 0.9f);
        }

        ZiplineDirection = GetZiplineDirection(CurrentZiplinePosition);
        LastZiplineWorldPosition = newZiplinePoint;
    }

    private void StopZiplining()
    {
        if (AttachmentPoint != null)
        {
            Destroy(AttachmentPoint.gameObject);
            AttachmentPoint = null;
        }

        if (ZiplineCollider != null)
        {
            ZiplineCollider.enabled = true;
            ZiplineCollider = null;
        }

        CurrentZiplineData = null;
        OnZipline = false;
        CurrentSlideVelocity = 0f;
        
        if (ZiplineAudioSource != null && ZiplineAudioSource.isPlaying)
        {
            ZiplineAudioSource.Stop();
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Stopped ziplining");
        }
    }

    private float FindClosestPointOnZipline(Vector3 worldPosition)
    {
        float closestT = 0f;
        float closestDistance = float.MaxValue;

        int samples = 50;
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 curvePoint = CurrentZiplineData.GetCurvePosition(t);
            float distance = Vector3.Distance(worldPosition, curvePoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        return closestT;
    }

    private Vector3 GetZiplineDirection(float t)
    {
        float delta = 0.01f;
        float t1 = Mathf.Clamp01(t - delta);
        float t2 = Mathf.Clamp01(t + delta);

        Vector3 pos1 = CurrentZiplineData.GetCurvePosition(t1);
        Vector3 pos2 = CurrentZiplineData.GetCurvePosition(t2);

        Vector3 direction = (pos2 - pos1).normalized;
        
        if (direction.magnitude < 0.01f)
        {
            direction = Vector3.forward;
        }

        return direction;
    }

    private float GetActualSlope()
    {
        Vector3 currentDirection = GetZiplineDirection(CurrentZiplinePosition);
        
        if (CurrentSlideVelocity >= 0)
        {
            return -currentDirection.y;
        }
        else
        {
            return currentDirection.y;
        }
    }

    private float GetSlopeInDirection(float deltaT)
    {
        float sampleT = Mathf.Clamp01(CurrentZiplinePosition + deltaT);
        Vector3 currentPos = CurrentZiplineData.GetCurvePosition(CurrentZiplinePosition);
        Vector3 samplePos = CurrentZiplineData.GetCurvePosition(sampleT);
        
        if (Vector3.Distance(currentPos, samplePos) < 0.001f) return 0f;
        
        Vector3 direction = (samplePos - currentPos).normalized;
        return -direction.y;
    }

    private float GetSegmentLength(float t)
    {
        float delta = 0.01f;
        float t1 = Mathf.Clamp01(t - delta);
        float t2 = Mathf.Clamp01(t + delta);

        Vector3 pos1 = CurrentZiplineData.GetCurvePosition(t1);
        Vector3 pos2 = CurrentZiplineData.GetCurvePosition(t2);

        float segmentDistance = Vector3.Distance(pos1, pos2);
        return Mathf.Max(segmentDistance / (delta * 2f), 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GrabableRange);

        if (OnZipline && CurrentZiplineData != null)
        {
            Gizmos.color = Color.green;
            Vector3 currentPos = CurrentZiplineData.GetCurvePosition(CurrentZiplinePosition);
            Gizmos.DrawSphere(currentPos, 0.1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(currentPos, ZiplineDirection * 2f);

            Gizmos.color = CurrentSlideVelocity > 0 ? Color.green : Color.red;
            Vector3 velocityDirection = CurrentSlideVelocity > 0 ? ZiplineDirection : -ZiplineDirection;
            Gizmos.DrawRay(currentPos + Vector3.up * 0.5f, velocityDirection * Mathf.Abs(CurrentSlideVelocity) * 0.2f);
        }
    }
}