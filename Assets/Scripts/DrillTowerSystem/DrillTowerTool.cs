using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 钻探深度记录 - 记录每次钻探结束时的精确位置信息
/// </summary>
[System.Serializable]
public class DrillDepthRecord
{
    public float depth; // 深度（从地表算起）
    public Vector3 worldPosition; // 世界坐标位置
    public Vector3 direction; // 钻探方向
    public List<LayerInfo> layersAtDepth; // 该深度处的地层信息
    
    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;
        public float remainingThickness; // 该地层剩余厚度
        public Vector3 layerContactPoint; // 地层接触点
    }
}

/// <summary>
/// 可放置的钻塔工具 - 支持多层深度采集
/// 可以在同一位置进行多次采集：0-2m, 2-4m, 4-6m, 6-8m, 8-10m
/// 样本会围绕钻塔呈环形排列
/// </summary>
public class DrillTowerTool : PlaceableTool
{
    [Header("钻塔设置")]
    public GameObject drillTowerPrefab; // 钻塔预制件
    public float interactionRange = 3f; // 交互范围
    public int maxDrillDepths = 5; // 最大钻探次数
    public float depthPerDrill = 2f; // 每次钻探深度
    [Tooltip("教学流程不需要回收钻塔：关闭时不提示也不响应 G 键回收")]
    public bool allowRecall = false;
    
    [Header("样本排列")]
    public float sampleRingRadius = 2.5f; // 样本环形半径
    public float sampleElevation = 3.0f; // 样本悬浮高度，可在Inspector中调整
    public float sampleSpacing = 0.8f; // 样本间最小间距
    
    [Header("钻探效果")]
    public ParticleSystem drillingEffectPrefab; // 钻探粒子效果
    public AudioClip drillingSound; // 钻探音效
    public Material activeDrillMaterial; // 钻探中的材质
    public Material inactiveDrillMaterial; // 闲置状态材质
    
    public DrillTower placedTower; // 已放置的钻塔引用
    public bool IsPlayerNearTower => placedTower != null && GetPlayerCamera() != null &&
        Vector3.Distance(GetPlayerCamera().transform.position, placedTower.transform.position) <= interactionRange;
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasFKeyPressedLastFrame = false; // 上一帧F键状态
    
    protected override void Start()
    {
        base.Start();
        toolName = LocalizationManager.Resolve(
            "tool.drill_tower.name",
            "ボーリング装置（ドリルタワー）");

        // 设置预制件
        if (drillTowerPrefab != null)
        {
            prefabToPlace = drillTowerPrefab;
        }

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }

        // 场景切换后恢复钻塔引用
        if (hasPlacedObject && placedTower == null)
        {
            RestorePlacedTowerReference();
        }

