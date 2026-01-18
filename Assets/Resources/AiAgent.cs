using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;



public class AiAgent : MonoBehaviour
{
    [Header("Highlighter")]
    public PrinterHighlighter highlighter;

    [Header("Voiceflow Configuration")]
    public string apiKey = "VF.DM.69408a82423694552798d787.5k0ArhEDjl059Yzl";
    public string versionID = "693ec6d2bdd798c48626df1b";
    private string userID = "unity_user_" + DateTime.Now.Ticks.ToString();

    [Header("Message Display")]
    public GameObject messagePrefab;     
    public Transform messageContent;     
    public ScrollRect scrollRect;
    public int maxMessages = 30;


    private bool isConversationStarted = true;
    private List<GameObject> currentMessages = new List<GameObject>();

    [Header("UI Elements")]
    public TMPro.TMP_InputField inputField; // Drag your Input Field here
    public Button sendButton;               // Drag your Send Button here



    [Serializable]
    private class VoiceflowRequest
    {
        public RequestData request;
        public ConfigData config;

        [Serializable]
        public class RequestData
        {
            public string type;
            public string payload;
        }

        [Serializable]
        public class ConfigData
        {
            public bool tts = false;
            public bool stripSSML = true;
            public bool stopTypes = true;
        }
    }

    // UPDATED: New response classes that match the actual JSON structure
    [Serializable]
    private class VoiceflowResponseItem
    {
        public long time;
        public string type;
        public ItemPayload payload;
    }

    [Serializable]
    private class ItemPayload
    {
        public bool enabled;
        public string[] delimiter;
        public int timeoutInSeconds;
        public int maxDigits;
        public TextPayload text;
        public SlatePayload slate;
        public bool ai;
        public string voice;
        public int delay;
        public string message;
    }

    [Serializable]
    private class TextPayload
    {
        public SlatePayload slate;
        public int delay;
        public string message;
        public bool ai;
        public string voice;
    }

    [Serializable]
    private class SlatePayload
    {
        public string id;
        public SlateContent[] content;
        public int messageDelayMilliseconds;
    }

    [Serializable]
    private class SlateContent
    {
        public SlateChildren[] children;
    }

    [Serializable]
    private class SlateChildren
    {
        public string text;
    }

    // Wrapper class for parsing JSON array
    [Serializable]
    private class ResponseWrapper
    {
        public VoiceflowResponseItem[] items;
    }

    [Header("Debug / Testing")]
    public bool useMockAI = false;

    void Start()
    {
        Debug.Log($"Starting AI Agent with UserID: {userID}");

        // FIX: Call it directly, not as a coroutine
        TestMessageLayout();

        // If you want to also test the real API later, uncomment:
        // StartCoroutine(LaunchAndSendMessage());
        if (!useMockAI)
        {
            StartCoroutine(LaunchAndSendMessage());
        }

    }

    // Make this a regular method (remove IEnumerator)
    void TestMessageLayout()
    {
        Debug.Log("Testing message layout...");

        // Clear existing
        foreach (Transform child in messageContent)
        {
            Destroy(child.gameObject);
        }

        currentMessages.Clear(); // Also clear the list

        // Add test messages with delay to see them appear
        StartCoroutine(AddTestMessagesWithDelay());
    }

    IEnumerator AddTestMessagesWithDelay()
    {
        // Add test messages one by one with a small delay
        ShowMessage("Hello! I'm your AI assistant. How can I help?", true);
        yield return new WaitForSeconds(0.3f);
        Debug.Log("Test messages added. Check if they're properly spaced.");
    }

