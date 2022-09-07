using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{

    #region Variables

    public Gun[] loadout;
    [HideInInspector] public Gun currentGunData;

    public Transform weaponParent;
    public GameObject bulletHolePrefab;
    public LayerMask canBeShot;
    public AudioSource sfx;

    private float currentCooldown;
    [HideInInspector] public bool isAiming;

    private GameObject currentWeapon;
    private int currentIndex;

    private bool isReloading = false;

    private Image hitmarkerImage;
    private float hitmarkerWait;
    public static float redHitmarker = 0f;
    public AudioClip hitmarkerSound;

    #endregion

    #region MonoBehaviour Callbacks

    // Start is called before the first frame update
    void Start()
    {

        if (photonView.IsMine)
        {

            foreach (Gun a in loadout) a.initialize();
            hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
            hitmarkerImage.color = new Color(1, 1, 1, 0);
            equip(0);

        }

    }
    
    // Update is called once per frame
    void Update()
    {

        if (Pause.paused && photonView.IsMine) return;

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1)) photonView.RPC("equip", RpcTarget.All, 0);
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2)) photonView.RPC("equip", RpcTarget.All, 1);
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha3)) photonView.RPC("equip", RpcTarget.All, 2);

        if (currentWeapon != null)
        {

            if (photonView.IsMine)
            {

                //isAiming = Input.GetKey(KeyCode.LeftAlt);
                //aim(isAiming);

                if (loadout[currentIndex].fireType != 1) {

                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0)
                    {

                        if (loadout[currentIndex].fireBullet()) photonView.RPC("shoot", RpcTarget.All);
                        else StartCoroutine(reload(loadout[currentIndex].reloadTime));

                    }

                } else
                {

                    if (Input.GetMouseButton(0) && currentCooldown <= 0)
                    {

                        if (loadout[currentIndex].fireBullet()) photonView.RPC("shoot", RpcTarget.All);
                        else StartCoroutine(reload(loadout[currentIndex].reloadTime));

                    }

                }

                if (Input.GetKeyDown(KeyCode.R) && loadout[currentIndex].getClip() < loadout[currentIndex].clipSize) photonView.RPC("reloadRPC", RpcTarget.All);

                //Cooldown
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

            }

            //Weapon position elasticity
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.down * .25f, Time.deltaTime * 4f);

        }

        if (photonView.IsMine)
        {

            if (hitmarkerWait > 0) hitmarkerWait -= Time.deltaTime;
            if (hitmarkerWait > 0 && Mathf.Abs(redHitmarker - hitmarkerImage.color.r) < .005) hitmarkerImage.color = Color.red;
            else if (hitmarkerImage.color.a > 0)
            {

                if (redHitmarker < .005) hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, new Color(1, 1, 1, 0), Time.deltaTime * 8f);
                else hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, new Color(1, 0, 0, 0), Time.deltaTime * 8f);
                redHitmarker = Mathf.Lerp(redHitmarker, 0f, Time.deltaTime * 8f);

            }

        }

    }

    #endregion

    #region Private Methods

    [PunRPC]
    void equip(int index)
    {

        if (currentWeapon != null)
        {

            if (isReloading) StopCoroutine("reload");
            Destroy(currentWeapon);

        }

        GameObject newWeapon = Instantiate(loadout[index].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        newWeapon.transform.localPosition += Vector3.down * .25f;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        if (photonView.IsMine) changeLayersRecursively(newWeapon, 10);
        else changeLayersRecursively(newWeapon, 0);

        newWeapon.GetComponent<Animator>().Play("Equip", 0, 0);

        currentWeapon = newWeapon;
        currentIndex = index;
        currentGunData = loadout[index];

    }

    private void changeLayersRecursively(GameObject target, int layer)
    {

        target.layer = layer;
        foreach (Transform a in target.transform) changeLayersRecursively(a.gameObject, layer);

    }

    public bool aim(bool isAiming)
    {

        if (!currentWeapon) return false;
        if (isReloading) isAiming = false;

        Transform anchor = currentWeapon.transform.Find("Anchor");
        Transform stateHip = currentWeapon.transform.Find("States/Hip");
        Transform stateADS = currentWeapon.transform.Find("States/ADS");

        if (isAiming) anchor.position = Vector3.Lerp(anchor.position, stateADS.position, Time.deltaTime * loadout[currentIndex].ADSSpeed);
        else anchor.position = Vector3.Lerp(anchor.position, stateHip.position, Time.deltaTime * loadout[currentIndex].ADSSpeed);

        return isAiming;

    }

    [PunRPC]
    void shoot()
    {

        if (isReloading) return;

        Transform spawn = transform.Find("Cameras/Normal Camera");

        //Bloom or something
        Vector3 bloom = spawn.position + spawn.forward * 1000;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * spawn.up;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * spawn.right;
        bloom -= spawn.position;
        bloom.Normalize();

        /*//Raycast
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(spawn.position, bloom, out hit, 1000f, canBeShot))
        {

            GameObject newBulletHole = Instantiate(bulletHolePrefab, hit.point + hit.normal * .001f, Quaternion.identity) as GameObject;
            newBulletHole.transform.LookAt(hit.point + hit.normal);
            Destroy(newBulletHole, 10f);

            if (photonView.IsMine) if (hit.collider.transform.gameObject.layer == 11 || hit.collider.transform.gameObject.layer == 12)
                {

                    //Damage player
                    int damage = loadout[currentIndex].damage;
                    if (hit.collider.transform.gameObject.layer == 12) damage = Mathf.FloorToInt((float)damage * 1.9f);
                    if (hit.collider.transform.gameObject.layer == 12) Debug.Log("HEADSHOT!!!");
                    hit.collider.transform.root.gameObject.GetPhotonView().RPC("preTakeDamage", RpcTarget.All, damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    //Show hitmarker
                    hitmarkerImage.color = Color.white;
                    if (redHitmarker > .005) hitmarkerImage.color = Color.red;
                    sfx.volume = 1.5f;
                    sfx.PlayOneShot(hitmarkerSound);
                    hitmarkerWait = 0.75f;

                }

        }*/

        

        //Sound
        //sfx.Stop();
        if (currentGunData.gunShotSound) sfx.clip = currentGunData.gunShotSound;
        sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        sfx.volume = currentGunData.gunShotVolume;
        sfx.Play();

        //Gun FX
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kick;

        //Cooldown
        currentCooldown = loadout[currentIndex].fireRate;

    }

    [PunRPC]
    private void preTakeDamage(int damage, int actor)
    {

        GetComponent<Player>().takeDamage(damage, actor);

    }

    [PunRPC]
    private void reloadRPC()
    {

        StartCoroutine(reload(loadout[currentIndex].reloadTime));

    }

    IEnumerator reload(float wait)
    {

        isReloading = true;
        if (currentWeapon.GetComponent<Animator>()) currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
        else currentWeapon.SetActive(false);
        yield return new WaitForSeconds(wait);
        loadout[currentIndex].reload();
        isReloading = false;
        currentWeapon.SetActive(true);

    }

    #endregion

    #region Public Methods

    public void refreshAmmo (Text text)
    {

        int clip = loadout[currentIndex].getClip();
        int stash = loadout[currentIndex].getStash();

        text.text = clip.ToString("D2") + " / " + stash.ToString("D2");

    }

    #endregion

}