using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviour
{
    #region Variables

    public float intensity;
    public float smooth;
    public bool isMine;

    private Quaternion originRotation;

    #endregion

    #region MonoBehaviour Callbacks

    // Start is called before the first frame update
    void Start()
    {

        originRotation = transform.localRotation;

    }

    // Update is called once per frame
    void Update()
    {

        if (Pause.paused) return;
        updateSway();

    }

    #endregion

    #region Private Methods

    private void updateSway()
    {

        //Controls
        float xMouse = Input.GetAxis("Mouse X");
        float yMouse = Input.GetAxis("Mouse Y");

        if (!isMine)
        {

            xMouse = 0;
            yMouse = 0;

        }

        //Calculate target rotation
        Quaternion xAdj = Quaternion.AngleAxis(-intensity * xMouse, Vector3.up);
        Quaternion yAdj = Quaternion.AngleAxis(intensity * yMouse, Vector3.right);
        Quaternion targetRotation = originRotation * xAdj * yAdj;

        //Perform rotation toward target
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);

    }

    #endregion

}