        // 钻塔工具初始化完成
    }

    /// <summary>
    /// 场景切换后恢复已放置钻塔的引用
    /// </summary>
    void RestorePlacedTowerReference()
    {
        DrillTower[] towers = FindObjectsByType<DrillTower>(FindObjectsSortMode.None);
        if (towers.Length > 0)
        {
            // 找到属于此工具的钻塔
            foreach (var tower in towers)
            {
                if (tower.toolReference == null || tower.toolReference == this)
                {
                    placedTower = tower;
                    tower.Initialize(this);
                    Debug.Log("[DrillTowerTool] 场景切换后恢复钻塔引用");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 检测F键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsFKeyPressed()
    {
        // 键盘F键检测
        var keyboard = Keyboard.current;
        bool keyboardFPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;

        // 移动端F键检测
        bool mobileFPressed = false;
        if (mobileInputManager != null)
        {
            bool currentFKeyState = mobileInputManager.IsSecondaryInteracting;
            mobileFPressed = currentFKeyState && !wasFKeyPressedLastFrame;
            wasFKeyPressedLastFrame = currentFKeyState;
        }

        return keyboardFPressed || mobileFPressed;
    }
    
    protected override void Update()
    {
        base.Update();

        if (IsToolInputBlocked)
        {
            return;
        }
        
        // 检查与已放置钻塔的交互
        if (!isPlacementMode && hasPlacedObject && placedTower != null)
        {
            CheckTowerInteraction();
            CheckTowerRecall(); // 检查是否要收回钻塔
        }
    }
    
    /// <summary>
    /// 检查玩家是否可以与钻塔交互
    /// </summary>
    void CheckTowerInteraction()
    {
        if (placedTower == null) return;
        
        var camera = GetPlayerCamera();
        if (camera == null) return;
        float distance = Vector3.Distance(camera.transform.position, placedTower.transform.position);
        
        if (distance <= interactionRange)
        {
            // 显示交互提示 - 使用F键交互
            if (IsFKeyPressed())
            {
                InteractWithTower();
            }
        }
    }
    
    /// <summary>
    /// 检查是否要收回钻塔
    /// </summary>
    void CheckTowerRecall()
    {
        if (!allowRecall || placedTower == null) return;
        
        var camera = GetPlayerCamera();
        if (camera == null) return;
        float distance = Vector3.Distance(camera.transform.position, placedTower.transform.position);
        
        if (distance <= interactionRange)
        {
            // 按G键收回钻塔
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                RecallTower();
            }
        }
    }
    
    /// <summary>
    /// 收回钻塔
    /// </summary>
    void RecallTower()
    {
        if (placedTower == null)
        {
            return;
        }
        
        // 检查钻塔是否正在钻探
        if (placedTower.isDrilling)
        {
            return;
        }
        
        
        // 销毁所有采集的样本
        foreach (GameObject sample in placedTower.collectedSamples)
        {
            if (sample != null)
            {
                Destroy(sample);
            }
        }
        
        // 销毁钻塔对象
        if (placedTower.gameObject != null)
        {
            Destroy(placedTower.gameObject);
        }
        
        // 重置状态，允许重新放置
        placedTower = null;
        hasPlacedObject = false;
        canUse = true;
        
    }
    
    /// <summary>
    /// 与钻塔交互，进行钻探
    /// </summary>
    void InteractWithTower()
    {
        if (placedTower == null)
        {
            return;
        }
        
        if (placedTower.CanDrill())
        {
            placedTower.StartDrilling();
        }
        else if (placedTower.CanPutAway())
        {
            PutAwayTower();
        }
    }

    /// <summary>满深度后只收起设备，已生成的岩芯留在原地。</summary>
    void PutAwayTower()
    {
        if (placedTower == null || !placedTower.CanPutAway()) return;
        foreach (var sample in placedTower.collectedSamples)
        {
            if (sample != null && sample.transform.IsChildOf(placedTower.transform)) sample.transform.SetParent(null, true);
        }
        Destroy(placedTower.gameObject);
        placedTower = null;
        hasPlacedObject = false;
        canUse = true;
        UISystem.GameToast.Show(UISystem.GameUI.L("drill_tower.put_away_toast"));
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        base.OnObjectPlaced(placedObject);
        
        // 确保钻塔可见和物理效果正常
        placedObject.SetActive(true);
        
        // 确保物理组件正常工作
        Rigidbody rb = placedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true; // 确保重力开启
            rb.isKinematic = false; // 确保不是运动学刚体
        }
        
        // 获取或添加DrillTower组件
        placedTower = placedObject.GetComponent<DrillTower>();
        if (placedTower == null)
        {
            placedTower = placedObject.AddComponent<DrillTower>();
        }
        
        // 初始化钻塔
        placedTower.Initialize(this);

        // 架设三脚架式钻探装置，旁边放好岩芯箱
        BoringRigVisual rig = BoringRigVisual.Attach(placedObject, false);
        if (rig != null)
        {
            rig.PlayUnfold();
            placedTower.coreBox = CoreBox.Create(placedTower, depthPerDrill);
            GeoModel.AudioSystem.AudioManager.Instance.PlaySFX3D(
                GeoModel.AudioSystem.AudioKeys.SFX.MetalScrape, placedObject.transform.position, 0.6f);
        }
        else
        {
            // 没有装置模型时保留旧外观的可见性兜底
            FixTowerVisibility(placedObject);
        }
    }

    /// <summary>预览也显示完整的钻探装置和岩芯箱的位置。</summary>
    protected override void SetupPreviewObject()
    {
        if (previewObject != null)
        {
            BoringRigVisual.Attach(previewObject, true);
        }
        base.SetupPreviewObject();
    }

    /// <summary>钻探装置总是垂直架设（各支脚再分别落到地面），不随坡面倾斜。</summary>
    protected override Quaternion GetPlacementRotation(RaycastHit hit)
    {
        Vector3 forward = playerCamera != null
            ? Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up)
            : Vector3.forward;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
    
    /// <summary>
    /// 立即修复钻塔可见性
    /// </summary>
    void FixTowerVisibility(GameObject towerObj)
    {
        
        // 确保所有渲染器都启用且有正确材质
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
            
            // 如果材质有问题，创建新的可见材质
            if (renderer.material == null || renderer.material.color.a < 0.5f)
            {
                Material visibleMaterial = new Material(Shader.Find("Standard"));
                visibleMaterial.color = new Color(1f, 1f, 1f, 1f); // 默认白灰色
                renderer.material = visibleMaterial;
            }
        }
        
        // 确保缩放正常
        if (towerObj.transform.localScale.magnitude < 0.1f)
        {
            towerObj.transform.localScale = Vector3.one;
        }
        
    }
    
    /// <summary>
    /// 获取样本放置位置（环形排列，自动避免重叠）
    /// </summary>
    public Vector3 GetSamplePosition(Vector3 towerPosition, int drillIndex)
    {
        // 简化的确定性环形排列算法
        float angle = (drillIndex * 360f / maxDrillDepths) * Mathf.Deg2Rad;
        
        // 确定性的水平偏移
        Vector3 horizontalOffset = new Vector3(
            Mathf.Sin(angle) * sampleRingRadius,
            0,
            Mathf.Cos(angle) * sampleRingRadius
        );
        
        Vector3 targetPosition = towerPosition + horizontalOffset;
        
        // 简单的地面检测，确保样本悬浮在合适高度
        RaycastHit hit;
        if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit))
        {
            targetPosition.y = hit.point.y + sampleElevation;
        }
        else
        {
            targetPosition.y = towerPosition.y + sampleElevation;
        }
        
        
        return targetPosition;
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        if (placedTower != null)
        {
            // 绘制交互范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(placedTower.transform.position, interactionRange);
            
            // 绘制样本环形排列
            Gizmos.color = Color.green;
            for (int i = 0; i < maxDrillDepths; i++)
            {
                Vector3 samplePos = GetSamplePosition(placedTower.transform.position, i);
                Gizmos.DrawWireSphere(samplePos, 0.3f);
            }
        }
    }
}

