using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    [Header("Hero Movement")]
    NavMeshAgent agent;
    Camera playerCamera;

    [Header("Hero Combat")]
    public float attackRange = 2f;
    public int attackDamage = 15;
    public float attackSpeed = 1.5f;

    [Header("Hero Abilities")]
    [Tooltip("Damage dealt by a single ranged projectile shot")] public int rangedDamage = 30;
    [Tooltip("Speed of hero projectile (units per second)")] public float projectileSpeed = 25f;
    [Tooltip("Max range for ranged ability lock and shot")] public float rangedRange = 25f;
    [Tooltip("Area blast damage")] public int areaDamage = 60;
    [Tooltip("Radius for area blast")] public float areaRadius = 8f;
    [Tooltip("Cooldown in seconds for ranged shot (E)")] public float rangedCooldown = 1.0f;
    [Tooltip("Cooldown in seconds for area blast")] public float areaCooldown = 5f;

    [Header("AOE Visual")] 
    [Tooltip("Ring color for Q blast visual")] public Color aoeRingColor = new Color(0.2f, 1f, 1f, 0.85f);
    [Tooltip("How long the ring expands/fades")] public float aoeRingDuration = 0.35f;
    [Tooltip("Line width of the ring")] public float aoeRingWidth = 0.15f;
    [Tooltip("Segments for the ring circle")] [Range(8,128)] public int aoeRingSegments = 64;

    [Header("Hero UI")]
    [Tooltip("Left offset (pixels) for ability boxes")] public float uiX = 10f;
    [Tooltip("Top offset (pixels) for ability boxes")] public float uiY = 300f;
    [Tooltip("Width of ability boxes")] public float uiBoxWidth = 180f;
    [Tooltip("Height of ability boxes")] public float uiBoxHeight = 60f;
    [Tooltip("Vertical spacing between boxes")] public float uiSpacing = 10f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip rangedShotClip; // E fire
    public AudioClip aoeClip;        // Q blast
    public AudioClip heroAttackClip; // melee/auto-attack sound
    [Range(0f,1f)] public float sfxVolume = 1f;

    private GameObject currentTarget;
    private GameObject lockedTarget; // E ability lock target
    private float lastAttackTime;
    private float lastAreaBlastTime = -Mathf.Infinity;
    private float lastRangedShotTime = -Mathf.Infinity;

    private HeroAnimationController heroAnim;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCamera = Camera.main;

        // Ensure SFX source for hero abilities
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D hero SFX by default
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }

        // Autoload default clips from Resources if not assigned
        if (rangedShotClip == null)
        {
            rangedShotClip = Resources.Load<AudioClip>("Audio/hero_shot");
        }
        if (aoeClip == null)
        {
            aoeClip = Resources.Load<AudioClip>("Audio/hero_blast");
        }
        if (heroAttackClip == null)
        {
            heroAttackClip = Resources.Load<AudioClip>("Audio/hero_attack");
        }

        // Acquire animation bridge and hook death animation
        heroAnim = GetComponentInChildren<HeroAnimationController>();
        var hh = GetComponent<HeroHealth>();
        if (hh != null)
        {
            HeroHealth.OnHeroDied.AddListener(() => heroAnim?.PlayDie());
        }
    }

    void Update()
    {
        // Right click for movement || combat
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }

        // Special abilities (remapped to 1 and 2)
        if (Keyboard.current != null)
        {
            if ((Keyboard.current.digit1Key != null && Keyboard.current.digit1Key.wasPressedThisFrame) ||
                (Keyboard.current.numpad1Key != null && Keyboard.current.numpad1Key.wasPressedThisFrame))
            {
                TryAreaBlast();
            }
            if ((Keyboard.current.digit2Key != null && Keyboard.current.digit2Key.wasPressedThisFrame) ||
                (Keyboard.current.numpad2Key != null && Keyboard.current.numpad2Key.wasPressedThisFrame))
            {
                TryRangedShot();
            }
        }

        // Drive locomotion animation by agent velocity
        if (agent != null && heroAnim != null)
        {
            float norm = (agent.speed > 0f) ? agent.velocity.magnitude / agent.speed : 0f;
            heroAnim.SetMoveSpeed(norm);
        }
        
        // Validate E-locked target range and existence (clear when out of range or destroyed)
        if (lockedTarget != null)
        {
            if (lockedTarget == null)
            {
                ClearLockedTarget("Destroyed");
            }
            else
            {
                float lockDist = Vector3.Distance(transform.position, lockedTarget.transform.position);
                if (lockDist > rangedRange)
                {
                    ClearLockedTarget("Out of range");
                }
            }
        }

        // auto-attack logic (unchanged)
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distance <= attackRange)
            {
                // if in range, attack continuously
                if (Time.time - lastAttackTime >= 1f / attackSpeed)
                {
                    PerformAttack();
                }
            }
            else
            {
                // too far, needs to be closer
                agent.SetDestination(currentTarget.transform.position);
            }
        }
    }

    void HandleRightClick()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.CompareTag("Enemy"))
            {
                AttackEnemy(hit.collider.gameObject);
            }
            // check if there's walkable ground and nothing on the path
            else if (IsWalkableGround(hit.collider))
            {
                // ensure NavMeshAgent can reach destination
                if (agent.enabled && IsValidDestination(hit.point))
                {
                    agent.SetDestination(hit.point);
                    SpawnMoveMarker(hit.point);
                    Debug.Log("Debug going to " + hit.point);
                }
                else
                {
                    Debug.Log("No way to walk");
                }
            }
            else
            {
                Debug.Log("Can't walk to " + hit.collider.name);
            }
        }
    }

    void AttackEnemy(GameObject enemy)
    {
        currentTarget = enemy;
        Debug.Log("HERO targeting " + enemy.name + " for auto-attack.");
    }

    void PerformAttack()
    {
        if (currentTarget != null)
        {
            EnemyHP enemyHP = currentTarget.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(attackDamage);
                if (sfxSource != null && heroAttackClip != null) sfxSource.PlayOneShot(heroAttackClip, Mathf.Clamp01(sfxVolume));
                heroAnim?.PlayAttack();
                lastAttackTime = Time.time;
                Debug.Log($"Hero deals {attackDamage} damage to {currentTarget.name}");

                // clear target upon destruction
                if (currentTarget == null)
                {
                    currentTarget = null;
                }
            }
            else
            {
                currentTarget = null;
            }
        }   
    }
    
    // helper functions for hero movement
    bool IsWalkableGround(Collider hitCollider)
    {
        // check layer instead of name
        return hitCollider.gameObject.layer == LayerMask.NameToLayer("Ground");
    }

    bool IsValidDestination(Vector3 destination)
    {
        // check if NavMesh can reach the click point destination
        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    void TryRangedShot()
    {
        // First, if we don't have a locked target, attempt to lock one under the cursor (or fallback to currentTarget)
        if (lockedTarget == null)
        {
            GameObject underCursor = GetEnemyUnderCursor();
            GameObject candidate = underCursor != null ? underCursor : currentTarget;

            if (candidate == null)
            {
                Debug.Log("Press 2 while pointing at an enemy to lock target.");
                return;
            }

            // Only lock if within ranged range
            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist <= rangedRange)
            {
                lockedTarget = candidate;
                Debug.Log($"Locked target: {lockedTarget.name}. Press 2 again to fire.");
                return; // Do not fire on the first press; only lock
            }
            else
            {
                Debug.Log("Target out of ranged ability range—move closer to lock.");
                return;
            }
        }

        // We have a locked target; respect cooldown and range
        if (Time.time - lastRangedShotTime < rangedCooldown)
        {
            float remain = Mathf.Max(0f, rangedCooldown - (Time.time - lastRangedShotTime));
            Debug.Log($"Ranged shot on cooldown: {remain:F1}s remaining");
            return;
        }

        if (lockedTarget == null)
        {
            Debug.Log("Locked target lost.");
            return;
        }

        float d = Vector3.Distance(transform.position, lockedTarget.transform.position);
        if (d > rangedRange)
        {
            ClearLockedTarget("Out of range");
            return;
        }

        FireRangedShot(lockedTarget);
        heroAnim?.PlayShoot();
        if (sfxSource != null && rangedShotClip != null) sfxSource.PlayOneShot(rangedShotClip, Mathf.Clamp01(sfxVolume));
        lastRangedShotTime = Time.time;
    }

    void FireRangedShot(GameObject enemy)
    {
        if (enemy == null) return;

        // Create a simple sphere projectile at hero position
        GameObject projGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projGO.name = "HeroProjectile";
        projGO.transform.position = transform.position + Vector3.up * 1f;

        // Optional visual tweak
        var renderer = projGO.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = Color.cyan;

        var projectile = projGO.AddComponent<Projectile>();
        projectile.Initialize(enemy.transform, rangedDamage, projectileSpeed);
    }

    void TryAreaBlast()
    {
        if (Time.time - lastAreaBlastTime < areaCooldown)
        {
            float remain = Mathf.Max(0f, areaCooldown - (Time.time - lastAreaBlastTime));
            Debug.Log($"Area blast on cooldown: {remain:F1}s remaining");
            return;
        }
        PerformAreaBlast();
        lastAreaBlastTime = Time.time;
    }

    void PerformAreaBlast()
    {
        // Visual feedback
        heroAnim?.PlayAOE();
        StartCoroutine(FlashColor(Color.cyan, 0.15f));
        SpawnAoeRing();

        Collider[] hits = Physics.OverlapSphere(transform.position, areaRadius);
        int hitCount = 0;
        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != null && hit.gameObject.CompareTag("Enemy"))
            {
                var hp = hit.gameObject.GetComponent<EnemyHP>();
                if (hp != null)
                {
                    hp.TakeDamage(areaDamage);
                    hitCount++;
                }
            }
        }
        if (sfxSource != null && aoeClip != null) sfxSource.PlayOneShot(aoeClip, Mathf.Clamp01(sfxVolume));
        Debug.Log($"Area blast hit {hitCount} enemies for {areaDamage} damage (radius {areaRadius}).");
    }

    void SpawnAoeRing()
    {
        // Build a transient ring using LineRenderer
        var go = new GameObject("AOE_Ring", typeof(LineRenderer));
        go.transform.position = new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);
        var lr = go.GetComponent<LineRenderer>();
        BuildRingRenderer(lr, 0.1f);
        StartCoroutine(AoeRingRoutine(lr, areaRadius, Mathf.Max(0.05f, aoeRingDuration)));
    }

    System.Collections.IEnumerator AoeRingRoutine(LineRenderer lr, float targetRadius, float duration)
    {
        float t = 0f;
        float startRadius = 0.1f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float r = Mathf.Lerp(startRadius, targetRadius, 1f - Mathf.Pow(1f - u, 3f));
            UpdateRing(lr, r);
            SetLineRendererColor(lr, new Color(aoeRingColor.r, aoeRingColor.g, aoeRingColor.b, Mathf.Lerp(aoeRingColor.a, 0f, u)));
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    void BuildRingRenderer(LineRenderer lr, float initialRadius)
    {
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = Mathf.Max(0.01f, aoeRingWidth);
        lr.positionCount = Mathf.Clamp(aoeRingSegments, 8, 256);
        // Material that supports color/alpha without needing textures
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        SetLineRendererColor(lr, aoeRingColor);
        UpdateRing(lr, initialRadius);
    }

    void UpdateRing(LineRenderer lr, float radius)
    {
        int count = lr.positionCount;
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    void SetLineRendererColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    // Right-click move marker (a small fading cross on the ground)
    void SpawnMoveMarker(Vector3 pos)
    {
        float r = 0.75f;
        float w = 0.06f;
        Color col = new Color(0.2f, 1f, 1f, 0.9f);

        var go = new GameObject("MoveMarker");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.position = new Vector3(pos.x, pos.y + 0.05f, pos.z);

        // Two line renderers forming an X
        var lr1 = go.AddComponent<LineRenderer>();
        ConfigureLR(lr1, w, col);
        lr1.positionCount = 2;
        lr1.useWorldSpace = false;
        lr1.SetPosition(0, new Vector3(-r, 0f, -r));
        lr1.SetPosition(1, new Vector3(r, 0f, r));

        var lr2 = new GameObject("Line2").AddComponent<LineRenderer>();
        lr2.transform.SetParent(go.transform, false);
        ConfigureLR(lr2, w, col);
        lr2.positionCount = 2;
        lr2.useWorldSpace = false;
        lr2.SetPosition(0, new Vector3(-r, 0f, r));
        lr2.SetPosition(1, new Vector3(r, 0f, -r));

        StartCoroutine(FadeAndScaleMarker(go, new [] { lr1, lr2 }, 0.35f));
    }

    void ConfigureLR(LineRenderer lr, float width, Color color)
    {
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 0;
    }

    System.Collections.IEnumerator FadeAndScaleMarker(GameObject go, LineRenderer[] lines, float duration)
    {
        float t = 0f;
        var start = go.transform.localScale;
        var end = start * 1.4f;
        Color startCol = lines[0].startColor;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            // Ease out
            float e = 1f - Mathf.Pow(1f - u, 3f);
            go.transform.localScale = Vector3.Lerp(start, end, e);
            Color c = new Color(startCol.r, startCol.g, startCol.b, Mathf.Lerp(startCol.a, 0f, e));
            foreach (var lr in lines)
            {
                lr.startColor = c;
                lr.endColor = c;
            }
            yield return null;
        }
        Destroy(go);
    }

    System.Collections.IEnumerator FlashColor(Color flashColor, float duration)
    {
        var r = GetComponent<Renderer>();
        if (r == null) yield break;

        Color original = r.material.color;
        r.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        r.material.color = original;
    }

    GameObject GetEnemyUnderCursor()
    {
        if (playerCamera == null || Mouse.current == null) return null;
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("Enemy"))
            {
                return hit.collider.gameObject;
            }
        }
        return null;
    }

    void ClearLockedTarget(string reason = "")
    {
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log($"Locked target cleared: {reason}");
        }
        lockedTarget = null;
    }

    void OnGUI()
    {
        // Simple, adjustable indicators on left side
        float boxWidth = uiBoxWidth;
        float boxHeight = uiBoxHeight;
        float y = uiY;
        float x = uiX;

        // 1 - AOE
        Rect qRect = new Rect(x, y, boxWidth, boxHeight);
        GUI.Box(qRect, "1 - AOE Blast");
        float areaElapsed = Time.time - lastAreaBlastTime;
        bool areaReady = areaElapsed >= areaCooldown;
        float areaRemaining = Mathf.Max(0f, areaCooldown - areaElapsed);
        string qText = areaReady ? "Ready" : $"Cooldown: {Mathf.Ceil(areaRemaining)}s";
        GUI.Label(new Rect(qRect.x + 10, qRect.y + 20, qRect.width - 20, 20), qText);
        // progress bar (fill = readiness)
        float areaFill = Mathf.Clamp01(areaElapsed / Mathf.Max(0.0001f, areaCooldown));
        GUI.Box(new Rect(qRect.x + 10, qRect.y + 40, qRect.width - 20, 10), "");
        GUI.Box(new Rect(qRect.x + 10, qRect.y + 40, (qRect.width - 20) * Mathf.Clamp01(areaFill), 10), "");

        // 2 - Ranged
        y += boxHeight + uiSpacing;
        Rect eRect = new Rect(x, y, boxWidth, boxHeight);
        GUI.Box(eRect, "2 - Ranged Shot");
        string eText;
        if (lockedTarget != null)
        {
            eText = $"Locked: {lockedTarget.name}";
        }
        else
        {
            eText = "Hover enemy and press 2 to lock";
        }
        GUI.Label(new Rect(eRect.x + 10, eRect.y + 20, eRect.width - 20, 20), eText);

        // Ranged cooldown bar
        float rangedElapsed = Time.time - lastRangedShotTime;
        float rangedFill = Mathf.Clamp01(rangedElapsed / Mathf.Max(0.0001f, rangedCooldown));
        float rangedRemaining = Mathf.Max(0f, rangedCooldown - rangedElapsed);
        GUI.Box(new Rect(eRect.x + 10, eRect.y + 40, eRect.width - 20, 10), "");
        GUI.Box(new Rect(eRect.x + 10, eRect.y + 40, (eRect.width - 20) * Mathf.Clamp01(rangedFill), 10), "");
        if (rangedRemaining > 0f)
        {
            GUI.Label(new Rect(eRect.x + eRect.width - 70, eRect.y + 20, 60, 20), $"{Mathf.Ceil(rangedRemaining)}s");
        }
    }
}
