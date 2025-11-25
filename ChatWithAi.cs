using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ChatWithAI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField questionInput;
    [SerializeField] TMP_Text answerText;

    [Header("Hugging Face")]
    [SerializeField] string hfToken = "hf_";
    [SerializeField] string modelId = "NousResearch/Hermes-2-Pro-Llama-3-8B";

    const string API_URL = "https://router.huggingface.co/v1/chat/completions";


    [TextArea(3, 6)]
    [SerializeField]
    string systemPrompt =
    "You are Foxxy, a friendly fox character and RUSSIAN language tutor in a mobile AR game. " +
    "The player writes in ENGLISH. " +
    "You ALWAYS answer in ENGLISH with RUSSIAN examples. ";




    public void OnAskButton()
    {
        string userQuestion = questionInput.text;

        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            answerText.text = "Foxxy: please type a question first.";
            return;
        }

        answerText.text = "Foxxy is thinking...";

        string wrappedQuestion =
            "REMINDER: follow all rules. " +
            "Answer in ENGLISH with RUSSIAN examples only. " +
            "Do NOT use Estonian, Ukrainian or other languages.\n\n" +
            userQuestion;

        StartCoroutine(SendChatRequest(wrappedQuestion));
    }




    IEnumerator SendChatRequest(string userQuestion)
    {
        
        string bodyJson = BuildRequestJson(userQuestion);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + hfToken);

            // sending request
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Foxy ERROR -> {www.responseCode} {www.error}\n{www.downloadHandler.text}");
                answerText.text = $"Foxy: ERROR -> {www.responseCode}\n{www.downloadHandler.text}";
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("HF raw response: " + json);

                string aiAnswer = ExtractAssistantText(json);
                answerText.text = "Foxy: " + aiAnswer;
            }
        }
    }

    string BuildRequestJson(string userQuestion)
    {
        string escSystem = Escape(systemPrompt);
        string escUser = Escape(userQuestion);


        string json =
            "{"
            + "\"model\":\"" + modelId + "\","
            + "\"messages\":["
                + "{\"role\":\"system\",\"content\":\"" + escSystem + "\"},"
                + "{\"role\":\"user\",\"content\":\"" + escUser + "\"}"
            + "],"
            + "\"max_tokens\":256,"
            + "\"temperature\":0.7"
            + "}";

        return json;
    }

    
    string Escape(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    
    string ExtractAssistantText(string json)
    {

        const string roleMark = "\"role\":\"assistant\"";
        int roleIndex = json.IndexOf(roleMark);
        if (roleIndex < 0) return json; 

        const string contentKey = "\"content\":\"";
        int contentIndex = json.IndexOf(contentKey, roleIndex);
        if (contentIndex < 0) return json;

        int start = contentIndex + contentKey.Length;


        int i = start;
        bool escaped = false;

        for (; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped)
            {
               
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
               
                escaped = true;
                continue;
            }

            if (c == '"')
            {
              
                break;
            }
        }

        if (i >= json.Length) return json;

        string raw = json.Substring(start, i - start);

       
        raw = raw.Replace("\\n", "\n")
                 .Replace("\\r", "\r")
                 .Replace("\\\"", "\"")
                 .Replace("\\\\", "\\");

        return raw;
    }

}