/// <summary>
/// 钻塔组件 - 负责管理钻探逻辑
/// </summary>
public class DrillTower : MonoBehaviour
{
    [Header("钻塔状态")]
    public int currentDrillCount = 0;
    public bool isDrilling = false;
    public Vector3 drillingPosition;
    
    [Header("连续钻探记录")]
    public List<DrillDepthRecord> depthRecords = new List<DrillDepthRecord>(); // 记录每个深度点的信息
    
    public DrillTowerTool toolReference;
    public List<GameObject> collectedSamples = new List<GameObject>();
    [Tooltip("岩芯箱：提上来的岩芯按深度顺序放进去；收起钻塔后仍留在原地")]
    public CoreBox coreBox;
    private GameObject pendingCore; // 已切出、正在提钻或入箱途中的岩芯
    private int pendingCoreIndex;
    private Renderer towerRenderer;
    private AudioSource audioSource;
    private ParticleSystem drillingEffect;
    
    public int CurrentDrillCount => currentDrillCount;
    
    void Start()
    {
        drillingPosition = transform.position;
        towerRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        UpdateTowerAppearance();
    }
    
    public void Initialize(DrillTowerTool tool)
    {
        toolReference = tool;
    }

    /// <summary>
    /// 确保工具引用有效，场景切换后重新查找
    /// </summary>
    void EnsureToolReference()
    {
        if (toolReference == null)
        {
            // 场景切换后重新查找工具引用
            DrillTowerTool[] tools = FindObjectsByType<DrillTowerTool>(FindObjectsSortMode.None);
            foreach (var tool in tools)
            {
                if (tool.hasPlacedObject && tool.placedTower == this)
                {
                    toolReference = tool;
                    Debug.Log("[DrillTower] 重新找到工具引用");
                    break;
                }
            }

            if (toolReference == null)
            {
                Debug.LogWarning("[DrillTower] 无法找到对应的DrillTowerTool引用");
            }
        }
    }
    
    public bool CanDrill()
    {
        EnsureToolReference();
        return !isDrilling && toolReference != null && currentDrillCount < toolReference.maxDrillDepths;
    }
    
    public static bool IsReadyToPutAway(int count, int maximum, bool drilling) => !drilling && count >= maximum;

    public bool CanPutAway()
    {
        EnsureToolReference();
        return toolReference != null && IsReadyToPutAway(currentDrillCount, toolReference.maxDrillDepths, isDrilling);
    }

    public void StartDrilling()
    {
        if (!CanDrill()) return;
        
        StartCoroutine(DrillingProcess());
    }
    
