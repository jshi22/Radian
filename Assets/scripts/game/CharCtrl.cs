﻿using UnityEngine;
using System.Collections;

public class CharCtrl : MonoBehaviour
{
    public static CharCtrl script = null;
    public GameObject death, itemIcon, spawn, lightBar, darkBar, gemObject, fireArm, fireHand, lightArrow, darkArrow, shadow;
    public bool controllable = true, usingLight = true, isDashing = false;
    public float arrowSpeed = 8f, charSpeed = 10f, maxBrakeF = 3f, dashDist = 2f, dashCoolDown = 1f, arrowCoolDown = 1f, dashLerp = 0.1f, meleeRadius = 2f, meleeField = 0f, meleeCoolDown = 0.5f, deathFallTime = 1f, timedUncontrollable = 0f, sqrUnitPerSound = 0.1f, arrowKB = 10f, shadowDarkness = 0.3f, shadowScale = 1.5f, shadowOffset = 0f;
    public float meleeCost = 0.05f, dashCost = 0.01f, arrowCost = 0.05f;
    public float darkMultiplyer = 2f;
    public int meleeDamage = 1;
    public int dashLayer = 8, playerLayer = 10;
    public Rigidbody2D pysc = null;
    public BarCtrl light, dark;
    public GemCtrl gem;
    public Vector2 feetPos, armPos;
    float autoOrderOffset = -0.6f, dashTime = 0f, meleeTime = 0f, animationOverride = 0f, fallTime = 100000000f;
    bool rooted = false, variate = false;
    Vector2 lastInput = Vector2.down, lastJuicePosition;
    Animator ani, handAni;
    SpriteRenderer sr;
    Consumable item;
    Vector2 dashPos;
    CircleCollider2D cc;
    // Use this for initialization
    void Start()
    {
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
    }
    public void damage(float amount)
    {
        light.barPercent -= amount;
    }
    public void kill()
    {
        //((GameObject)Instantiate(death, transform.position + (new Vector3(0f, 12f, 0f)), transform.rotation)).GetComponent<RespawnAni>().player = this;
        //gameObject.SetActive(false);
        //SoundManager.script.playOnListener (SoundManager.script.death0, 0.4f);
        controllable = false;
        respawn();
    }

    public void respawn()
    {
        //gameObject.SetActive(true);
        //pysc.velocity = Vector2.zero;
        //transform.position = new Vector3(curSpawn.transform.position.x, curSpawn.transform.position.y - 4f, 0f);
        shadow.SetActive(true);
        light.barPercent = 1f;
        dark.barPercent = 0f;
        controllable = true;
        fallTime = float.PositiveInfinity;
        gameObject.layer = playerLayer;
        pysc.gravityScale = 0f;
        pysc.velocity = Vector2.zero;
        transform.position = spawn.transform.position;
        feetPos = pysc.position + cc.offset;
    }

