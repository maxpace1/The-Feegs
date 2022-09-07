using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{

    public string gunName;
    public float ADSSpeed;
    public float fireRate;
    public int fireType; //0 - Semi | 1 - Full Auto | 2+ Burst
    public float bloom;
    public float recoil;
    public float kick;
    public int damage;
    public int ammo;
    public int clipSize;
    public float reloadTime;
    [Range(0, 1)] public float mainFOV;
    [Range(0, 1)] public float weaponFOV;
    public float aimSensitivityMultiplier;

    private int clip;
    private int stash;

    public AudioClip gunShotSound;
    public float pitchRandomization;
    public float gunShotVolume;

    public GameObject prefab;

    public void initialize()
    {

        stash = ammo;
        clip = clipSize;

    }

    public bool fireBullet()
    {

        if (clip > 0)
        {

            clip--;
            return true;

        } else return false;

    }

    public void reload()
    {

        stash += clip;
        clip = Mathf.Min(clipSize, stash);
        stash -= clip;

    }

    public int getStash() { return stash; }
    public int getClip() { return clip; }

}