    IEnumerator DrillingProcess()
    {
        isDrilling = true;
        UpdateTowerAppearance();

        // 播放钻探效果
        PlayDrillingEffects();

        var audio = GeoModel.AudioSystem.AudioManager.Instance;
        // 钻探循环音（挂在钻塔上的 3D 循环）
        var drillLoop = audio.StartLoopSFX3D(
            GeoModel.AudioSystem.AudioKeys.SFX.DrillLoop, transform);

        EnsureToolReference();
        if (toolReference == null)
        {
            if (drillLoop != null) audio.StopLoop(drillLoop, 0.2f);
            StopDrillingEffects();
            isDrilling = false;
            yield break;
        }

        float currentDepthStart = currentDrillCount * toolReference.depthPerDrill;
        float currentDepthEnd = currentDepthStart + toolReference.depthPerDrill;

        BoringRigVisual rig = GetComponent<BoringRigVisual>();
        if (rig != null && rig.IsBuilt)
        {
            // 钻进 → 提钻 → 岩芯入箱
            yield return StagedDrilling(rig, drillLoop, currentDepthStart, currentDepthEnd);
        }
        else
        {
            // 没有装置模型时沿用旧流程：等待后在钻塔周围生成岩芯
            yield return new WaitForSeconds(2.0f);
            PerformDrilling(currentDepthStart, currentDepthEnd, false);
            if (drillLoop != null) audio.StopLoop(drillLoop, 0.3f);
        }

        // 停止效果
        StopDrillingEffects();
        audio.PlaySFX3D(GeoModel.AudioSystem.AudioKeys.SFX.DrillComplete, transform.position);

        currentDrillCount++;
        isDrilling = false;
        UpdateTowerAppearance();

    }

