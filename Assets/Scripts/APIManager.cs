using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager
{
    public static IEnumerator Auth(string email, string password, System.Action<int> callback = null)
    {
        // make email lower case and remove any random spaces
        email = email.ToLowerInvariant();
        email = email.Replace(" ", "");
        password = password.Replace(" ", "");

        string url = UnityEngine.RemoteSettings.GetString("ApiAuthURL", "https://api.surewash.net/api/auth/login/");
        WWWForm form = new WWWForm();

        if (email == "anonymous.user@surewash.com")
        {
            PlayerPrefs.SetInt("AnonymousLogin", 1);
        }
        else
        {
            PlayerPrefs.SetInt("AnonymousLogin", 0);
        }

        form.AddField("email", email);
        form.AddField("password", password);

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        www.timeout = UnityEngine.RemoteSettings.GetInt("ApiTimeOutSeconds", 3);

        yield return www.Send();

        //yield return www.SendWebRequest();

        if (www.isError)
        {
            UnityEngine.Debug.LogError("APIManager:Auth - Network Error");

            PlayerPrefs.SetInt("APITokenSet", -2);
            if (callback != null)
                callback(-3);
        }
        else if (www.isError)
        {
            UnityEngine.Debug.LogError("APIManager:Auth - Http Error");

            PlayerPrefs.SetInt("APITokenSet", -2);
            if (callback != null)
                callback(-2);
        }
        else
        {
            string apiTokenText = www.downloadHandler.text;
            try
            {
                Token token = JsonUtility.FromJson<Token>(apiTokenText);
                if (token.token != null)
                {
                    PlayerPrefs.SetString("APIToken", token.token);
                    PlayerPrefs.SetInt("APITokenSet", 1);
                    SaveCredentials(email, password);
                    if (callback != null)
                        callback(1);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("APIManager:Auth - Error decoding json token");
            }
        }
    }

    public static IEnumerator GetSettings(System.Action<int, RemoteAPISettings> callback)
    {
        string url = UnityEngine.RemoteSettings.GetString("ApiSettingsURL", "https://api.surewash.net/api/settings/");

        yield return ProcessAPIRequest<System.Action<int, RemoteAPISettings>>(url, true, null, FinishGetSettings, callback);
    }

    public static IEnumerator PostGoalComplete(string goalName, System.Action<int> callback = null)
    {
        Dictionary<string, string> formData = new Dictionary<string, string>();

        formData.Add("goal_name", goalName);
        formData.Add("device_id", SystemInfo.deviceUniqueIdentifier);
        formData.Add("device_type", SystemInfo.deviceModel);
        formData.Add("date_achieved", DateTime.Now.ToString("o"));

        yield return PostData(formData, "ApiGoalCompleteURL", callback, true);
    }



    public static IEnumerator PostInformation(string information, System.Action<int> callback = null)
    {
        Dictionary<string, string> formData = new Dictionary<string, string>();

        formData.Add("device_id", SystemInfo.deviceUniqueIdentifier);
        formData.Add("log_entry", information);
        formData.Add("timestamp", DateTime.Now.ToString("o"));

        yield return PostData(formData, "ApiLogURL", callback, true);
    }

    public static IEnumerator PostHandyMDLevel(bool passed, DateTime startTime, DateTime endTime, string level, int score, int stars, string levelOverReason, System.Action<int> callback = null)
    {
        Dictionary<string, string> formData = new Dictionary<string, string>();

        formData.Add("DeviceID", SystemInfo.deviceUniqueIdentifier);
        formData.Add("DeviceType", SystemInfo.deviceModel);
        formData.Add("Passed", passed.ToString());
        formData.Add("StartTime", startTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
        formData.Add("EndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
        formData.Add("StartUTCTime", startTime.ToString("o"));
        formData.Add("EndUTCTime", endTime.ToString("o"));
        formData.Add("Level", level);
        formData.Add("Score", score.ToString());
        formData.Add("Stars", stars.ToString());
        formData.Add("ReasonForLevelEnd", levelOverReason);
        formData.Add("SoftwareVersion", Application.version);

        yield return PostData(formData, "ApiHandyMDLevelURL", callback, true);
    }

    public static IEnumerator PostHandyMDOpened(System.Action<int> callback = null)
    {
        Dictionary<string, string> formData = new Dictionary<string, string>();

        DateTime timeStamp = DateTime.Now;

        formData.Add("DeviceID", SystemInfo.deviceUniqueIdentifier);
        formData.Add("DeviceType", SystemInfo.deviceModel);
        formData.Add("TimestampLocal", timeStamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
        formData.Add("TimestampUTC", timeStamp.ToString("o"));
        formData.Add("SoftwareVersion", Application.version);

        yield return PostData(formData, "ApiHandyMDOpenedURL", callback, true);
    }


    private static IEnumerator ProcessAPIRequest<T>(string url, bool get, WWWForm formData, System.Action<int, string, T, APIData> callback, T finalCallback, APIData data = null)
    {
        UnityWebRequest www;
        if (get) www = UnityWebRequest.Get(url);
        else www = UnityWebRequest.Post(url, formData);


        bool finished = false;

        // If no token exists - load credentials and create one
        if (PlayerPrefs.GetInt("APITokenSet", 0) <= 0)
        {
            Debug.Log("APIManager - ProcessAPIRequest - no token found");
            Credentials credentials = LoadCredentials();
            Debug.Log("APIManager - ProcessAPIRequest - attempting re-auth using user: " + credentials.email);
            yield return Auth(credentials.email, credentials.redeemCode);
        }

        // If there is still no token finish
        if (PlayerPrefs.GetInt("APITokenSet", 0) <= 0)
        {
            Debug.Log("APIManager - ProcessAPIRequest - still no token found even after re-auth");
            callback(-1, "", finalCallback, data);
            finished = true;
        }

        // If not finished make attempt request, finish if attempt has Network error
        if (!finished)
        {
            string apiToken = PlayerPrefs.GetString("APIToken", "");
            www.SetRequestHeader("Authorization", "JWT " + apiToken);

            www.timeout = UnityEngine.RemoteSettings.GetInt("ApiTimeOutSeconds", 3);

            yield return www.Send();

            //yield return www.SendWebRequest();

            if (www.isError)
            {
                Debug.Log("APIManager - ProcessAPIRequest - network error. Code: " + www.responseCode + " Message: " + www.downloadHandler.text);
                callback(-3, "", finalCallback, data);
                finished = true;
            }
        }

        // If not finished but had http error try re-authenticating and retry;
        if (!finished && www.isError)
        {
            if (www.downloadHandler.text.Contains("Duplicate RecordID Error"))
            {
                string apiRequestText = www.downloadHandler.text;
                callback(2, apiRequestText, finalCallback, data);
            }
            else
            {
                Debug.Log("APIManager - ProcessAPIRequest - http error in request - most likely expired token. Code: " + www.responseCode + " Message: " + www.downloadHandler.text);
                Credentials credentials = LoadCredentials();
                Debug.Log("APIManager - ProcessAPIRequest - attempting re-auth using user: " + credentials.email);
                yield return Auth(credentials.email, credentials.redeemCode);

                if (PlayerPrefs.GetInt("APITokenSet", 0) <= 0)
                {
                    Debug.Log("APIManager - ProcessAPIRequest - error re-auth user after likely expired token. Code: " + www.responseCode + " Message: " + www.downloadHandler.text);
                    callback(-2, "", finalCallback, data);
                    finished = true;
                }
                else
                {
                    if (get) www = UnityWebRequest.Get(url);
                    else www = UnityWebRequest.Post(url, formData);

                    string apiToken = PlayerPrefs.GetString("APIToken", "");
                    www.SetRequestHeader("Authorization", "JWT " + apiToken);
                    www.timeout = UnityEngine.RemoteSettings.GetInt("ApiTimeOutSeconds", 3);
                    yield return www.Send();

                    //yield return www.SendWebRequest();

                    if (www.isError)
                    {
                        Debug.Log("APIManager - ProcessAPIRequest - http error after re-auth user. Code: " + www.responseCode + " Message: " + www.downloadHandler.text);
                        callback(-2, "", finalCallback, data);
                        finished = true;
                    }
                }
            }
        }

        if (!finished)
        {
            string apiRequestText = www.downloadHandler.text;
            callback(1, apiRequestText, finalCallback, data);
        }
    }



    private static IEnumerator PostData(Dictionary<string, string> formData, string ApiURL, System.Action<int> callback = null, bool SaveOnFail = false)
    {
        Debug.Log("APIManager - PostData - starting post data to " + ApiURL);

        APIData apiData = new APIData();
        apiData.saveOnFail = SaveOnFail;
        apiData.formData = formData;
        apiData.url = ApiURL;

        string url = UnityEngine.RemoteSettings.GetString(ApiURL, "");

        if (url == "")
        {
            if (ApiURL == "ApiLogURL") url = "https://api.surewash.net/api/log/";
            if (ApiURL == "ApiSessionsURL") url = "https://api.surewash.net/api/sessions/";
            if (ApiURL == "ApiGoalCompleteURL") url = "https://api.surewash.net/api/goals/";
            if (ApiURL == "ApiHandyMDOpenedURL") url = "https://api.surewash.net/api/handymdopened";
            if (ApiURL == "ApiHandyMDLevelURL") url = "https://api.surewash.net/api/handymdlevel";
        }

        if (PlayerPrefs.GetInt("AnonymousLogin", 1) == 1 && UnityEngine.RemoteSettings.GetInt("APISendAnomousData", 0) == 0)
        {
            if (callback != null)
                callback(-4);
            if (SaveOnFail)
                SaveData(apiData);
            yield break;
        }
        else
        {
            WWWForm form = new WWWForm();

            foreach (KeyValuePair<string, string> entry in formData)
            {
                form.AddField(entry.Key, entry.Value);
            }

            yield return ProcessAPIRequest<System.Action<int>>(url, false, form, FinishPostData, callback, apiData);
        }
    }


    private static void FinishPostData(int status, string responce, System.Action<int> callback, APIData data)
    {
        if (callback != null)
            callback(status);
        if (data != null && data.saveOnFail && status < 1)
            SaveData(data);
    }
    

    private static void FinishGetSettings(int status, string responce, System.Action<int, RemoteAPISettings> callback, APIData data)
    {
        if (status == 1)
        {
            RemoteAPISettings settings = JsonUtility.FromJson<RemoteAPISettings>(responce);

            if (settings != null)
                callback(status, settings);
            else
                callback(-4, null);
        }
        else
        {
            callback(status, null);
        }
    }




    private static void SaveCredentials(string email, string redeemCode)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            if (!Directory.Exists(Application.persistentDataPath + "/apiData2")) Directory.CreateDirectory(Application.persistentDataPath + "/apiData2");
            FileStream stream = new FileStream(Application.persistentDataPath + "/apiData2/APICred.hhs", FileMode.Create);

            Credentials data = new Credentials();
            data.email = email;
            data.redeemCode = redeemCode;

            bf.Serialize(stream, data);
            stream.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError("APIManager - SaveCredentials" + ex.Message);
        }
    }

    public static Credentials LoadCredentials()
    {
        try
        {

            if (File.Exists(Application.persistentDataPath + "/apiData2/APICred.hhs"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream stream = new FileStream(Application.persistentDataPath + "/apiData2/APICred.hhs", FileMode.Open);

                Credentials data = bf.Deserialize(stream) as Credentials;
                stream.Close();

                return data;
            }
            else
            {
                Credentials cred = new Credentials();
                cred.email = "anonymous.user@surewash.com";
                cred.redeemCode = "27L7X2";
                Debug.Log("APIManager - LoadCredentials - Defaulting to anonymous credentials");
                return cred;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("APIManager - LoadCredentials - " + ex.Message);
            return null;
        }
    }

    private static void SaveData(APIData data)
    {
        if(data != null)
            SaveData(data.formData, data.url);
    }


    private static void SaveData(Dictionary<string, string> formData, string url)
    {
        try
        {
            Debug.Log("APIManager - SaveData - Starting saving");
            string dataName = "data_" + String.Format("{0:yyyyMMdd_HHmmss}", DateTime.Now);
            BinaryFormatter bf = new BinaryFormatter();
            if (!Directory.Exists(Application.persistentDataPath + "/apiData")) Directory.CreateDirectory(Application.persistentDataPath + "/apiData");
            FileStream stream = new FileStream(Application.persistentDataPath + "/apiData/" + dataName + ".hhs", FileMode.Create);

            APIData data = new APIData();
            data.formData = formData;
            data.url = url;

            bf.Serialize(stream, data);
            stream.Close();
            Debug.Log("APIManager - SaveData - Finished saving");
        }
        catch (Exception ex)
        {
            Debug.LogError("APIManager - SaveData" + ex.Message);
        }
    }


    public static IEnumerator LoadAndSend()
    {
        Debug.Log("APIManager - LoadAndSend - starting load and save");
        if (PlayerPrefs.GetInt("AnonymousLogin", 1) == 1 && UnityEngine.RemoteSettings.GetInt("APISendAnomousData", 0) == 0)
        {
            Debug.Log("APIManager - LoadAndSend - not sending anoymous data");
            yield break;
        }

        string apiDataDirectoryPath = Application.persistentDataPath + "/apiData";

        if (Directory.Exists(apiDataDirectoryPath))
        {
            Debug.Log("APIManager - LoadAndSend - Directory exists");
            var allFiles = Directory.GetFiles(apiDataDirectoryPath);
            Debug.Log("APIManager - LoadAndSend - " + allFiles.Length.ToString() + "files to send");
            // Load each of the files
            foreach (var file in allFiles)
            {
                var loadedAPIData = LoadAPIDataUsingPath(file);

                if (loadedAPIData != null)
                {
                    yield return PostData(loadedAPIData.formData, loadedAPIData.url, (int i) => { if (i == 1) File.Delete(file); });
                }
                else
                {
                    Debug.Log("APIManager - LoadAndSend - File not loaded correctly - " + file);
                    File.Delete(file);
                }
            }
        }
    }



    private static APIData LoadAPIDataUsingPath(string apiDataPath)
    {
        if (File.Exists(apiDataPath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(apiDataPath, FileMode.Open);

            APIData data = bf.Deserialize(stream) as APIData;
            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("File does not exist.");
            return null;
        }
    }
}

[Serializable]
public class APIData
{
    public bool saveOnFail = false;
    public Dictionary<string, string> formData;
    public string url;
}

[Serializable]
public class Credentials
{
    public string email;
    public string redeemCode;
}


[Serializable]
public class Token
{
    public string token;
    public string non_field_errors;
}


[Serializable]
public class RemoteAPISettings
{
    public string detail;
    public string setting_id;
    public string image_url;
    public string info_url;
    public Site site;
}

[Serializable]
public class Site
{
    public int id;
    public string site_name;
    public string city;
    public string country;
    public object parent_organization;
}

