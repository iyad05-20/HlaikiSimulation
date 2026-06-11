using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class GroqMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class GroqRequest
{
    public string model = "llama-3.3-70b-versatile";
    public List<GroqMessage> messages = new List<GroqMessage>();
    public float temperature = 0.8f;
    public int max_tokens = 500;
}

[System.Serializable]
public class GroqChoice
{
    public GroqMessage message;
}

[System.Serializable]
public class GroqResponse
{
    public List<GroqChoice> choices;
}

public class GroqApiClient : MonoBehaviour
{
    private static GroqApiClient _instance;
    public static GroqApiClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GroqApiClient>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";
    
    [Header("API Settings")]
    [Tooltip("Enter your Groq API Key here for testing")]
    public string apiKey = "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";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = "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";
        }
    }

    public IEnumerator SendChatRequest(List<GroqMessage> history, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            onError?.Invoke("API Key is missing! Please set it in the GroqApiClient component.");
            yield break;
        }

        GroqRequest requestData = new GroqRequest();
        requestData.messages = history;

        string jsonPayload = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            }
            else
            {
                try
                {
                    Debug.Log($"[GroqApiClient] Raw Response: {request.downloadHandler.text}");
                    GroqResponse response = JsonUtility.FromJson<GroqResponse>(request.downloadHandler.text);
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        onSuccess?.Invoke(response.choices[0].message.content);
                    }
                    else
                    {
                        onError?.Invoke("Invalid response format.");
                    }
                }
                catch (System.Exception e)
                {
                    onError?.Invoke("Failed to parse response: " + e.Message);
                }
            }
        }
    }
}