    /// <summary>
    /// 一次完整的钻探：钻杆旋转下钻并显示深度，随后提钻，岩芯从孔里吊出来放进岩芯箱。
    /// 岩芯在开始时就切好（先不显示），所以深度计的颜色和孔口岩屑都跟着真实地层变化。
    /// </summary>
    IEnumerator StagedDrilling(BoringRigVisual rig, AudioSource drillLoop, float depthStart, float depthEnd)
    {
        var audio = GeoModel.AudioSystem.AudioManager.Instance;
        int drillIndex = currentDrillCount;
        rig.CompleteUnfold();
        if (coreBox == null)
        {
            coreBox = CoreBox.Create(this, toolReference.depthPerDrill);
        }

        GameObject core = PerformDrilling(depthStart, depthEnd, true);
        pendingCore = core;
        pendingCoreIndex = drillIndex;

        ReadCoreLayers(core, out Material[] layerMaterials, out Color[] layerColours, out float[] layerFractions);
        UISystem.DrillDepthGauge.Begin(depthStart, depthEnd, layerColours, layerFractions);

        // 1. 钻进
        rig.SetRunning(true);
        float loopPitch = drillLoop != null ? drillLoop.pitch : 1f;
        float elapsed = 0f;
        float nextCuttings = 0f;
        int lastSegment = -1;
        while (elapsed < BoringRigLayout.DrillSeconds)
        {
            float progress = elapsed / BoringRigLayout.DrillSeconds;
            rig.SetSwivelTop(BoringRigLayout.SwivelTopWhileDrilling(progress));
            UISystem.DrillDepthGauge.SetProgress(progress);

            int segment = BoringRigLayout.SegmentAt(progress, layerFractions);
            if (segment != lastSegment)
            {
                // 钻到新的地层：钻机声变调，并有碎石声
                if (drillLoop != null) drillLoop.pitch = loopPitch * (segment % 2 == 0 ? 1f : 0.88f);
                if (lastSegment >= 0)
                {
                    audio.PlaySFX3D(GeoModel.AudioSystem.AudioKeys.SFX.RockCrumble, rig.HoleWorldPosition, 0.5f);
                }
                lastSegment = segment;
            }

            if (elapsed >= nextCuttings)
            {
                // 孔口冒出当前地层颜色的岩屑
                nextCuttings += 0.12f;
                Material cuttings = segment >= 0 ? layerMaterials[segment] : null;
                DebrisBurst.Emit(rig.HoleWorldPosition + Vector3.up * 0.05f, Vector3.up, cuttings, 2, 1.3f, 0.028f, 0.55f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rig.SetSwivelTop(BoringRigLayout.SwivelDrilledTop);
        rig.SetRunning(false);
        UISystem.DrillDepthGauge.Finish();
        if (drillLoop != null) audio.StopLoop(drillLoop, 0.3f);
        audio.PlaySFX3D(GeoModel.AudioSystem.AudioKeys.SFX.MetalScrape, transform.position, 0.7f);

        // 2. 提钻：先把钻杆提起、钻机头沿导轨退开，让出孔口
        float liftSeconds = BoringRigLayout.PullSeconds * BoringRigLayout.RodLiftShare;
        elapsed = 0f;
        while (elapsed < liftSeconds)
        {
            float t = elapsed / liftSeconds;
            rig.SetSwivelTop(Mathf.Lerp(BoringRigLayout.SwivelDrilledTop, BoringRigLayout.SwivelPulledTop, Mathf.SmoothStep(0f, 1f, t)));
            rig.SetHeadRetract(t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rig.SetSwivelTop(BoringRigLayout.SwivelPulledTop);
        rig.SetHeadRetract(1f);
        rig.SetRodVisible(false);

        float hoistSeconds = BoringRigLayout.PullSeconds - liftSeconds;
        if (core != null)
        {
            // 岩芯从孔里被吊出来
            SetCoreRenderersEnabled(core, true);
            Vector3 hole = rig.HoleWorldPosition + Vector3.down * 0.05f;
            elapsed = 0f;
            while (elapsed < hoistSeconds && core != null)
            {
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / hoistSeconds) * Mathf.PI * 0.5f);
                Vector3 top = Vector3.Lerp(hole, rig.SwivelBottomWorld, t);
                core.transform.SetPositionAndRotation(top, Quaternion.identity);
                rig.SetSling(top);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rig.SetSling(null);

            // 3. 放倒，送进岩芯箱里对应深度的那一格
            Vector3 from = rig.SwivelBottomWorld;
            CoreRestPose(drillIndex, out Vector3 to, out Quaternion lying);
            elapsed = 0f;
            while (elapsed < BoringRigLayout.BoxSeconds && core != null)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / BoringRigLayout.BoxSeconds);
                core.transform.SetPositionAndRotation(
                    BoringRigLayout.CoreTransferPoint(from, to, t), Quaternion.Slerp(Quaternion.identity, lying, t));
                rig.SetHeadRetract(1f - t);
                rig.SetSwivelTop(Mathf.Lerp(BoringRigLayout.SwivelPulledTop, BoringRigLayout.SwivelIdleTop, t));
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(hoistSeconds + BoringRigLayout.BoxSeconds);
        }

        rig.SetHeadRetract(0f);
        rig.SetSwivelTop(BoringRigLayout.SwivelIdleTop);
        rig.SetRodVisible(true);
        FinishPendingCore();
    }

    /// <summary>岩芯在箱中的位置和朝向；没有岩芯箱模型时放在同一位置的地面上。</summary>
    void CoreRestPose(int drillIndex, out Vector3 position, out Quaternion rotation)
    {
        if (coreBox != null)
        {
            position = coreBox.SlotPosition(drillIndex);
            rotation = coreBox.CoreRotation;
            return;
        }

        position = transform.TransformPoint(BoringRigLayout.CoreBoxOffset + BoringRigLayout.SlotLocalPosition(drillIndex));
        rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * BoringRigLayout.CoreLocalRotation;
    }

    /// <summary>刚切出的岩芯先藏在孔里：不显示、不碰撞、不可拾取，也不走掉落物理。</summary>
    void HidePendingCore(GameObject core)
    {
        GeometricSampleFloating floating = core.GetComponent<GeometricSampleFloating>();
        if (floating != null)
        {
            // 在它的 Start 之前移除，避免给岩芯加上掉落控制器。
            floating.enabled = false;
            Destroy(floating);
        }

        Rigidbody body = core.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
        }

        SetCoreRenderersEnabled(core, false);
        foreach (Collider coreCollider in core.GetComponentsInChildren<Collider>(true))
        {
            coreCollider.enabled = false;
        }
    }

    static void SetCoreRenderersEnabled(GameObject core, bool visible)
    {
        foreach (Renderer coreRenderer in core.GetComponentsInChildren<Renderer>(true))
        {
            coreRenderer.enabled = visible;
        }
    }

    /// <summary>岩芯入箱：恢复显示和碰撞，成为可拾取的样本。</summary>
    void FinishPendingCore()
    {
        GameObject core = pendingCore;
        pendingCore = null;
        if (core == null) return;

        CoreRestPose(pendingCoreIndex, out Vector3 position, out Quaternion rotation);
        core.transform.SetPositionAndRotation(position, rotation);
        if (coreBox != null) coreBox.Place(core, pendingCoreIndex);

        SetCoreRenderersEnabled(core, true);
        foreach (Collider coreCollider in core.GetComponentsInChildren<Collider>(true))
        {
            coreCollider.enabled = true;
        }

        // 给根对象一个贴合岩芯的小触发器，SampleCollector 就不会再加一个半径数米的球形触发器。
        if (core.GetComponent<Collider>() == null)
        {
            BoxCollider reach = core.AddComponent<BoxCollider>();
            reach.isTrigger = true;
            float length = toolReference != null ? toolReference.depthPerDrill : 2f;
            reach.center = new Vector3(0f, -length * 0.5f, 0f);
            reach.size = new Vector3(BoringRigLayout.CoreRadius * 2f, length, BoringRigLayout.CoreRadius * 2f);
        }

        SampleCollector collector = core.GetComponent<SampleCollector>();
        if (collector == null)
        {
            collector = core.AddComponent<SampleCollector>();
            collector.sourceToolID = "1001";
        }
        // 岩芯平放后有 2m 长，从箱子周围任何一侧都要能拾取。
        collector.interactionRange = BoringRigLayout.BoxedCorePickupRange;
        collector.enabled = true;

        collectedSamples.Add(core);
    }

    /// <summary>岩芯各地层段（自上而下）的材质、颜色和所占比例。</summary>
    static void ReadCoreLayers(GameObject core, out Material[] materials, out Color[] colours, out float[] fractions)
    {
        var materialList = new List<Material>();
        var colourList = new List<Color>();
        var lengthList = new List<float>();
        if (core != null)
        {
            foreach (Transform segment in core.transform)
            {
                MeshRenderer segmentRenderer = segment.GetComponent<MeshRenderer>();
                MeshFilter segmentMesh = segment.GetComponent<MeshFilter>();
                if (segmentRenderer == null || segmentMesh == null || segmentMesh.sharedMesh == null) continue;
                materialList.Add(segmentRenderer.sharedMaterial);
                colourList.Add(FieldToolModels.ReadColor(segmentRenderer.sharedMaterial, new Color(0.6f, 0.55f, 0.5f)));
                lengthList.Add(Mathf.Max(0.001f, segmentMesh.sharedMesh.bounds.size.y * segment.lossyScale.y));
            }
        }

        float total = 0f;
        foreach (float length in lengthList) total += length;
        materials = materialList.ToArray();
        colours = colourList.ToArray();
        fractions = new float[lengthList.Count];
        for (int i = 0; i < fractions.Length; i++)
        {
            fractions[i] = total > 0f ? lengthList[i] / total : 1f / fractions.Length;
        }
    }

    void OnDestroy()
    {
        // 钻塔在提钻途中被移除（例如切换场景）时，岩芯直接入箱，不会丢失。
        if (pendingCore != null) FinishPendingCore();
        if (isDrilling) UISystem.DrillDepthGauge.Hide();
    }
    
    /// <param name="intoCoreBox">true：岩芯先藏在孔里，由提钻动画送进岩芯箱；false：旧流程，在钻塔周围掉落。</param>
    GameObject PerformDrilling(float depthStart, float depthEnd, bool intoCoreBox)
    {
        EnsureToolReference();
        if (toolReference == null) return null;

        // 计算样本位置（旧流程：环形排列）
        Vector3 samplePosition = intoCoreBox
            ? drillingPosition
            : toolReference.GetSamplePosition(drillingPosition, currentDrillCount);
        
        // 🚀 使用集成了v1.3检测的几何重建系统进行钻探
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            reconstructor = gameObject.AddComponent<GeometricSampleReconstructor>();
        }
        
        Vector3 actualDrillingStart;
        Vector3 drillingDirection = Vector3.down;
        float actualDrillingDepth;
        float drillingRadius = 0.1f;
        
        // 🔧 修复钻探起点计算：确保从真正的地表开始钻探
        // 核心问题：原来的起点计算低于地层表面，导致只检测到深层地层
        
        // 第1步：找到钻塔水平位置上的最高地层表面
        Vector3 horizontalPosition = new Vector3(drillingPosition.x, 0, drillingPosition.z);
        Vector3 skyPosition = horizontalPosition + Vector3.up * 50f; // 从高空开始检测
        
        RaycastHit[] allHits = Physics.RaycastAll(skyPosition, Vector3.down, 100f);
        
        // 🔧 验证：检查钻塔脚下的实际地表位置
        Vector3 towerBase = drillingPosition + Vector3.down * 0.5f; // 钻塔底部位置
        RaycastHit[] nearbyHits = Physics.RaycastAll(towerBase + Vector3.up * 2f, Vector3.down, 5f);
        
        // 🔧 找到最高的地层表面（确保从真正的地表开始）
        GeologyLayer highestLayer = null;
        float highestY = float.MinValue;
        Vector3 surfacePoint = drillingPosition; // 默认值
        
        
        foreach (RaycastHit hit in allHits)
        {
            // 跳过钻塔自身的组件
            if (hit.collider.name.Contains("DrillTower") || 
                hit.collider.name.Contains("Tower") || 
                hit.collider.name.Contains("Drill") ||
                hit.collider.name.Contains("Glow"))
            {
                continue;
            }
            
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                // 选择Y坐标最高的地层表面（真正的地表）
                if (hit.point.y > highestY)
                {
                    highestY = hit.point.y;
                    highestLayer = geoLayer;
                    surfacePoint = hit.point;
                }
            }
        }
        
        // 🔧 验证并比较钻塔脚下的地表
        GeologyLayer nearestLayer = null;
        float nearestY = float.MinValue;
        Vector3 nearestSurfacePoint = surfacePoint;
        
        foreach (RaycastHit hit in nearbyHits)
        {
            if (hit.collider.name.Contains("DrillTower") || 
                hit.collider.name.Contains("Tower") || 
                hit.collider.name.Contains("Drill") ||
                hit.collider.name.Contains("Glow"))
            {
                continue;
            }
            
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null && hit.point.y > nearestY)
            {
                nearestY = hit.point.y;
                nearestLayer = geoLayer;
                nearestSurfacePoint = hit.point;
            }
        }
        
