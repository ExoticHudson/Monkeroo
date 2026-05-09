using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using GorillaLocomotion;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(PhotonView))]
public class KillPlayerPlus : MonoBehaviourPunCallbacks
{
    // ==================== REFERENCES ====================
    [HideInInspector] public HorrorAIPlus horrorAI;
    [HideInInspector] public Player player;

    // ==================== KILL DETECTION SETTINGS ====================
    [HideInInspector] public bool requireChasing = true;
    [HideInInspector] public float killCooldown = 3f;
    [HideInInspector] public List<string> playerTags = new List<string>() { "HandTag", "Player" };

    // ==================== NETWORK KILL (MONSTER MODEL) ====================
    [HideInInspector] public bool networkKill = true;
    [HideInInspector] public GameObject defaultMonsterModel;
    [HideInInspector] public GameObject killMonsterModel;
    [HideInInspector] public float killDuration = 2f;
    [HideInInspector] public float aiPauseDuration = 3f;

    // ==================== JUMPSCARE SETTINGS ====================
    [HideInInspector] public bool enableJumpscare = false;
    [HideInInspector] public GameObject jumpscareObject;
    [HideInInspector] public float jumpscareDuration = 1f;

    // ==================== PLAYER TELEPORT SETTINGS ====================
    [HideInInspector] public Transform respawnPoint;
    [HideInInspector] public float locomotionEnableDelay = 0.1f;
    [HideInInspector] public LayerMask locomotionLayer;
    [HideInInspector] public bool disableGravityDuringKill = true;
    [HideInInspector] public float gravityEnableDelay = 1f;

    // ==================== TARGET CLEARING SETTINGS ====================
    [HideInInspector] public bool clearTargetAfterKill = true;
    [HideInInspector] public float immunityDuration = 5f;

    // ==================== RUNTIME VARIABLES ====================
    [HideInInspector] public bool isPerformingKill = false;
    
    private float lastKillTime = -999f;
    private PhotonView pv;
    private bool hasValidPhotonView = false;
    private Coroutine killSequenceCoroutine;
    private Coroutine gravityCoroutine;
    private Coroutine locomotionCoroutine;
    private Coroutine jumpscareCoroutine;
    private Coroutine modelCoroutine;
    private Coroutine immunityCoroutine;
    private LayerMask originalLocomotionLayer;
    private bool hasImmunity = false;
    private float immunityEndTime = 0f;
    private Rigidbody playerRigidbody;
    private bool networkKillModelActive = false;

    // ==================== EVENTS ====================
    public System.Action OnPlayerKilled;
    public System.Action OnKillStarted;
    public System.Action OnKillFinished;

    [HideInInspector] public int currentEditorTab = 0;

