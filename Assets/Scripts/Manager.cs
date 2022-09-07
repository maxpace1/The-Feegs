using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerInfo
{

    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;
    public short rank;

    public PlayerInfo (ProfileData p, int a, short k, short d)
    {

        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;

    }

}

public class Manager : MonoBehaviour, IOnEventCallback
{

    #region Fields

    public string playerPrefabString;
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;

    private Text UIKills;
    private Text UIDeaths;
    private Transform UILeaderboard;

    #endregion

    #region Codes
    public enum EventCodes : byte
    {

        NewPlayer,
        UpdatePlayers,
        ChangeStat

    }

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {

        ValidateConnection();
        initializeUI();
        NewPlayer_S(Launcher.myProfile);
        Spawn();

    }

    private void Update()
    {

        if (Input.GetKey(KeyCode.Tab)) leaderboard(UILeaderboard);
        else UILeaderboard.gameObject.SetActive(false);

    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    #endregion

    #region Methods

    private void initializeUI ()
    {

        UIKills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<Text>();
        UIDeaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<Text>();
        UILeaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;

        refreshStats();

    }

    private void refreshStats()
    {

        if (playerInfo.Count > myind)
        {

            UIKills.text = $"Kills - {playerInfo[myind].kills}";
            UIDeaths.text = $"Deaths - {playerInfo[myind].deaths}";

        } else {

            UIKills.text = "Kills - 0";
            UIDeaths.text = "Deaths - 0";

        }

    }

    public void Spawn()
    {

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        PhotonNetwork.Instantiate(playerPrefabString, spawnPoint.position, spawnPoint.rotation);

    }

    private void leaderboard(Transform board)
    {

        for (int i = 2; i < board.childCount; i++) Destroy(board.GetChild(i).gameObject);

        board.Find("Header/Mode").GetComponent<Text>().text = "FREE FOR ALL";
        board.Find("Header/Map").GetComponent<Text>().text = "GEOMETRY";

        GameObject playerCard = board.GetChild(1).gameObject;
        playerCard.SetActive(false);

        List<PlayerInfo> sortedInfo = sortPlayers(playerInfo);

        bool alternateColor = false;
        foreach (PlayerInfo a in sortedInfo)
        {

            GameObject newCard = Instantiate(playerCard, board) as GameObject;
            if (alternateColor) newCard.GetComponent<Image>().color = new Color32(0,12,43,100);
            alternateColor = !alternateColor;

            newCard.transform.Find("Rank").GetComponent<Text>().text = a.profile.rank.ToString("D2");
            newCard.transform.Find("Username").GetComponent<Text>().text = a.profile.username;
            newCard.transform.Find("Kills").GetComponent<Text>().text = a.kills.ToString("D2");
            newCard.transform.Find("Deaths").GetComponent<Text>().text = a.deaths.ToString("D2");

            newCard.SetActive(true);

        }

        board.gameObject.SetActive(true);

    }

    private List<PlayerInfo> sortPlayers(List<PlayerInfo> info)
    {

        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < info.Count)
        {

            short highest = -1;

            PlayerInfo selection = info[0];
            foreach (PlayerInfo a in info)
            {

                if (sorted.Contains(a)) continue;
                if (a.kills > highest)
                {

                    highest = a.kills;
                    selection = a;

                }

            }

            sorted.Add(selection);

        }

        return sorted;

    }

    private void ValidateConnection()
    {

        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(0);

    }

    #endregion

    #region Photon

    public void OnEvent (EventData photonEvent)
    {

        if (photonEvent.Code >= 200) return;
        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {

            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                break;
            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;
            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;

        }

    }

    #endregion

    #region Events

    public void NewPlayer_S(ProfileData p)
    {

        object[] package = new object[8];

        package[0] = p.username;
        package[1] = p.kills;
        package[2] = p.deaths;
        package[3] = p.rank;
        package[4] = p.sensitivity;
        package[5] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[6] = (short)0;
        package[7] = (short)0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );

    }

    public void NewPlayer_R(object[] data)
    {

        PlayerInfo p = new PlayerInfo(new ProfileData((string)data[0], (int)data[1], (int)data[2], (int)data[3], (int)data[4]), (int)data[5], (short)data[6], (short)data[7]);
        playerInfo.Add(p);
        UpdatePlayers_S(playerInfo);

    }

    public void UpdatePlayers_S(List<PlayerInfo> info)
    {

        object[] package = new object[info.Count];

        for (int i = 0; i < info.Count; i++)
        {

            object[] piece = new object[8];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.kills;
            piece[2] = info[i].profile.deaths;
            piece[3] = info[i].profile.rank;
            piece[4] = info[i].profile.sensitivity;
            piece[5] = info[i].actor;
            piece[6] = info[i].kills;
            piece[7] = info[i].deaths;

            package[i] = piece;

        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );

    }

    public void UpdatePlayers_R(object[] data)
    {

        playerInfo = new List<PlayerInfo>();

        for (int i = 0; i < data.Length; i++)
        {

            object[] extract = (object[])data[i];

            PlayerInfo p = new PlayerInfo(new ProfileData((string)extract[0], (int)extract[1], (int)extract[2], (int)extract[3], (int)extract[4]), (int)extract[5], (short)extract[6], (short)extract[7]);
            playerInfo.Add(p);

            if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor) myind = i;

        }

    }

    public void ChangeStat_S(int actor, byte stat, byte amt)
    {

        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );

    }

    public void ChangeStat_R(object[] data)
    {

        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];

        for (int i = 0; i < playerInfo.Count; i++) if (playerInfo[i].actor == actor)
            {
                switch (stat)
                {

                    case 0: //Kills
                        playerInfo[i].kills += amt;
                        if (i == myind)
                        {
                            Launcher.myProfile.kills += amt;
                            if (Launcher.myProfile.kills % 20 == 0) Launcher.myProfile.rank++;
                            Data.saveProfile(Launcher.myProfile);
                            Weapon.redHitmarker = 1f;
                        }
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].profile.kills}");
                        break;
                    case 1: //Deaths
                        playerInfo[i].deaths += amt;
                        if (i == myind)
                        {
                            Launcher.myProfile.deaths += amt;
                            Data.saveProfile(Launcher.myProfile);
                        }
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].profile.deaths}");
                        break;

                }

                if (i == myind) refreshStats();
                if (UILeaderboard.gameObject.activeSelf) leaderboard(UILeaderboard);
                return;

            }

    }

    #endregion

}