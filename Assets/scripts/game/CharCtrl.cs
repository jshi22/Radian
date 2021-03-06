﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharCtrl : MonoBehaviour
{
    public static CharCtrl script = null;
    public float diff;
    public GameObject death, itemIcon, lightBar, darkBar, gemObject, fireArm, fireHand, lightArrow, darkArrow, shadow, darkP, lightP;
    public string introAnimation = "", sceneChangeOnDeath = "";
    public bool controllable = true, usingLight = true, isDashing = false, arrowLoaded = false, invulnerable = false;
    public float spawnLength = 1.2f, arrowSpeed = 8f, charSpeed = 10f, maxBrakeF = 3f, dashDist = 2f, dashCoolDown = 1f, arrowWindUp = 1f, arrowCoolDown = 0.5f, dashLerp = 0.1f, meleeRadius = 2f, meleeField = 0f, meleeCoolDown = 0.5f, deathFallTime = 1f, timedUncontrollable = 0f, timedInvulnerable = 0f, sqrUnitPerSound = 0.1f, arrowKB = 10f, meleeAdv = 10f, shadowDarkness = 0.3f, shadowScale = 1.5f, shadowOffset = 0f, shadowZOffset = 0f, staggerTime = 0.1f, deathAnimationTime = 1f;
    public float arrowCost = 0.05f;
    public float lifeMultiplyer = 2f, darkAcumulation = 0.01f;
    public int meleeDamage = 1, maxComboCount = 3;
    public int dashLayer = 8, playerLayer = 10, noclipLayer = 16;
    public Rigidbody2D pysc = null;
    public new BarCtrl light;
    public BarCtrl dark;
    public GemCtrl gem;
    public Vector2 feetPos, armPos;
    public CanvasRenderer cr;
    float curA = 0f, autoOrderOffset = -0.6f, dashTime = 0f, meleeTime = 0f, arrowTime, animationOverride = 0f, fallTime = 100000000f, deathTimer = float.PositiveInfinity;
    bool rooted = false, variate = false, overAir = false, noUpdate = false;
    int comboCount = 0;
    Vector2 lastInput = Vector2.down, lastJuicePosition, dashPos;
    Animator ani, handAni;
    SpriteRenderer sr;
    Consumable item;
    CircleCollider2D cc;
    void Awake()
    {
        diff = PlayerPrefs.GetFloat("Diff", 1f);
        PlayerPrefs.SetString("lastScene", SceneManager.GetActiveScene().name);
        light = lightBar.GetComponent<BarCtrl>();
        dark = darkBar.GetComponent<BarCtrl>();
        script = this;
        pysc = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cc = GetComponent<CircleCollider2D>();
        gem = gemObject.GetComponent<GemCtrl>();
        ani = GetComponent<Animator>();
        autoOrderOffset = GetComponent<AutoOrder>().offset;
        handAni = fireHand.GetComponent<Animator>();
        lastJuicePosition = pysc.position;
        if (PlayerPrefs.HasKey("spawnY_" + SceneManager.GetActiveScene().name))
        {
            ani.Play("Awake", 0);
            timedUncontrollable = 1.2f;
        }
        else
        {
            if (introAnimation.Length != 0)
                ani.Play(introAnimation, 0);
            timedUncontrollable = spawnLength;
        }
        gem.isLight = usingLight;
        if (PlayerPrefs.HasKey("spawnX_" + SceneManager.GetActiveScene().name))
            transform.position = new Vector2(PlayerPrefs.GetFloat("spawnX_" + SceneManager.GetActiveScene().name), PlayerPrefs.GetFloat("spawnY_" + SceneManager.GetActiveScene().name));
    }
    public void damage(float amount)
    {
        if (invulnerable || timedInvulnerable > 0f)
            return;
        light.barPercent -= amount * diff;
        curA = 1;
        timedUncontrollable = timedInvulnerable = staggerTime;
        SoundManager.script.playOnListener(variate ? SoundManager.script.playerHit1 : SoundManager.script.playerHit2, 1f);
        if (Mathf.Abs(lastInput.x) >= Mathf.Abs(lastInput.y))
            ani.Play(lastInput.x > 0 ? "RightStagger" : "LeftStagger", 0);
        else
            ani.Play(lastInput.y > 0 ? "UpStagger" : "DownStagger", 0);
    }
    public void kill()
    {
        curA = float.PositiveInfinity;
        controllable = false;
        noUpdate = true;
        invulnerable = true;
        deathTimer = deathAnimationTime;
        ani.Play("Death", 0);
        SoundManager.script.playOn(transform, SoundManager.script.deathCHR);
        fireArm.SetActive(false);
    }
    public void respawn()
    {
        if (sceneChangeOnDeath.Length == 0)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(sceneChangeOnDeath);
    }
    public bool eat(Consumable c)
    {
        if (item == null)
        {
            item = c;
            itemIcon.GetComponent<UnityEngine.UI.Image>().sprite = c.icon;
            itemIcon.SetActive(true);
            return true;
        }
        return false;
    }
    void FixedUpdate()
    {
        transform.localPosition += (Vector3)dashPos * dashLerp;
        dashPos *= 1 - dashLerp;
    }
    void Update()
    {
        variate = !variate;
        deathTimer -= Time.deltaTime;
        timedUncontrollable -= Time.deltaTime;
        dashTime -= Time.deltaTime;
        meleeTime -= Time.deltaTime;
        animationOverride -= Time.deltaTime;
        timedInvulnerable -= Time.deltaTime;
        curA *= 0.95f;
        if (curA < 0.05f)
        {
            if (cr.gameObject.activeSelf)
                cr.gameObject.SetActive(false);
        }
        else if (cr.gameObject.activeSelf)
            cr.SetAlpha(Mathf.Min(curA, 1));
        else
            cr.gameObject.SetActive(true);
        if (deathTimer <= 0f)
            respawn();
        if (noUpdate)
            return;
        if (!shadow)
            genShadow();
        else
        {
            if (shadow.GetComponent<SpriteRenderer>().sprite != sr.sprite)
                genShadow();
            shadow.GetComponent<SpriteRenderer>().flipX = sr.flipX;
        }
        if (fallTime <= deathFallTime)
        {
            fallTime -= Time.deltaTime;
            gameObject.layer = noclipLayer;
            pysc.gravityScale = 7f;
            gameObject.layer = dashLayer;
            if (fallTime <= 0f)
                kill();
            transform.position = new Vector3(transform.position.x, transform.position.y, (transform.position.y + autoOrderOffset) / 100f);
            return;
        }
        if (light.barPercent <= 0f)
        {
            kill();
            return;
        }
        Vector2 redirect = Vector2.right;
        feetPos = pysc.position + cc.offset;
        armPos = pysc.position + (Vector2)(fireArm.transform.localPosition);
        if (isDashing = dashPos.sqrMagnitude > 0.1f)
        {
            gameObject.layer = dashLayer;
            if (shadow.activeSelf && overAir)
                shadow.SetActive(false);
        }
        else
        {
            gameObject.layer = playerLayer;
            if (!shadow.activeSelf && overAir)
                shadow.SetActive(true);
        }
        float closestA = float.PositiveInfinity;
        Activatable aInRange = null;
        foreach (RaycastHit2D rh in Physics2D.CircleCastAll(feetPos, 0.5f, Vector2.down, 0f))
            if (rh.collider.isTrigger)
            {
                if (!isDashing && rh.collider.gameObject.GetComponent<Air>())
                {
                    fallTime = deathFallTime;
                    if (Mathf.Abs(lastInput.x) >= Mathf.Abs(lastInput.y))
                        ani.Play(lastInput.x > 0 ? "RightFall" : "LeftFall", 0);
                    else
                        ani.Play(lastInput.y > 0 ? "UpFall" : "DownFall", 0);
                    return;
                }
                else if (rh.collider.gameObject.GetComponent<Activatable>() && rh.distance < closestA)
                {
                    closestA = rh.distance;
                    aInRange = rh.collider.gameObject.GetComponent<Activatable>();
                }
                if (rh.collider.gameObject.GetComponent<MovementRedirect>())
                    redirect = rh.collider.gameObject.GetComponent<MovementRedirect>().dir;
            }
        if (timedUncontrollable < 0f)
        {
            if (controllable)
            {
                Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                Vector2 rPosFromArm = ((Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition)) - armPos).normalized;
                if (animationOverride <= 0f)
                    rooted = false;
                if (!isDashing)
                {
                    if (input.sqrMagnitude != 0f && animationOverride <= 0f && !arrowLoaded)
                    {
                        comboCount = 0;
                        rooted = false;
                        lastInput = input;
                        input = input.normalized;
                        input = redirect * input.x + new Vector2(-redirect.y, redirect.x) * input.y;
                        pysc.AddForce((input * charSpeed - pysc.velocity) * pysc.mass, ForceMode2D.Impulse);
                        if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                            ani.Play(input.x > 0 ? "RightWalk" : "LeftWalk", 0);
                        else
                            ani.Play(input.y > 0 ? "UpWalk" : "DownWalk", 0);
                    }
                    else
                    {
                        brake();
                        if (!(rooted || arrowLoaded))
                            playIdleAnimation();
                    }
                    if (Input.GetKeyDown(Settings.keys[Settings.player, Settings.dash]) && !arrowLoaded && dashTime <= 0f)
                    {
                        comboCount = 0;
                        float closest = dashDist;
                        lastInput = rPosFromArm;
                        overAir = false;
                        foreach (RaycastHit2D rh in Physics2D.RaycastAll(feetPos, rPosFromArm, dashDist))
                        {
                            if (!rh.collider.isTrigger && rh.distance < closest && !(rh.collider.attachedRigidbody && rh.collider.attachedRigidbody.gameObject == gameObject) && rh.collider.gameObject != gameObject)
                                closest = rh.distance;
                            if (!overAir && rh.collider.gameObject.GetComponent<Air>())
                                overAir = true;
                        }
                        dashPos = rPosFromArm * closest;
                        if (Mathf.Abs(dashPos.x) > Mathf.Abs(dashPos.y))
                            ani.Play(overAir ? dashPos.x > 0 ? "RightDash" : "LeftDash" : dashPos.x > 0 ? "RightRoll" : "LeftRoll", 0);
                        else
                            ani.Play(overAir ? dashPos.y > 0 ? "UpDash" : "DownDash" : dashPos.y > 0 ? "UpRoll" : "DownRoll", 0);
                        dashTime = dashCoolDown;
                        SoundManager.script.playOnListener(SoundManager.script.dash, 0.7f);
                    }
                    if (!arrowLoaded && Input.GetMouseButtonDown(0))
                        comboCount++;
                    if ((light.barPercent > arrowCost || !usingLight) && Input.GetMouseButtonDown(1) && canAfford(arrowCost))
                    {
                        arrowLoaded = true;
                        comboCount = 0;
                        handAni.Play("boxWindUp", 0);
                        SoundManager.script.playOnListener(SoundManager.script.bowDraw);
                    }
                    if (arrowLoaded && Input.GetMouseButton(1))
                    {
                        arrowTime += Time.deltaTime;
                        if (Input.GetKeyDown(Settings.keys[Settings.player, Settings.cancel]))
                        {
                            arrowLoaded = false;
                            handAni.Play("NoAnimation", 0);
                            arrowTime = 0f;
                            arrowLoaded = false;
                        }
                        fireArm.transform.localRotation = Quaternion.LookRotation(Vector3.forward, -rPosFromArm);
                        if (Mathf.Abs(rPosFromArm.x) > Mathf.Abs(rPosFromArm.y))
                        {
                            ani.Play(rPosFromArm.x < 0 ? "LeftFireState" : "RightFireState", 0);
                            if (fireHand.transform.localPosition.z != 0.01f)
                                fireArm.transform.localPosition = new Vector3(fireArm.transform.localPosition.x, fireArm.transform.localPosition.y, 0.0001f);
                        }
                        else if (rPosFromArm.y > 0)
                        {
                            ani.Play("UpFireState", 0);
                            if (fireHand.transform.localPosition.z != 0.01f)
                                fireArm.transform.localPosition = new Vector3(fireArm.transform.localPosition.x, fireArm.transform.localPosition.y, 0.0001f);
                        }
                        else if (fireHand.transform.localPosition.z != -0.01f)
                        {
                            ani.Play("DownFireState", 0);
                            fireArm.transform.localPosition = new Vector3(fireArm.transform.localPosition.x, fireArm.transform.localPosition.y, -0.0001f);
                        }
                    }
                    else
                    {
                        if (arrowTime >= arrowWindUp)
                            fire(rPosFromArm);
                        handAni.Play("NoAnimation", 0);
                        arrowTime = 0f;
                        arrowLoaded = false;
                    }
                    if (!arrowLoaded && meleeTime <= 0f && comboCount > 0)
                    {
                        comboCount--;
                        BasicEnemy be = null;
                        foreach (RaycastHit2D rh in Physics2D.CircleCastAll(pysc.position, meleeRadius, Vector2.down, 0f))
                            if (!rh.collider.isTrigger && (be = rh.collider.gameObject.GetComponent<BasicEnemy>()) && Vector2.Dot((rh.point - pysc.position).normalized, rPosFromArm) >= meleeField)
                            {
                                be.damage((int)(meleeDamage / diff), BasicEnemy.MELEE_DAMAGE);
                                corrupt(meleeDamage);
                                SoundManager.script.playOnListener(variate ? SoundManager.script.enemyHit1 : SoundManager.script.enemyHit2, 0.8f);
                            }
                        SoundManager.script.playOnListener(variate ? SoundManager.script.sword1 : SoundManager.script.sword2, 0.8f);
                        meleeTime = meleeCoolDown;
                        rooted = true;
                        animationOverride = meleeCoolDown;
                        if (Mathf.Abs(rPosFromArm.x) >= Mathf.Abs(rPosFromArm.y))
                            if (variate)
                                ani.Play(rPosFromArm.x > 0 ? "RightAttack1" : "LeftAttack1", 0, 0);
                            else
                                ani.Play(rPosFromArm.x > 0 ? "RightAttack2" : "LeftAttack2", 0, 0);
                        else if (variate)
                            ani.Play(rPosFromArm.y > 0 ? "UpAttack" : "DownAttack", 0, 0);
                        else
                            ani.Play(rPosFromArm.y > 0 ? "UpAttack2" : "DownAttack2", 0, 0);
                        lastInput = rPosFromArm;
                        pysc.AddForce(rPosFromArm * meleeAdv);
                    }
                }
                else
                    brake();
                if (Input.GetKeyDown(Settings.keys[Settings.player, Settings.use]))
                {
                    if (aInRange && aInRange.playerActivatable)
                        aInRange.activate(this);
                    else
                    {
                        SoundManager.script.playOnListener(SoundManager.script.lightSwitch, 1f);
                        usingLight = !usingLight;
                        gem.isLight = usingLight;
                        (usingLight ? lightP : darkP).GetComponent<ParticleSystem>().Play();
                    }
                }
            }
            else
            {
                playIdleAnimation();
                brake();
            }
        }
        if ((lastJuicePosition - pysc.position).sqrMagnitude >= sqrUnitPerSound)
        {
            lastJuicePosition = pysc.position;
            if (!isDashing && fallTime > deathFallTime)
                SoundManager.script.playOnListener(variate ? SoundManager.script.step1 : SoundManager.script.step2, 0.8f);
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, (transform.position.y + autoOrderOffset) / 100f);
    }
    public void corrupt(float damage)
    {
        dark.barPercent = Mathf.Min(1f, dark.barPercent + damage * darkAcumulation / diff);
    }
    public void brake()
    {
        pysc.AddForce(Vector2.ClampMagnitude(-pysc.velocity * pysc.mass, maxBrakeF), ForceMode2D.Impulse);
    }
    public bool canAfford(float cost)
    {
        return (usingLight ? light.barPercent - cost / lifeMultiplyer : dark.barPercent - cost) >= 0f;
    }
    public void fire(Vector2 dir)
    {
        if (animationOverride > 0f)
            return;
        SoundManager.script.playOnListener(SoundManager.script.bowRelease);
        arrowTime = 0f;
        cost(arrowCost);
        rooted = true;
        animationOverride = arrowCoolDown;
        handAni.Play(dir.x > 0 ? "FireUpDownFliped" : "FireUpDown");
        fireArm.transform.localRotation = Quaternion.LookRotation(Vector3.forward, -dir);
        pysc.AddForce(-dir * arrowKB);
        GameObject tmp = (GameObject)(Instantiate(usingLight ? lightArrow : darkArrow, fireHand.transform.position, Quaternion.identity));
        tmp.GetComponent<Projectile>().setVelocity(dir * arrowSpeed);
        lastInput = dir;
    }
    public void playIdleAnimation()
    {
        if (Mathf.Abs(lastInput.x) >= Mathf.Abs(lastInput.y))
            ani.Play(lastInput.x > 0 ? "idleRight" : "idleLeft", 0);
        else
            ani.Play(lastInput.y > 0 ? "idleUp" : "idleDown", 0);
    }
    public void cost(float cost)
    {
        if (usingLight)
            light.barPercent -= cost / lifeMultiplyer;
        else
            dark.barPercent -= cost;
    }
    void genShadow()
    {
        if (!shadow)
        {
            shadow = new GameObject();
            shadow.name = "shadow";
            shadow.transform.SetParent(gameObject.transform);
            shadow.AddComponent<SpriteRenderer>();
        }
        SpriteRenderer shadowSr = shadow.GetComponent<SpriteRenderer>();
        shadowSr.sprite = sr.sprite;
        shadowSr.color = new Color(0f, 0f, 0f, shadowDarkness);
        shadow.transform.localPosition = Vector3.zero;
        shadow.transform.localScale = new Vector3(1f, shadowScale, 1f);
        float dMin = shadowScale > 0 ? sr.bounds.extents.y - shadowSr.bounds.extents.y : -sr.bounds.extents.y + shadowSr.bounds.extents.y;
        shadow.transform.localPosition = new Vector3(0f, -dMin + shadowOffset, shadowZOffset);
    }
}
