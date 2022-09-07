using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{

    public static void saveProfile(ProfileData profile)
    {

        try
        {
            string path = Application.persistentDataPath + "/profile.dt";
            if (File.Exists(path)) File.Delete(path);

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, profile);
            file.Close();

            Debug.Log("Saved successfully");

        } catch
        {
            Debug.Log("Oopsie daisy... something happened that wasn't supposed to!");
        }

    }

    public static ProfileData loadProfile()
    {

        ProfileData ret = new ProfileData();

        try
        {
            string path = Application.persistentDataPath + "/profile.dt";
            if (File.Exists(path))
            {

                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                ret = (ProfileData)bf.Deserialize(file);

                Debug.Log("Loaded successfully");

            }
        } catch {

            Debug.Log("Oopsie daisy... something happened that wasn't supposed to!");

        }

        return ret;

    }

}
