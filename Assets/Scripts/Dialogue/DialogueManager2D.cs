using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DialogueManager2D : MonoBehaviour
{
    private string speakerName;
    private string[] lines;
    private int lineIndex;
    private bool isOpen;
    private int openedOnFrame;
    private int finishedOnFrame = -1;
    private Action onFinished;

    public bool IsOpen => isOpen;
    public bool BlocksWorldInput => isOpen || Time.frameCount == finishedOnFrame;

    public void StartDialogue(string speaker, string[] dialogueLines, Action finishedCallback = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            finishedCallback?.Invoke();
            return;
        }

        speakerName = speaker;
        lines = dialogueLines;
        lineIndex = 0;
        isOpen = true;
        openedOnFrame = Time.frameCount;
        onFinished = finishedCallback;
    }

    private void Update()
    {
        if (!isOpen || Time.frameCount == openedOnFrame)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    private void Advance()
    {
        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            FinishDialogue();
            return;
        }

        openedOnFrame = Time.frameCount;
    }

    private void FinishDialogue()
    {
        isOpen = false;
        finishedOnFrame = Time.frameCount;
        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        float panelWidth = Mathf.Min(900f, Screen.width - 40f);
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - 230f, panelWidth, 190f);
        GUI.Box(panelRect, GUIContent.none);

        GUIStyle speakerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        GUIStyle lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };

        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 16f, panelRect.width - 48f, 32f), speakerName, speakerStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 54f, panelRect.width - 48f, 78f), lines[lineIndex], lineStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 144f, panelRect.width - 48f, 28f), "Aperte E para continuar", GUI.skin.label);
    }
}
