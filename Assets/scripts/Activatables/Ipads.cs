﻿using UnityEngine;
using System.Collections;

public class Ipads : Activatable
{
    public GameObject canvas;
    public AudioClip sound;
    public string content;
    UnityEngine.UI.Text txt;
    float a = 0f;
    bool showing = false, falsified = false;
    CanvasRenderer cr;
    CanvasRenderer[] crs;
    public override void init()
    {
        txt = canvas.GetComponentInChildren<UnityEngine.UI.Text>();
        cr = canvas.GetComponent<CanvasRenderer>();
        crs = canvas.GetComponentsInChildren<CanvasRenderer>();
        content = content.Replace("\\n", "\n");
        content = content.Replace("\\t", "\t");
    }
    public override void activate(CharCtrl player)
    {
        if (chainedActivatable != null)
            chainedActivatable.activate(player);
        if (showing)
            CharCtrl.script.invulnerable = false;
        else
        {
            CharCtrl.script.invulnerable = true;
            txt.text = content;
            SoundManager.script.playOn(transform, sound);
        }
        falsified = false;
        showing = !showing;
    }
    void Update()
    {
        if (showing)
        {
            a += (1 - a) * 0.1f;
            if (canvas.activeSelf)
            {
                cr.SetAlpha(a);
                for (int i = 0; i < crs.Length; i++)
                    crs[i].SetAlpha(a);
            }
        }
        else
        {
            a -= a * 0.1f;
            if (canvas.activeSelf && !falsified)
            {
                cr.SetAlpha(a);
                for (int i = 0; i < crs.Length; i++)
                    crs[i].SetAlpha(a);
            }
        }
        if (a < 0.05f)
        {
            if (canvas.activeSelf && !falsified)
            {
                canvas.SetActive(false);
                falsified = true;
            }
        }
        else if (!canvas.activeSelf)
            canvas.SetActive(true);
    }
    void OnTriggerExit2D(Collider2D c)
    {
        if (showing && c.gameObject == CharCtrl.script.gameObject)
            activate(CharCtrl.script);
    }
}