    #region Unity

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        
        if (pv != null && pv.ViewID > 0)
        {
            hasValidPhotonView = true;
        }
        else
        {
            hasValidPhotonView = false;
            Debug.LogWarning("KillPlayerPlus PhotonView missing");
        }
    }

    private void Start()
    {
        if (horrorAI == null) horrorAI = GetComponent<HorrorAIPlus>();
        
        if (killMonsterModel != null) killMonsterModel.SetActive(false);
        if (defaultMonsterModel != null) defaultMonsterModel.SetActive(true);
        if (jumpscareObject != null) jumpscareObject.SetActive(false);

        if (player == null) player = FindObjectOfType<Player>();

        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
            originalLocomotionLayer = player.locomotionEnabledLayers;
            
            if (locomotionLayer.value == 0) locomotionLayer = originalLocomotionLayer;
        }
    }

    private void Update()
    {
        if (hasImmunity && Time.time >= immunityEndTime)
        {
            hasImmunity = false;
        }

        if (networkKill && hasValidPhotonView && !PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnected)
        {
             if (killMonsterModel != null && killMonsterModel.activeSelf != networkKillModelActive && modelCoroutine == null)
             {
                 if (networkKillModelActive) StartKillModelSequence();
             }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPerformingKill) return;
        if (Time.time - lastKillTime < killCooldown) return;
        if (hasImmunity) return;

        if (requireChasing && horrorAI != null)
        {
            bool isSpecialMode = horrorAI.detectionSystem == HorrorAIPlus.DetectionSystemType.WeepingAngel || 
                                 horrorAI.detectionSystem == HorrorAIPlus.DetectionSystemType.Blind;

            if (!isSpecialMode && !horrorAI.isChasing) return;
        }

        if (HasValidTag(other))
        {
            if (IsLocalPlayerCollider(other))
            {
                TriggerKill();
            }
        }
    }

    #endregion

    #region Photon

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!networkKill) return;

        if (stream.IsWriting)
        {
            stream.SendNext(networkKillModelActive);
        }
        else
        {
            networkKillModelActive = (bool)stream.ReceiveNext();
        }
    }

    #endregion

    #region Detection

    private bool HasValidTag(Collider collider)
    {
        if (playerTags == null || playerTags.Count == 0) return false;
        foreach (string tag in playerTags)
        {
            if (!string.IsNullOrEmpty(tag) && collider.CompareTag(tag)) return true;
        }
        return false;
    }

    private bool IsLocalPlayerCollider(Collider other)
    {
        if (player == null) return false;
        
        Transform current = other.transform;
        while (current != null)
        {
            if (current == player.transform) return true;
            if (current.parent != null && player.transform.IsChildOf(current.parent)) return true;
            current = current.parent;
        }
        
        if (Vector3.Distance(other.transform.position, player.transform.position) < 2f)
        {
            if (other.GetComponentInParent<Player>() == player) return true;
        }
        
        return false;
    }

    #endregion

    #region Kill

    private void TriggerKill()
    {
        if (isPerformingKill) return;
        if (hasImmunity) return;
        if (player == null) return;

        isPerformingKill = true;
        lastKillTime = Time.time;
        
        hasImmunity = true;
        immunityEndTime = Time.time + immunityDuration;

        OnKillStarted?.Invoke();
        OnPlayerKilled?.Invoke();

        if (killSequenceCoroutine != null) StopCoroutine(killSequenceCoroutine);
        killSequenceCoroutine = StartCoroutine(LocalPlayerKillSequence());

        if (immunityCoroutine != null) StopCoroutine(immunityCoroutine);
        immunityCoroutine = StartCoroutine(ImmunityCoroutine());

        if (networkKill)
        {
            if (PhotonNetwork.IsConnected && hasValidPhotonView)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    RPC_StartKill(); 
                }
                else
                {
                    pv.RPC("RPC_RequestKillSequence", RpcTarget.MasterClient);
                }
            }
            else
            {
                StartKillModelSequence();
            }
        }
    }

    [PunRPC]
    private void RPC_RequestKillSequence()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            pv.RPC("RPC_StartKill", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_StartKill()
    {
        StartKillModelSequence();

        if (PhotonNetwork.IsMasterClient && horrorAI != null)
        {
            horrorAI.StopMovement(); 
            
            if (clearTargetAfterKill)
            {
                horrorAI.currentTarget = null;
                horrorAI.isChasing = false;
                horrorAI.isInvestigating = false;
                horrorAI.chaseTimer = 0f;
            }
        }
    }

    private IEnumerator LocalPlayerKillSequence()
    {
        if (enableJumpscare && jumpscareObject != null)
        {
            jumpscareObject.SetActive(true);
            if (jumpscareCoroutine != null) StopCoroutine(jumpscareCoroutine);
            jumpscareCoroutine = StartCoroutine(HideJumpscareAfterDelay());
        }

        if (player != null) player.locomotionEnabledLayers = 0;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            if (disableGravityDuringKill) playerRigidbody.useGravity = false;
        }

        if (player != null && respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            if (playerRigidbody != null) playerRigidbody.velocity = Vector3.zero;
        }

        if (locomotionCoroutine != null) StopCoroutine(locomotionCoroutine);
        locomotionCoroutine = StartCoroutine(ReEnableLocomotionAfterDelay());

        if (disableGravityDuringKill)
        {
            if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
            gravityCoroutine = StartCoroutine(ReEnableGravityAfterDelay());
        }

        yield return new WaitForSeconds(killDuration);

        isPerformingKill = false;
        OnKillFinished?.Invoke();
    }

    private IEnumerator ImmunityCoroutine()
    {
        float checkInterval = 0.1f;
        while (hasImmunity && Time.time < immunityEndTime)
        {
            if (horrorAI != null && horrorAI.currentTarget != null)
            {
                if (IsThisPlayer(horrorAI.currentTarget)) ClearLocalAITarget();
            }
            yield return new WaitForSeconds(checkInterval);
        }
        hasImmunity = false;
    }

    private bool IsThisPlayer(Transform target)
    {
        if (player == null || target == null) return false;
        Transform current = target;
        while (current != null)
        {
            if (current == player.transform) return true;
            current = current.parent;
        }
        if (Vector3.Distance(target.position, player.transform.position) < 3f) return true;
        return false;
    }

    private void ClearLocalAITarget()
    {
        if (horrorAI == null) return;
        horrorAI.currentTarget = null;
        horrorAI.isChasing = false;
    }

    private IEnumerator HideJumpscareAfterDelay()
    {
        yield return new WaitForSeconds(jumpscareDuration);
        if (jumpscareObject != null) jumpscareObject.SetActive(false);
    }

    private IEnumerator ReEnableLocomotionAfterDelay()
    {
        yield return new WaitForSeconds(locomotionEnableDelay);
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        if (player != null) player.locomotionEnabledLayers = locomotionLayer;
    }

    private IEnumerator ReEnableGravityAfterDelay()
    {
        yield return new WaitForSeconds(gravityEnableDelay);
        if (playerRigidbody != null) playerRigidbody.useGravity = true;
    }

    #endregion

    #region Networked Monster Model

    private void StartKillModelSequence()
    {
        if (modelCoroutine != null) StopCoroutine(modelCoroutine);
        modelCoroutine = StartCoroutine(MonsterModelSequence());
    }

    private IEnumerator MonsterModelSequence()
    {
        networkKillModelActive = true;

        if (defaultMonsterModel != null) defaultMonsterModel.SetActive(false);
        if (killMonsterModel != null) killMonsterModel.SetActive(true);

        yield return new WaitForSeconds(killDuration);

        networkKillModelActive = false;
        if (killMonsterModel != null) killMonsterModel.SetActive(false);
        if (defaultMonsterModel != null) defaultMonsterModel.SetActive(true);

        if (aiPauseDuration > killDuration)
        {
            yield return new WaitForSeconds(aiPauseDuration - killDuration);
        }

        if (PhotonNetwork.IsMasterClient && horrorAI != null)
        {
            if (clearTargetAfterKill)
            {
                horrorAI.currentTarget = null;
                horrorAI.isChasing = false;
            }
            horrorAI.ResumeMovement();
        }
    }

    #endregion

    #region APIs

    public void ForceKill()
    {
        if (!isPerformingKill && player != null && !hasImmunity) TriggerKill();
    }

    public bool IsKilling() { return isPerformingKill; }
    public bool HasValidNetworking() { return hasValidPhotonView; }

    public void CancelKill()
    {
        StopAllCoroutines();
        
        networkKillModelActive = false;
        if (killMonsterModel != null) killMonsterModel.SetActive(false);
        if (defaultMonsterModel != null) defaultMonsterModel.SetActive(true);
        if (jumpscareObject != null) jumpscareObject.SetActive(false);

        if (player != null) player.locomotionEnabledLayers = locomotionLayer;
        if (playerRigidbody != null) playerRigidbody.useGravity = true;

        if (horrorAI != null && PhotonNetwork.IsMasterClient) horrorAI.ResumeMovement();

        isPerformingKill = false;
        OnKillFinished?.Invoke();
    }

    public void ClearImmunity()
    {
        hasImmunity = false;
        if (immunityCoroutine != null) StopCoroutine(immunityCoroutine);
    }

    public bool HasImmunity() { return hasImmunity; }
    
    public float GetRemainingImmunityTime()
    {
        if (!hasImmunity) return 0f;
        return Mathf.Max(0f, immunityEndTime - Time.time);
    }

    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(KillPlayerPlus))]