        // 🔧 选择最合适的地表位置
        if (highestLayer != null && nearestLayer != null)
        {
            float heightDifference = Mathf.Abs(highestY - nearestY);
            if (heightDifference > 0.5f) // 如果高度差超过0.5米，报告差异
            {
                // 使用钻塔脚下的地表位置，更准确
                surfacePoint = nearestSurfacePoint;
            }
            else
            {
                // 高度差异不大，使用原来的最高地表
            }
        }
        else if (highestLayer != null)
        {
        }
        else if (nearestLayer != null)
        {
            surfacePoint = nearestSurfacePoint;
        }
        else
        {
            surfacePoint = drillingPosition;
        }
        
        // 第2步：从真正的地表开始钻探
        actualDrillingStart = surfacePoint; // 🔧 关键修复：使用真正的地表位置
        actualDrillingDepth = depthEnd; // 使用目标深度
        if (intoCoreBox)
        {
            // 岩芯就在孔里生成（顶端略低于地表），等提钻时再出现。
            samplePosition = surfacePoint + Vector3.down * 0.05f;
        }
        
        
        GeometricSampleReconstructor.ReconstructedSample geometricSample;
        
        // 使用6参数版本，传递正确的深度范围
        geometricSample = reconstructor.ReconstructSample(
            actualDrillingStart,
            drillingDirection,
            drillingRadius,
            actualDrillingDepth,
            samplePosition,
            depthStart,
            depthEnd
        );
        
