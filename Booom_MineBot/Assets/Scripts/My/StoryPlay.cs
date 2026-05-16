using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryPlay : MonoBehaviour
{
    [Header("对话内容")]
    [SerializeField] private string[] storyLines;

    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("显示设置")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("场景设置")]
    [SerializeField] private string nextSceneName = "Gameplay";

    private int currentIndex = 0;
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (storyLines == null || storyLines.Length == 0)
        {
            Debug.LogWarning("[StoryPlay] 对话内容为空");
            return;
        }

        ShowCurrentLine();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (isTyping)
        {
            // 正在打字时点击，直接显示完整文字
            CompleteTyping();
        }
        else
        {
            // 打字完成时点击，播放下一句
            AdvanceToNextLine();
        }
    }

    private void ShowCurrentLine()
    {
        if (currentIndex >= storyLines.Length)
        {
            OnAllLinesPlayed();
            return;
        }

        if (textComponent != null)
        {
            textComponent.text = "";
            currentFullText = storyLines[currentIndex];
            StartTypingEffect();
        }
    }

    private void StartTypingEffect()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypingRoutine());
    }

    private IEnumerator TypingRoutine()
    {
        isTyping = true;
        int charIndex = 0;

        while (charIndex < currentFullText.Length)
        {
            charIndex++;
            if (textComponent != null)
            {
                textComponent.text = currentFullText.Substring(0, charIndex);
            }
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;
        if (textComponent != null)
        {
            textComponent.text = currentFullText;
        }
    }

    private void AdvanceToNextLine()
    {
        currentIndex++;
        ShowCurrentLine();
    }

    private void OnAllLinesPlayed()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}