[InitializeOnLoad]
public class KillPlayerPlusEditor : Editor
{
    #region CustomIcon
    static KillPlayerPlusEditor()
    {
        EditorApplication.delayCall += AssignCustomIcon;
    }

    private static void AssignCustomIcon()
    {
        string iconPath = "Assets/HorrorAIPlus/Logo/KillPlayerPlusIcon.png";
        string[] guids = AssetDatabase.FindAssets("KillPlayerPlus t:MonoScript");

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

    private readonly string[] tabNames = { "General", "Player Settings", "Kill Settings" };

    public override void OnInspectorGUI()
    {
        KillPlayerPlus kill = (KillPlayerPlus)target;

        serializedObject.Update();

        EditorGUILayout.Space(5);
        DrawHeader();
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < tabNames.Length; i++)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            if (kill.currentEditorTab == i)
            {
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.cyan;
            }
            if (GUILayout.Button(tabNames[i], style, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                kill.currentEditorTab = i;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        switch (kill.currentEditorTab)
        {
            case 0: DrawGeneralTab(kill); break;
            case 1: DrawPlayerSettingsTab(kill); break;
            case 2: DrawKillSettingsTab(kill); break;
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(kill);
    }

    new void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HorrorAIPlus/Logo/KillPlayerPlusLogo.png");

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
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            EditorGUILayout.LabelField("Kill Player PLUS", titleStyle);
        }

        EditorGUILayout.LabelField("An enhanced player killing system for Horror AI Plus - Made by P1vr", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawGeneralTab(KillPlayerPlus kill)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("General Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        kill.horrorAI = (HorrorAIPlus)EditorGUILayout.ObjectField("Horror AI Plus", kill.horrorAI, typeof(HorrorAIPlus), true);
        kill.player = (Player)EditorGUILayout.ObjectField("GorillaPlayer", kill.player, typeof(Player), true);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Kill Detection", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        
        SerializedProperty playerTagsProp = serializedObject.FindProperty("playerTags");
        EditorGUILayout.PropertyField(playerTagsProp, new GUIContent("Player Tags"), true);
        
        EditorGUILayout.Space(5);
        kill.requireChasing = EditorGUILayout.Toggle("Require Chasing", kill.requireChasing);
        kill.killCooldown = EditorGUILayout.FloatField("Kill Cooldown", kill.killCooldown);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Target Clearing", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        kill.clearTargetAfterKill = EditorGUILayout.Toggle("Clear Target After Kill", kill.clearTargetAfterKill);
        kill.immunityDuration = EditorGUILayout.FloatField("Player Immunity Duration", kill.immunityDuration);
        EditorGUILayout.EndVertical();
    }
    
    void DrawPlayerSettingsTab(KillPlayerPlus kill)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Player Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Jumpscare", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        kill.enableJumpscare = EditorGUILayout.Toggle("Enable Jumpscare", kill.enableJumpscare);
        
        if (kill.enableJumpscare)
        {
            EditorGUILayout.Space(5);
            kill.jumpscareObject = (GameObject)EditorGUILayout.ObjectField("Jumpscare Object", kill.jumpscareObject, typeof(GameObject), true);
            kill.jumpscareDuration = EditorGUILayout.FloatField("Jumpscare Duration", kill.jumpscareDuration);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Respawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        kill.respawnPoint = (Transform)EditorGUILayout.ObjectField("Respawn Point", kill.respawnPoint, typeof(Transform), true);
        
        EditorGUILayout.Space(5);
        kill.locomotionLayer = LayerMaskField("Locomotion Layer", kill.locomotionLayer);
        kill.locomotionEnableDelay = EditorGUILayout.FloatField("Locomotion Enable Delay", kill.locomotionEnableDelay);
        
        EditorGUILayout.Space(5);
        kill.disableGravityDuringKill = EditorGUILayout.Toggle("Disable Gravity During Kill", kill.disableGravityDuringKill);
        if (kill.disableGravityDuringKill)
        {
            kill.gravityEnableDelay = EditorGUILayout.FloatField("Gravity Enable Delay", kill.gravityEnableDelay);
        }
        
        EditorGUILayout.EndVertical();
    }

    void DrawKillSettingsTab(KillPlayerPlus kill)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Kill Settings", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical("helpbox");
        kill.networkKill = EditorGUILayout.Toggle("Network Kill", kill.networkKill);
        
        if (kill.networkKill)
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Monster Models", EditorStyles.boldLabel);
            kill.defaultMonsterModel = (GameObject)EditorGUILayout.ObjectField("Default Monster Model", kill.defaultMonsterModel, typeof(GameObject), true);
            kill.killMonsterModel = (GameObject)EditorGUILayout.ObjectField("Kill Monster Model", kill.killMonsterModel, typeof(GameObject), true);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            kill.killDuration = EditorGUILayout.FloatField("Kill Model Duration", kill.killDuration);
            kill.aiPauseDuration = EditorGUILayout.FloatField("Stop Chasing Duration", kill.aiPauseDuration);
        }
        EditorGUILayout.EndVertical();
    }

    LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName)) 
            { 
                layers.Add(layerName); 
                layerNumbers.Add(i); 
            }
        }
        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++) 
            if (((1 << layerNumbers[i]) & layerMask.value) != 0) 
                maskWithoutEmpty |= (1 << i);
        
        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
        
        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++) 
            if ((maskWithoutEmpty & (1 << i)) != 0) 
                mask |= (1 << layerNumbers[i]);
        
        layerMask.value = mask;
        return layerMask;
    }
}
#endif