        if (geometricSample != null && geometricSample.sampleContainer != null)
        {
            if (intoCoreBox)
            {
                HidePendingCore(geometricSample.sampleContainer);
            }
            else
            {
                collectedSamples.Add(geometricSample.sampleContainer);
            }
            
            // 统一的样本组成分析（所有层都使用）
            
            if (geometricSample.layerSegments != null)
            {
                for (int i = 0; i < geometricSample.layerSegments.Length; i++)
                {
                    var segment = geometricSample.layerSegments[i];
                    if (segment != null && segment.sourceLayer != null)
                    {
                    }
                }
            }
            
            // 记录本次钻探结束时的深度信息
            RecordDepthInfo(depthEnd);
            
            // 设置样本标识
            DepthSampleMarker marker = geometricSample.sampleContainer.AddComponent<DepthSampleMarker>();
            marker.depthStart = depthStart;
            marker.depthEnd = depthEnd;
            marker.drillIndex = currentDrillCount;
            marker.collectionPosition = actualDrillingStart; // 设置采集位置
            marker.parentTower = this;
            
            // 立即为生成的样本添加收集组件（按需集成）
            DrillToolSampleIntegrator.IntegrateSampleAfterDrilling(
                geometricSample.sampleContainer, 
                "1001", // DrillTowerTool的ID
                $"钻塔钻探_{currentDrillCount}"
            );

            if (intoCoreBox)
            {
                // 入箱之前不可拾取（SampleCollector 的 Start 也推迟到那时）。
                SampleCollector collector = geometricSample.sampleContainer.GetComponent<SampleCollector>();
                if (collector != null) collector.enabled = false;
            }

            return geometricSample.sampleContainer;
        }

