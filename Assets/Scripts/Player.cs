using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{

    #region Variables

    public float speed;
    public float sprintMultiplier;
    public float crouchModifier;
    public float aimMultiplier;
    public float jumpForce;
    public int maxHealth;
    public float regenDelay;
    private float currentRegenWait;
    public float regenSpeed;
    private int currentHealth;
    private float healthProjection;
    public float slideDuration;
    public float slideModifier;

    public float slideAmount;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;
    public GameObject headCollider;
    public GameObject mesh;

    private Rigidbody rig;
    public Camera normalCam;
    public Camera weaponCam;
    public GameObject cameraParent;
    private Vector3 normalCamTarget;
    private Vector3 weaponCamTarget;

    public Transform weaponParent;
    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrentPosition;
    private float movementCounter;
    private float idleCounter;

    public Transform groundDetector;
    public LayerMask ground;
    private float previousGroundcast = 0;

    private float baseFOV;
    private Vector3 origin;
    private float sprintFOVMultiplier = 1.25f;

    private Transform UIHealthBar;
    private Text UIAmmo;
    private Text UIUsername;

    [HideInInspector] public ProfileData playerProfile;
    public TextMeshPro playerUsername;

    private Manager manager;
    private Weapon weapon;

    private bool sliding;
    private float slideTime;
    private Vector3 slideDirection;
    private bool crouched;

    private float aimAngle;
    [HideInInspector] public bool isAiming;

    private Animator anim;

    #endregion

    #region Photon Callbacks

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo message)
    {

        if (stream.IsWriting)
        {

            stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));

        } else
        {

            aimAngle = (int)stream.ReceiveNext() / 100f;

        }

    }

    #endregion

    #region MonoBehaviour Callbacks

    // Start is called before the first frame update
    private void Start()
    {

        cameraParent.SetActive(photonView.IsMine);
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        weapon = GetComponent<Weapon>();

        if (!photonView.IsMine)
        {

            gameObject.layer = 11;
            standingCollider.layer = 11;
            crouchingCollider.layer = 11;
            changeLayerRecursively(mesh.transform, 11);
            headCollider.layer = 12;

        }

        if (Camera.main) Camera.main.enabled = false;
        rig = GetComponent<Rigidbody>();

        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrentPosition = weaponParentOrigin;

        normalCamTarget = normalCam.transform.localPosition;
        weaponCamTarget = weaponCam.transform.localPosition;
        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        currentHealth = maxHealth;
        healthProjection = currentHealth;

        if (photonView.IsMine)
        {

            UIHealthBar = GameObject.Find("HUD/Health/Bar").transform;
            UIAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            UIUsername = GameObject.Find("HUD/Username/Text").GetComponent<Text>();

            refreshHealthBar();
            UIUsername.text = Launcher.myProfile.username;

            photonView.RPC("syncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.kills, Launcher.myProfile.deaths, Launcher.myProfile.rank, Launcher.myProfile.sensitivity);

            anim = GetComponent<Animator>();

        }

    }

    private void Update()
    {

        if (!photonView.IsMine)
        {

            RefreshMultiplayerState();
            return;

        }

        //Axes
        float hmove = Input.GetAxisRaw("Horizontal");
        float vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprintKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jumpKeyDown = Input.GetKeyDown(KeyCode.Space);
        bool crouchKeyDown = Input.GetKeyDown(KeyCode.LeftControl);
        bool aimKeyDown = Input.GetKey(KeyCode.C);
        bool pauseKeyDown = Input.GetKeyDown(KeyCode.Escape);

        //States
        bool isGrounded = groundTest();
        bool isJumping = jumpKeyDown && isGrounded;
        isAiming = aimKeyDown;
        bool isSprinting = sprintKeyDown && vmove > 0 && !isJumping && !isAiming;
        bool isCrouching = crouchKeyDown && !isSprinting && !isJumping && isGrounded;

        //Pause
        if (pauseKeyDown)
        {

            GameObject.Find("Pause").GetComponent<Pause>().togglePause();

        }

            if (Pause.paused)
        {

            hmove = 0f;
            vmove = 0f;
            sprintKeyDown = false;
            jumpKeyDown = false;
            crouchKeyDown = true;
            aimKeyDown = false;
            pauseKeyDown = false;
            isGrounded = false;
            isJumping = false;
            isCrouching = false;
            isAiming = false;

        }

        //Crouching
        if (isCrouching) photonView.RPC("setCrouch", RpcTarget.All, !crouched);

        //Jumping
        if (isJumping)
        {

            if (crouched) photonView.RPC("setCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);

        }

        if (Input.GetKeyDown(KeyCode.U)) takeDamage(20, -1);

        //Head Bob

        if (!isGrounded) //Airborne
        {

            headBob(idleCounter, .01f, .01f);
            if (!isAiming) weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f * .2f);
            else weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 6f * .2f);

        } else if (sliding) //Sliding
        {

            headBob(movementCounter, .1f, .08f);
            if (!isAiming) weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f * .2f);
            else weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 10f * .2f);

        } else if (hmove == 0 && vmove == 0) //Idling
        {

            headBob(idleCounter, .025f, .025f);
            idleCounter += Time.deltaTime;
            if (!isAiming) weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f * .2f);
            else weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 2f * .2f);
            if (idleCounter >= 20 * Mathf.PI) idleCounter = 0;

        } else if (!isSprinting && !crouched) //Walking
        {

            headBob(movementCounter, .06f, .06f);
            movementCounter += Time.deltaTime * 6;
            if (!isAiming) weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * .2f);
            else weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 6f * .2f);
            if (movementCounter >= 20 * Mathf.PI) movementCounter = 0;

        } else if (crouched) //Crouched
        {

            headBob(movementCounter, .02f, .02f);
            movementCounter += Time.deltaTime * 1.75f;
            if (!isAiming) weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * .2f);
            else weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 6f * .2f);
            if (movementCounter >= 20 * Mathf.PI) movementCounter = 0;

        } else //Sprinting
        {

            headBob(movementCounter, .1f, .08f);
            movementCounter += Time.deltaTime * 8;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * .2f);
            if (movementCounter >= 20 * Mathf.PI) movementCounter = 0;

        }

        if (transform.position.y < -10) takeDamage(1000, -1);

        //UI Refreshes
        refreshHealthBar();
        weapon.refreshAmmo(UIAmmo);

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (!photonView.IsMine) return;

        //Axes
        float hmove = Input.GetAxisRaw("Horizontal");
        float vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprintKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jumpKeyDown = Input.GetKeyDown(KeyCode.Space);
        bool slideKeyDown = Input.GetKeyDown(KeyCode.F);
        bool crouchKeyDown = Input.GetKeyDown(KeyCode.LeftControl);

        //States
        bool isGrounded = groundTest();
        bool isJumping = jumpKeyDown && isGrounded;
        bool isSprinting = sprintKeyDown && vmove > 0 && !isJumping && !isAiming;
        bool isSliding = isSprinting && slideKeyDown && !sliding && isGrounded && !isAiming;
        bool isCrouching = crouchKeyDown && !isSprinting && !isJumping && isGrounded;

        if (Pause.paused)
        {

            hmove = 0f;
            vmove = 0f;
            sprintKeyDown = false;
            jumpKeyDown = false;
            slideKeyDown = false;
            crouchKeyDown = true;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
            isCrouching = false;

        }

        //Movement
        Vector3 direction = Vector3.zero;
        float adjustedSpeed = speed;
        if (!sliding)
        {

            direction = new Vector3(hmove, 0, vmove);
            direction.Normalize();
            direction = transform.TransformDirection(direction);

            if (isSprinting)
            {

                if (crouched) photonView.RPC("setCrouch", RpcTarget.All, !crouched);
                adjustedSpeed *= sprintMultiplier;

            } else if (crouched)
            {

                adjustedSpeed *= crouchModifier;

            } else if (isAiming) adjustedSpeed *= aimMultiplier;

        } else
        {

            direction = slideDirection;
            adjustedSpeed *= slideModifier;
            slideTime -= Time.deltaTime;
            if (slideTime <= 0 || isAiming)
            {

                sliding = false;
                weaponParentCurrentPosition += Vector3.up * (slideAmount - crouchAmount);

            }


        }

        Vector3 targetVelocity = direction * adjustedSpeed * Time.deltaTime;
        targetVelocity.y = rig.velocity.y;
        rig.velocity = targetVelocity;
        //rig.AddForce(direction * speed * Time.deltaTime, ForceMode.VelocityChange);

        if (isSliding)
        {

            sliding = true;
            slideDirection = direction;
            slideTime = slideDuration;
            weaponParentCurrentPosition += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("setCrouch", RpcTarget.All, !crouched);

        }

        //Aiming
        isAiming = weapon.aim(isAiming);

        //Camera Stuff
        if (sliding)
        {

            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVMultiplier * 1.2f, Time.deltaTime * 8f);
            normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);

            weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVMultiplier * 1.2f, Time.deltaTime * 8f);
            weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);

        } else {

            if (isSprinting)
            {

                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVMultiplier, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVMultiplier, Time.deltaTime * 8f);

            } else if (isAiming)
            {

                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 8f);

            } else {

                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);

            }

            if (crouched)
            {

                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);

            } else
            {

                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin, Time.deltaTime);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin, Time.deltaTime);

            }

        }

        //Animations
        float animHorizontal = 0f;
        float animVertical = 0f;

        if (isGrounded)
        {

            animHorizontal = direction.x;
            animVertical = direction.z;

        }

        anim.SetFloat("VelX", animHorizontal);
        anim.SetFloat("VelY", animVertical);

    }

    private void LateUpdate()
    {
        normalCam.transform.localPosition = normalCamTarget;
        weaponCam.transform.localPosition = weaponCamTarget;
    }

    #endregion

    #region Private Methods

    void RefreshMultiplayerState()
    {

        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;

    }

    [PunRPC]
    private void syncProfile(string username, int kills, int deaths, int rank, int sensitivity)
    {

        playerProfile = new ProfileData(username, kills, deaths, rank, sensitivity);
        playerUsername.text = username;

    }

    private void changeLayerRecursively(Transform trans, int layer)
    {

        trans.gameObject.layer = layer;
        foreach (Transform t in trans) changeLayerRecursively(t, layer);

    }

    bool groundTest()
    {

        RaycastHit hit;
        if (Physics.Raycast(groundDetector.position, Vector3.down, out hit, ground))
        {
            bool canJump = false;
            if (Mathf.Abs(previousGroundcast - hit.distance) < .01 * Time.deltaTime && hit.distance < .21) canJump = true;
            previousGroundcast = hit.distance;
            return canJump;

        }

        return false;

    }

    void headBob(float t, float xIntensity, float yIntensity)
    {

        float aimAdjust = 1f;
        if (isAiming) aimAdjust = 0.1f;
        targetWeaponBobPosition = weaponParentCurrentPosition + new Vector3(Mathf.Cos(t) * xIntensity * aimAdjust, Mathf.Sin(t * 2) * yIntensity * aimAdjust, 0);

    }

    #endregion

    #region Public Methods

    [PunRPC]
    public void takeDamage(int damage, int actor)
    {

        
        if (photonView.IsMine)
        {

            healthProjection -= damage;
            currentHealth -= damage;
            refreshHealthBar();

            currentRegenWait = regenDelay;

            if (currentHealth <= 0)
            {

                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
                PhotonNetwork.Destroy(gameObject);

                if (actor >= 0) manager.ChangeStat_S(actor, 0, 1);

            }

        }

    }

    private void refreshHealthBar()
    {

        if (currentHealth < maxHealth)
        {

            if (currentRegenWait > 0) currentRegenWait -= Time.deltaTime;
            else healthProjection += regenSpeed * Time.deltaTime;
            currentHealth = (int) healthProjection;

        }

        float healthRatio = (float)currentHealth / (float)maxHealth;
        UIHealthBar.localScale = Vector3.Lerp(UIHealthBar.localScale, new Vector3(healthRatio, 1, 1), Time.deltaTime * 10);

    }

    [PunRPC]
    void setCrouch(bool state)
    {

        if (crouched == state) return;
        crouched = state;

        if (crouched)
        {

            standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            weaponParentCurrentPosition += Vector3.down * crouchAmount;

        } else
        {

            standingCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPosition -= Vector3.down * crouchAmount;

        }

    }

    #endregion

}