    IEnumerator LaunchAndSendMessage()
    {
        Debug.Log("Starting Voiceflow conversation...");

        // Launch the conversation to initialize state
        yield return StartCoroutine(SendVoiceflowRequest(""));
    }
    public void OnSendButtonClicked()
    {
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage))
        {
            SendMessageToAI(userMessage);
            inputField.text = ""; 
            // clear input
        }
    }
    

    IEnumerator SendVoiceflowRequest(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.Log("Sending launch request to Voiceflow...");
        }
        else
        {
            Debug.Log($"Sending message to Voiceflow: {message}");
        }

        string url = $"https://general-runtime.voiceflow.com/state/user/{userID}/interact";

        VoiceflowRequest requestData = new VoiceflowRequest
        {
            request = new VoiceflowRequest.RequestData
            {
                type = string.IsNullOrEmpty(message) ? "launch" : "text",
                payload = message
            },
            config = new VoiceflowRequest.ConfigData
            {
                tts = false,
                stripSSML = true,
                stopTypes = true
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log($"Request JSON: {jsonPayload}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", apiKey);

            if (!string.IsNullOrEmpty(versionID))
            {
                www.SetRequestHeader("versionID", versionID);
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"✅ Voiceflow Response Success!");

                if (string.IsNullOrEmpty(message))
                {
                    Debug.Log("Conversation launched successfully!");
                    isConversationStarted = true;
                }

                // Process the response
                ProcessResponse(responseText);
            }
            else
            {
                Debug.LogError($" Voiceflow Request Failed!");
                Debug.LogError($"Status Code: {www.responseCode}");
                Debug.LogError($"Error: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");

                ShowMessage($"Error: {www.responseCode} - Could not connect to AI");
            }
        }
    }

    void ProcessResponse(string jsonResponse)
    {
        Debug.Log("Processing Voiceflow response...");

        try
        {
            // First, try to parse as an array of response items
            VoiceflowResponseItem[] responseItems = null;

            // Try parsing with the wrapper class
            string wrappedJson = "{\"items\":" + jsonResponse + "}";
            ResponseWrapper wrapper = JsonUtility.FromJson<ResponseWrapper>(wrappedJson);

            if (wrapper != null && wrapper.items != null && wrapper.items.Length > 0)
            {
                responseItems = wrapper.items;
                Debug.Log($"Parsed {responseItems.Length} response items using wrapper");
            }
            else
            {
                // Alternative parsing method
                Debug.Log("Trying alternative parsing with JsonHelper...");
                responseItems = JsonHelper.FromJson<VoiceflowResponseItem>(jsonResponse);

                if (responseItems != null && responseItems.Length > 0)
                {
                    Debug.Log($"Parsed {responseItems.Length} response items using JsonHelper");
                }
            }

            if (responseItems == null || responseItems.Length == 0)
            {
                Debug.LogWarning("No response items found, trying simple extraction...");
                string simpleText = ExtractTextSimple(jsonResponse);
                if (!string.IsNullOrEmpty(simpleText))
                {
                    ShowMessage(simpleText);
                    HandleAICommand(simpleText);
                }
                else
                {
                    ShowMessage("No response received from AI");
                }
                return;
            }

            // Extract all text from the response
            List<string> allTexts = new List<string>();

            foreach (var item in responseItems)
            {
                if (item.type == "text" && item.payload != null)
                {
                    // Try to get message from payload
                    if (!string.IsNullOrEmpty(item.payload.message))
                    {
                        allTexts.Add(item.payload.message);
                    }

                    // Try to get message from text payload
                    if (item.payload.text != null && !string.IsNullOrEmpty(item.payload.text.message))
                    {
                        allTexts.Add(item.payload.text.message);
                    }

                    // Try to extract text from slate
                    if (item.payload.slate != null && item.payload.slate.content != null)
                    {
                        string slateText = ExtractTextFromSlate(item.payload.slate);
                        if (!string.IsNullOrEmpty(slateText))
                        {
                            allTexts.Add(slateText);
                        }
                    }

                    // Also check text payload's slate
                    if (item.payload.text != null && item.payload.text.slate != null && item.payload.text.slate.content != null)
                    {
                        string slateText = ExtractTextFromSlate(item.payload.text.slate);
                        if (!string.IsNullOrEmpty(slateText))
                        {
                            allTexts.Add(slateText);
                        }
                    }
                }
            }

            // Combine all extracted text
            string combinedText = string.Join(" ", allTexts).Trim();

            if (string.IsNullOrEmpty(combinedText))
            {
                Debug.LogWarning("No text content extracted from response, trying simple extraction...");
                string simpleText = ExtractTextSimple(jsonResponse);
                if (!string.IsNullOrEmpty(simpleText))
                {
                    ShowMessage(simpleText);
                    HandleAICommand(simpleText);
                }
                else
                {
                    ShowMessage("AI responded, but no text was extracted.");
                }
            }
            else
            {
                Debug.Log($" Extracted AI Response: {combinedText}");
                ShowMessage(combinedText);
                // HandleAICommand(combinedText);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing Voiceflow response: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");

            // Fallback: Try simple text extraction
            string simpleText = ExtractTextSimple(jsonResponse);
            if (!string.IsNullOrEmpty(simpleText))
            {
                ShowMessage(simpleText);
                HandleAICommand(simpleText);
            }
            else
            {
                ShowMessage($"Error: {e.Message}");
            }
        }
    }

    string ExtractTextFromSlate(SlatePayload slate)
    {
        if (slate.content == null) return "";

        List<string> texts = new List<string>();
        foreach (var content in slate.content)
        {
            if (content.children != null)
            {
                foreach (var child in content.children)
                {
                    if (!string.IsNullOrEmpty(child.text))
                    {
                        texts.Add(child.text.Trim());
                    }
                }
            }
        }
        return string.Join(" ", texts).Trim();
    }

    string ExtractTextSimple(string json)
    {
        // Simple regex-free extraction for "message" field
        if (json.Contains("\"message\":\""))
        {
            int start = json.IndexOf("\"message\":\"") + 11;
            int end = json.IndexOf("\"", start);
            if (end > start)
            {
                string text = json.Substring(start, end - start);
                text = text.Replace("\\n", " ").Replace("\\\"", "\"");
                return text;
            }
        }

        // Also try looking for "text" fields in slate content
        List<string> foundTexts = new List<string>();
        int textIndex = json.IndexOf("\"text\":\"");
        while (textIndex > -1)
        {
            textIndex += 8; // Length of "\"text\":\""
            int textEnd = json.IndexOf("\"", textIndex);
            if (textEnd > textIndex)
            {
                string text = json.Substring(textIndex, textEnd - textIndex);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    foundTexts.Add(text);
                }
                textIndex = json.IndexOf("\"text\":\"", textEnd);
            }
            else
            {
                break;
            }
        }

        if (foundTexts.Count > 0)
        {
            return string.Join(" ", foundTexts).Trim();
        }

        return "";
    }

    // MESSAGE DISPLAY METHODS
    void ShowMessage(string text, bool isFromAI = true)
    {
        if (messagePrefab == null || messageContent == null)
        {
            Debug.LogError("Message prefab or message content is not assigned!");
            return;
        }

        // Format du texte
        string formattedText = isFromAI ? $"AI: {text}" : $"You: {text}";

        // Instanciation du message
        GameObject newMessage = Instantiate(messagePrefab, messageContent);

        // ✅ FORCER L'ORDRE D'AFFICHAGE
        newMessage.transform.SetAsLastSibling();

        // Appliquer le texte
        SetMessageText(newMessage, formattedText);

        // Enregistrer le message
        currentMessages.Add(newMessage);

        // Limiter le nombre de messages
        if (currentMessages.Count > maxMessages)
        {
            Destroy(currentMessages[0]);
            currentMessages.RemoveAt(0);
        }

        // ✅ FORCER LE RECALCUL DU LAYOUT
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            messageContent.GetComponent<RectTransform>()
        );

        // ✅ Scroll automatique vers le bas
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }


    void SetMessageText(GameObject messageObj, string text)
    {
        // Try TextMeshPro first
        TMPro.TextMeshProUGUI tmpComponent = messageObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpComponent != null)
        {
            tmpComponent.text = text;
            return;
        }

        // Try regular UI Text
        UnityEngine.UI.Text textComponent = messageObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
            return;
        }

        // Try TextMeshPro 3D
        TMPro.TextMeshPro tmp3D = messageObj.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp3D != null)
        {
            tmp3D.text = text;
            return;
        }

        Debug.LogWarning("No text component found in message prefab!");
    }
    IEnumerator MockAIResponse(string userMessage)
    {
        yield return new WaitForSeconds(0.4f);

        string lower = userMessage.ToLower();
        string aiResponse;

        if (lower.Contains("start") || lower.Contains("on"))
            aiResponse = "Press the Power button to start the printer.";
        else if (lower.Contains("cancel") || lower.Contains("stop"))
            aiResponse = "Press the Cancel button to stop printing.";
        else if (lower.Contains("restart") || lower.Contains("reset"))
            aiResponse = "Press the Restart button to reset the printer.";
        else
            aiResponse = "I can help you start, cancel, or restart the printer.";

        ShowMessage(aiResponse, true);
        HandleAICommand(aiResponse);
    }


    IEnumerator MockConversation()
    {
        // Wait a little to simulate AI thinking
        yield return new WaitForSeconds(0.5f);

        // Example: AI response about starting printer
        string aiResponse = "Press the Power button on the control panel.";
        ShowMessage(aiResponse, true);
        HandleAICommand(aiResponse);

        yield return new WaitForSeconds(1f);

        // Example: AI response about canceling print
        aiResponse = "Press the Cancel button to stop printing.";
        ShowMessage(aiResponse, true);
        HandleAICommand(aiResponse);

        yield return new WaitForSeconds(1f);

        // Example: AI response about restarting printer
        aiResponse = "Press the Restart button to reset the printer.";
        ShowMessage(aiResponse, true);
        HandleAICommand(aiResponse);
    }


    public void HandleAICommand(string aiText)
    {
        string lowerText = aiText.ToLower().Trim();
        Debug.Log($"Processing AI command: {lowerText}");

        // Define keyword mappings
        var keywordMapping = new Dictionary<string, string>()
        {
            { "power on", "PowerButton" },
            { "power button", "PowerButton" },
            { "turn on", "PowerButton" },
            { "press power", "PowerButton" },
            { "start printer", "PowerButton" },

            // CancelButton keywords
            { "cancel", "CancelButton" },
            { "stop print", "CancelButton" },
            { "stop printing", "CancelButton" },
            { "abort", "CancelButton" },

            // RestartButton keywords
            { "restart", "RestartButton" },
            { "reset", "RestartButton" },
            { "reboot", "RestartButton" }
        };

        // Check for keywords
        foreach (var mapping in keywordMapping)
        {
            if (lowerText.Contains(mapping.Key))
            {
                Debug.Log($"✅ Found keyword '{mapping.Key}' -> Highlighting '{mapping.Value}'");
                if (highlighter != null)
                {
                    highlighter.HighlightOnlyThisPart(mapping.Value);
                }
                else
                {
                    Debug.LogError("Highlighter reference is null!");
                }
                return;
            }
        }

        // No keyword found
        Debug.Log("No specific keyword found in AI response");
        if (highlighter != null)
        {
            highlighter.SetAllTransparent();
        }
    }

    // Public method to send messages from other scripts (e.g., UI button)
    public void SendMessageToAI(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        ShowMessage(message, false); // user message

        if (useMockAI)
        {
            StartCoroutine(MockAIResponse(message)); // ✅ NO API
        }
        else
        {
            if (isConversationStarted)
            {
                StartCoroutine(SendVoiceflowRequest(message)); // real API
            }
            else
            {
                ShowMessage("Please wait, AI is initializing...", true);
            }
        }
    }


    // Reset conversation with new user ID
    public void ResetConversation(string newUserID = null)
    {
        if (!string.IsNullOrEmpty(newUserID))
        {
            userID = newUserID;
        }
        else
        {
            userID = "unity_user_" + UnityEngine.Random.Range(1000, 9999);
        }

        isConversationStarted = false;

        // Clear all messages
        ClearAllMessages();

        ShowMessage($"New conversation started with ID: {userID}");

        Debug.Log($"Reset conversation with UserID: {userID}");
    }

    // Clear all messages
    public void ClearAllMessages()
    {
        foreach (GameObject msg in currentMessages)
        {
            if (msg != null) Destroy(msg);
        }
        currentMessages.Clear();
    }

    // Utility method to test the connection
    public void TestConnection()
    {
        Debug.Log("Testing Voiceflow connection...");
        ShowMessage("Testing connection...");
        StartCoroutine(SendVoiceflowRequest("Test connection"));
    }
}

// Helper class for parsing JSON arrays with JsonUtility
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}