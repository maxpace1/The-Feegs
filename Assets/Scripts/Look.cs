using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Look : MonoBehaviourPunCallbacks
{

    #region Variables

    public static bool cursorLocked = true;

    public Transform player;
    public Transform normalCam;
    public Transform weaponCam;
    public Transform weapon;

    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;

    private Quaternion camCenter;

    #endregion

    #region MonoBehaviour Callbacks

    // Start is called before the first frame update
    void Start()
    {

        camCenter = normalCam.localRotation; //Set origin rotation

    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine || Pause.paused) return;

        setX();
        setY();
        updateCursorLock();

        weaponCam.rotation = normalCam.rotation;

    }

    #endregion

    #region Private Methods

    void setX()
    {

        float input = Input.GetAxis("Mouse X") * xSensitivity * Launcher.myProfile.sensitivity / 50 * Time.deltaTime;
        if (GetComponent<Player>().isAiming) input *= GetComponent<Weapon>().currentGunData.aimSensitivityMultiplier;
        Quaternion adj = Quaternion.AngleAxis(input, Vector3.up);
        Quaternion delta = player.localRotation * adj;
        player.localRotation = delta;

    }

    void setY()
    {

        float input = Input.GetAxis("Mouse Y") * ySensitivity * Launcher.myProfile.sensitivity / 50 * Time.deltaTime;
        if (GetComponent<Player>().isAiming) input *= GetComponent<Weapon>().currentGunData.aimSensitivityMultiplier;
        Quaternion adj = Quaternion.AngleAxis(input, Vector3.left);
        Quaternion delta = normalCam.localRotation * adj;

        if (Quaternion.Angle(camCenter, delta) < maxAngle) normalCam.localRotation = delta;
        weapon.rotation = normalCam.rotation;

    }

    void updateCursorLock()
    {

        if (cursorLocked)
        {

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (Input.GetKeyDown(KeyCode.Escape)) cursorLocked = !cursorLocked;

        } else
        {

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape)) cursorLocked = !cursorLocked;

        }

    }

    #endregion

}
