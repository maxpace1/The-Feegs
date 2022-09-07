using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[System.Serializable]
public class ProfileData
{

    public string username;
    public int kills;
    public int deaths;
    public int sensitivity;
    public int rank;

    public ProfileData (string u, int k, int d, int s, int r)
    {

        this.username = u;
        this.kills = k;
        this.deaths = d;
        this.sensitivity = s;
        this.rank = r;

    }

    public ProfileData()
    {

        this.username = "";
        this.kills = 0;
        this.deaths = 0;
        this.sensitivity = 50;
        this.rank = 0;

    }

}

public class Launcher : MonoBehaviourPunCallbacks
{

    public InputField usernameField;
    public Slider sensitivitySlider;
    public static ProfileData myProfile = new ProfileData();

    private Text UISensitivityLabel;
    private Text UIKillsLabel;
    private Text UIDeathsLabel;
    private Text UIRankLabel;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        myProfile = Data.loadProfile();
        usernameField.text = myProfile.username;
        UIKillsLabel = GameObject.Find("Canvas/Profile/Kills").GetComponent<Text>();
        UIDeathsLabel = GameObject.Find("Canvas/Profile/Deaths").GetComponent<Text>();
        UIRankLabel = GameObject.Find("Canvas/Rank").GetComponent<Text>();
        UIKillsLabel.text = "KILLS " + myProfile.kills.ToString("D2");
        UIDeathsLabel.text = "DEATHS " + myProfile.deaths.ToString("D2");
        UIRankLabel.text = "RANK " + myProfile.rank.ToString("D2");

        Connect();

    }

    public override void OnConnectedToMaster()
    {

        //Join();
        Debug.Log("Connected!");
        base.OnConnectedToMaster();

    }

    public override void OnJoinedRoom()
    {

        startGame();

        base.OnJoinedRoom();

    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        
        Create();

        base.OnJoinRandomFailed(returnCode, message);

    }

    public void Connect()
    {
        
        PhotonNetwork.GameVersion = "0.0.0";
        PhotonNetwork.ConnectUsingSettings();

    }

    public void Join()
    {

        VerifyUsername();
        PhotonNetwork.JoinRandomRoom();

    }

    public void Create()
    {

        PhotonNetwork.CreateRoom("");

    }

    private void VerifyUsername ()
    {

        if (string.IsNullOrEmpty(usernameField.text)) myProfile.username = "Snuzzleface_" + Random.Range(100, 1000);
        else myProfile.username = usernameField.text;

    }
    
    public void startGame()
    {

        VerifyUsername();
        myProfile.sensitivity = (int) sensitivitySlider.value;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {

            Data.saveProfile(myProfile);
            PhotonNetwork.LoadLevel(1);

        }

    }

    public void Update()
    {

        UISensitivityLabel = GameObject.Find("Canvas/Profile/Sensitivity/Value").GetComponent<Text>();
        UISensitivityLabel.text = "" + sensitivitySlider.value;

    }

}