using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARQuizManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField answerField;
    [SerializeField] TMP_Text QuestionText;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text levelText;

    [Header("Words & Objects")]
    [SerializeField] List<WordEntry> words = new List<WordEntry>();

    [Header("Spawn")]
    [SerializeField] Transform spawnParent;      // camera
    [SerializeField] float distanceFromCamera = 0.7f;

    int score = 0;
    int level = 1;

    GameObject currentObject;
    WordEntry currentWord;

    // word queue
    Queue<WordEntry> mainQueue = new Queue<WordEntry>();
    // words with mistakes- repeat
    List<WordEntry> reviewList = new List<WordEntry>();

    void Start()
    {
        // debug
        Debug.Log("[ARQuizManager] Start");

        if (words == null || words.Count == 0)
        {
            Debug.LogError("[ARQuizManager] words list is EMPTY! Заполни список слов в инспекторе.");
        }

        if (spawnParent == null)
        {
            // finding camera auto
            if (Camera.main != null)
            {
                spawnParent = Camera.main.transform;
                Debug.Log("[ARQuizManager] spawnParent был null, использую Camera.main");
            }
            else
            {
                Debug.LogError("[ARQuizManager] spawnParent == null и Camera.main не найдена. Объекты не смогут спавниться.");
            }
        }

        score = 0;
        level = 1;
        UpdateScoreText();
        levelText.text = $"Level {level}";

        PrepareMainQueue();
        NextObject();
    }

    void PrepareMainQueue()
    {
        mainQueue.Clear();

        List<WordEntry> shuffled = new List<WordEntry>(words);

        // mixing
        for (int i = 0; i < shuffled.Count; i++)
        {
            int j = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (var w in shuffled)
        {
            if (w == null)
            {
                Debug.LogWarning("[ARQuizManager] В списке words есть пустой элемент (null). Пропускаю его.");
                continue;
            }

            if (w.prefab == null)
            {
                Debug.LogWarning($"[ARQuizManager] У слова '{w.russian}' не задан prefab. Оно не будет показано.");
                continue;
            }

            mainQueue.Enqueue(w);
        }

        Debug.Log($"[ARQuizManager] Main queue prepared. Count = {mainQueue.Count}");
    }

    void NextObject()
    {
        // destroy old object
        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }

        // if all objects are named right
        if (mainQueue.Count == 0)
        {
            if (reviewList.Count > 0)
            {
                level++;
                levelText.text = $"Review {level - 1}";
                QuestionText.text = "Let's repeat the difficult words!";

                mainQueue = new Queue<WordEntry>(reviewList);
                reviewList.Clear();
            }
            else
            {
                QuestionText.text = "All objects completed!";
                levelText.text = "Level complete!";
                Debug.Log("[ARQuizManager] Quiz finished.");
                return;
            }
        }

        currentWord = mainQueue.Dequeue();
        Debug.Log($"[ARQuizManager] Next word: {currentWord.russian}");


        Transform t = spawnParent;
        if (t == null)
        {
            if (Camera.main != null)
            {
                t = Camera.main.transform;
                Debug.LogWarning("[ARQuizManager] spawnParent потерялся, беру Camera.main");
            }
            else
            {
                Debug.LogError("[ARQuizManager] Нет spawnParent и нет Camera.main — некуда спавнить объект.");
                return;
            }
        }

        if (currentWord.prefab != null)
        {
            Vector3 pos = t.position + t.forward * distanceFromCamera;
            currentObject = Instantiate(currentWord.prefab, pos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"[ARQuizManager] Prefab у слова '{currentWord.russian}' == null. Объект не будет показан.");
        }

        QuestionText.text = "Name this object in RUSSIAN:";
        answerField.text = "";
        answerField.ActivateInputField();
    }

    public void OnCheck()
    {
        if (currentWord == null)
            return;

        string user = Normalize(answerField.text);
        string correct = Normalize(currentWord.russian);

        if (string.IsNullOrWhiteSpace(user))
        {
            QuestionText.text = "Please type an answer first.";
            return;
        }

        if (user == correct)
        {
            score += 10;
            UpdateScoreText();
            QuestionText.text = $"Correct! The word is \"{currentWord.russian}\".";
        }
        else
        {
            QuestionText.text = $"Wrong! Correct answer: \"{currentWord.russian}\".";

            if (!reviewList.Contains(currentWord))
                reviewList.Add(currentWord);
        }

        StartCoroutine(NextObjectDelayed());
    }

    IEnumerator NextObjectDelayed()
    {
        yield return new WaitForSeconds(1.2f);
        NextObject();
    }


    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    string Normalize(string s)
    {
        if (s == null) return "";
        s = s.Trim().ToLowerInvariant();
        s = s.Replace('ё', 'е');
        return s;
    }
}
