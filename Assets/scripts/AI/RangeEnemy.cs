﻿using UnityEngine;
using System.Collections;

public class RangeEnemy : BasicEnemy
{
    public float projectileAirTime = 1f, range = 10f, avoidDistance = 3f, maxImpulse = 1f, atkTime = 0.5f;
    float atkTimer = float.PositiveInfinity;
    public GameObject projectile;
    public void fire(Transform t)
    {
        atkTimer = float.PositiveInfinity;
        Vector2 dPos = (Vector2)(t.position) - pysc.position;
        GameObject proj = (GameObject)Instantiate(projectile, transform.position, Quaternion.identity);
        proj.GetComponent<Rigidbody2D>().velocity = new Vector2(dPos.x / projectileAirTime,
            (dPos.y - Physics2D.gravity.y * (projectile.GetComponent<Rigidbody2D>().gravityScale) * projectileAirTime * projectileAirTime / 2) / projectileAirTime);
        proj.GetComponent<Projectile>().lifeTime = projectileAirTime + 0.1f;
    }

    void Update()
    {
        Vector2 dPos = pysc.position - CharCtrl.script.pysc.position;
        float d = dPos.x * dPos.x + dPos.y * dPos.y;
        atkTimer -= Time.deltaTime;
        agro = agro ? true : d < range * range;
        if (agro)
        {
            if (d < avoidDistance * avoidDistance)
            {
                pysc.AddForce(Vector2.ClampMagnitude(dPos.normalized * walkSpeed * pysc.mass - pysc.velocity, maxImpulse), ForceMode2D.Impulse);
                ani.Play(dPos.x < 0f ? "walk" : "walkFlipped", 0);
            }
            else if (d > range * range)
            {
                pysc.AddForce(Vector2.ClampMagnitude(-dPos.normalized * walkSpeed * pysc.mass - pysc.velocity, maxImpulse), ForceMode2D.Impulse);
                ani.Play(dPos.x > 0f ? "walk" : "walkFlipped", 0);
            }
            else
                pysc.AddForce(Vector2.ClampMagnitude(-pysc.velocity * pysc.mass, maxImpulse), ForceMode2D.Impulse);
            if (atkTimer <= 0f)
                fire(CharCtrl.script.transform);
            if (atkTimer > atkTime)
                atkTimer = d < range * range ? atkTime : float.PositiveInfinity;
        }
    }
}