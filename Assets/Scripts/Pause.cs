using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class Pause : MonoBehaviour
{

    public static bool paused = false;
    private bool disconnecting = false;

    public void togglePause()
    {

        if (disconnecting) return;

        paused = !paused;

        transform.GetChild(0).gameObject.SetActive(paused);
        Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = paused;

    }

    public void quit()
    {

        disconnecting = true;
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);

    }

}