    public bool eat(Consumable c)
    {
        if (item == null)
        {
            item = c;
            itemIcon.GetComponent<UnityEngine.UI.Image>().sprite = c.icon;
            //SoundManager.script.playOnListener(SoundManager.script.pickup0, 0.8f);
            itemIcon.SetActive(true);
            return true;
        }
        return false;
    }
    // Update is called once per frame
    void Update()
    {
        if (!shadow)
            genShadow();
        else
        {
            if (shadow.GetComponent<SpriteRenderer>().sprite != sr.sprite)
                genShadow();
            shadow.GetComponent<SpriteRenderer>().flipX = sr.flipX;
        }
        timedUncontrollable -= Time.deltaTime;
        if (fallTime <= deathFallTime)
        {
            fallTime -= Time.deltaTime;
            pysc.gravityScale = 7f;
            gameObject.layer = dashLayer;
            if (fallTime <= 0f)
                kill();
            return;
        }
        if (light.barPercent <= 0f || dark.barPercent >= 1f)
        {
            kill();
            return;
        }
        dashTime -= Time.deltaTime;
        meleeTime -= Time.deltaTime;
        animationOverride -= Time.deltaTime;
        Vector2 redirect = Vector2.right;
        feetPos = pysc.position + cc.offset;
        armPos = pysc.position + (Vector2)(fireArm.transform.localPosition);
        if (isDashing = dashPos.sqrMagnitude > 0.1f)
        {
            gameObject.layer = dashLayer;
            if (shadow.activeSelf)
                shadow.SetActive(false);
        }
        else
        {
            gameObject.layer = playerLayer;
            if (!shadow.activeSelf)
                shadow.SetActive(true);
        }
        foreach (RaycastHit2D rh in Physics2D.CircleCastAll(feetPos, 0.5f, Vector2.down, 0f))
            if (rh.collider.isTrigger)
                if (!isDashing && rh.collider.gameObject.GetComponent<Air>())
                {
                    fallTime = deathFallTime;
                    if (Mathf.Abs(lastInput.x) >= Mathf.Abs(lastInput.y))
                        ani.Play(lastInput.x > 0 ? "RightFall" : "LeftFall", 0);
                    else
                        ani.Play(lastInput.y > 0 ? "UpFall" : "DownFall", 0);
                    return;
                }
                else if (rh.collider.gameObject.GetComponent<MovementRedirect>())
                    redirect = rh.collider.gameObject.GetComponent<MovementRedirect>().dir;
        if (controllable && timedUncontrollable < 0f)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 rPosFromArm = ((Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition)) - armPos).normalized;
            if (animationOverride <= 0f)
                rooted = false;
            if (!isDashing)
            {
                if (input.sqrMagnitude != 0f && animationOverride <= 0f)
                {
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
                    if (rooted)
                    {
                        if (Mathf.Abs(rPosFromArm.x) > Mathf.Abs(rPosFromArm.y) || rPosFromArm.y > 0)
                        {
                            if (fireHand.transform.localPosition.z != 0.01f)
                                fireArm.transform.localPosition = new Vector3(fireArm.transform.localPosition.x, fireArm.transform.localPosition.y, 0.01f);
                        }
                        else if (fireHand.transform.localPosition.z != -0.01f)
                            fireArm.transform.localPosition = new Vector3(fireArm.transform.localPosition.x, fireArm.transform.localPosition.y, -0.01f);
                    }
                    else
                        playIdleAnimation();
                }
            }
            else
            {
                brake();
                if (Mathf.Abs(dashPos.x) > Mathf.Abs(dashPos.y))
                    ani.Play(dashPos.x > 0 ? "RightRoll" : "LeftRoll", 0);
                else
                    ani.Play(dashPos.y > 0 ? "UpRoll" : "DownRoll", 0);
            }
            if (light.barPercent > dashCost && dashTime <= 0f && Input.GetKeyDown(Settings.keys[Settings.player, Settings.dash]))
            {
                float closest = dashDist;
                lastInput = rPosFromArm;
                foreach (RaycastHit2D rh in Physics2D.RaycastAll(feetPos, rPosFromArm, dashDist))
                    if (!rh.collider.isTrigger && rh.distance < closest && !(rh.collider.attachedRigidbody && rh.collider.attachedRigidbody.gameObject == gameObject))
                        closest = rh.distance;
                dashPos = rPosFromArm * closest;
                dashTime = dashCoolDown;
                cost(dashCost);
                SoundManager.script.playOnListener(SoundManager.script.dash, 0.7f);
            }
            if (meleeTime <= 0f && Input.GetMouseButtonDown(0))
            {
                BasicEnemy be = null;
                foreach (RaycastHit2D rh in Physics2D.CircleCastAll(pysc.position, meleeRadius, Vector2.down, 0f))
                    if (!rh.collider.isTrigger && (be = rh.collider.gameObject.GetComponent<BasicEnemy>()) && Vector2.Dot((rh.point - pysc.position).normalized, rPosFromArm) >= meleeField)
                        be.damage(meleeDamage, BasicEnemy.MELEE_DAMAGE);
                meleeTime = meleeCoolDown;
                cost(meleeCost);
                rooted = true;
                animationOverride = meleeCoolDown;
                if (Mathf.Abs(rPosFromArm.x) >= Mathf.Abs(rPosFromArm.y))
                    if (variate)
                        ani.Play(rPosFromArm.x > 0 ? "RightAttack1" : "LeftAttack1", 0);
                    else
                        ani.Play(rPosFromArm.x > 0 ? "RightAttack2" : "LeftAttack2", 0);
                else
                    ani.Play(rPosFromArm.y > 0 ? "UpAttack" : "DownAttack", 0);
                variate = !variate;
                lastInput = rPosFromArm;
            }
            if (light.barPercent > arrowCost && Input.GetMouseButtonDown(1))
                fire(rPosFromArm);
            if (Input.GetKeyDown(Settings.keys[Settings.player, Settings.toggleEnergy]))
            {
                usingLight = !usingLight;
                gem.isLight = usingLight;
            }
        }
        else
        {
            playIdleAnimation();
            brake();
        }
        if ((lastJuicePosition - pysc.position).sqrMagnitude >= sqrUnitPerSound)
        {
            lastJuicePosition = pysc.position;
            if (!isDashing && fallTime > deathFallTime)
                SoundManager.script.playOnListener(SoundManager.script.grassFootStep, 0.8f);
        }
        pysc.position += dashPos * dashLerp;
        dashPos *= 1 - dashLerp;
        transform.position = new Vector3(transform.position.x, transform.position.y, (transform.position.y + autoOrderOffset) / 100f);
    }
    public void brake()
    {
        pysc.AddForce(Vector2.ClampMagnitude(-pysc.velocity * pysc.mass, maxBrakeF), ForceMode2D.Impulse);
    }
    public void fire(Vector2 dir)
    {
        if (animationOverride > 0f)
            return;
        cost(arrowCost);
        rooted = true;
        animationOverride = arrowCoolDown;
        handAni.Play(dir.x > 0 ? "FireUpDownFliped" : "FireUpDown");
        fireArm.transform.localRotation = Quaternion.LookRotation(Vector3.forward, -dir);
        pysc.AddForce(-dir * arrowKB);
        GameObject tmp = (GameObject)(Instantiate(usingLight ? lightArrow : darkArrow, fireHand.transform.position, Quaternion.identity));
        tmp.GetComponent<Projectile>().setVelocity(dir * arrowSpeed);
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            ani.Play(dir.x > 0 ? "RightFireState" : "LeftFireState", 0);
        else
            ani.Play(dir.y > 0 ? "UpFireState" : "DownFireState", 0);
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
            light.barPercent -= cost;
        else
            dark.barPercent += cost / darkMultiplyer;
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
        float dMin = sr.bounds.extents.y - shadowSr.bounds.extents.y;
        shadow.transform.localPosition = new Vector3(0f, -dMin + shadowOffset, 0f);
    }
}