        return null;
    }
    
    /// <summary>
    /// 记录钻探深度信息
    /// </summary>
    void RecordDepthInfo(float totalDepthFromSurface)
    {
        // 计算从地表算起的绝对位置
        Vector3 depthPosition = drillingPosition + Vector3.down * totalDepthFromSurface;
        
        DrillDepthRecord record = new DrillDepthRecord
        {
            depth = totalDepthFromSurface,
            worldPosition = depthPosition,
            direction = Vector3.down,
            layersAtDepth = new List<DrillDepthRecord.LayerInfo>()
        };
        
        depthRecords.Add(record);
        
    }
    
    // 移除了AddSampleLabel方法，不再显示深度标签
    // void AddSampleLabel(GameObject sample, float depthStart, float depthEnd)
    // {
    //     // 创建样本标签
    //     GameObject labelObj = new GameObject("SampleLabel");
    //     labelObj.transform.SetParent(sample.transform);
    //     labelObj.transform.localPosition = Vector3.up * 0.5f;
    //     
    //     TextMesh textMesh = labelObj.AddComponent<TextMesh>();
    //     textMesh.text = $"{depthStart:F1}m-{depthEnd:F1}m";
    //     textMesh.fontSize = 20;
    //     textMesh.color = Color.white;
    //     textMesh.anchor = TextAnchor.MiddleCenter;
    //     
    //     // 让文字始终面向玩家
    //     RotateTowardsPlayer rotateScript = labelObj.AddComponent<RotateTowardsPlayer>();
    // }
    
    void PlayDrillingEffects()
    {
        EnsureToolReference();
        if (toolReference == null) return;

        // 播放钻探音效
        if (audioSource != null && toolReference.drillingSound != null)
        {
            audioSource.clip = toolReference.drillingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 播放粒子效果
        if (toolReference.drillingEffectPrefab != null)
        {
            drillingEffect = Instantiate(toolReference.drillingEffectPrefab, transform.position, Quaternion.identity);
            drillingEffect.transform.SetParent(transform);
        }
    }
    
    void StopDrillingEffects()
    {
        // 停止音效
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 停止粒子效果
        if (drillingEffect != null)
        {
            drillingEffect.Stop();
            Destroy(drillingEffect.gameObject, 2f);
        }
    }
    
    void UpdateTowerAppearance()
    {
        if (towerRenderer == null) return;

        EnsureToolReference();
        if (toolReference == null) return;

        // 根据状态更新材质
        if (isDrilling && toolReference.activeDrillMaterial != null)
        {
            towerRenderer.material = toolReference.activeDrillMaterial;
        }
        else if (toolReference.inactiveDrillMaterial != null)
        {
            towerRenderer.material = toolReference.inactiveDrillMaterial;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (toolReference != null)
        {
            // 绘制已钻探的深度层
            Gizmos.color = Color.red;
            for (int i = 0; i < currentDrillCount; i++)
            {
                float depth = i * toolReference.depthPerDrill;
                Vector3 layerPosition = drillingPosition + Vector3.down * depth;
                Gizmos.DrawWireCube(layerPosition, new Vector3(0.4f, 0.1f, 0.4f));
            }
            
            // 绘制下一层钻探位置
            if (currentDrillCount < toolReference.maxDrillDepths)
            {
                Gizmos.color = Color.yellow;
                float nextDepth = currentDrillCount * toolReference.depthPerDrill;
                Vector3 nextPosition = drillingPosition + Vector3.down * nextDepth;
                Gizmos.DrawWireCube(nextPosition, new Vector3(0.4f, 0.1f, 0.4f));
            }
        }
    }
}

/// <summary>
/// 深度样本标记组件 - 用于标识来自钻塔的特定深度样本
/// </summary>
public class DepthSampleMarker : MonoBehaviour
{
    [Header("深度信息")]
    public float depthStart;
    public float depthEnd;
    public int drillIndex;
    
    [Header("位置信息")]
    public Vector3 collectionPosition; // 采集点位置（用于世界坐标计算）
    
    [Header("钻塔引用")]
    public DrillTower parentTower;
    
    [Header("显示设置")]
    public bool showDepthInfo = false; // 默认不显示深度信息
    public Color depthLabelColor = Color.white;
    
    void Start()
    {
        if (showDepthInfo)
        {
            UpdateDepthDisplay();
        }
    }
    
    void UpdateDepthDisplay()
    {
        // 不显示深度信息，深度数据仅保存在组件中
        // TextMesh textMesh = GetComponentInChildren<TextMesh>();
        // if (textMesh != null)
        // {
        //     textMesh.text = $"第{drillIndex + 1}次\n{depthStart:F1}m-{depthEnd:F1}m";
        //     textMesh.color = depthLabelColor;
        // }
    }
    
    /// <summary>
    /// 获取样本描述信息
    /// </summary>
    public string GetSampleDescription()
    {
        float thickness = depthEnd - depthStart;
        
        // 使用世界坐标深度计算
        if (collectionPosition != Vector3.zero)
        {
            string depthInfo = WorldDepthCalculator.GetLocalizedDepthDescription(
                collectionPosition, depthStart, depthEnd, true);
            
            var localizationManager = LocalizationManager.Instance;
            if (localizationManager != null)
            {
                return localizationManager.GetText("sample.drill_tower.description", 
                    (drillIndex + 1).ToString(), depthInfo, thickness.ToString("F1"));
            }
            return $"ボーリングコア #{drillIndex + 1}\n地表からの深さ：{depthStart:F1}m～{depthEnd:F1}m\n厚さ：{thickness:F1}m";
        }
        else
        {
            // 如果没有位置信息，使用相对深度
            return $"ボーリングコア #{drillIndex + 1}\n地表からの深さ：{depthStart:F1}m～{depthEnd:F1}m\n厚さ：{thickness:F1}m";
        }
    }
    
}
