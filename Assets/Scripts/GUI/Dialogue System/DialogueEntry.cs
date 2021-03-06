﻿using UnityEngine;
using System.Collections;
using System;

public class DialogueEntry
{
    public static int ENTRY_WIDTH = Screen.width;
    public static int ENTRY_HEIGHT = (int)(Screen.height * 0.1f);

    [SerializeField]
    Author author;
    public Author Author
    {
        get { return author; }
    }

    [SerializeField]
    [Multiline]
    string content;
    public string Content
    {
        get { return content; }
    }

    public DialogueEntry(Author author, string content = "Content")
    {
        this.author = author;
        this.content = content;
    }

    public void DisplayEntry(string displayedText)
    {
        if (Author.Avatar != null)
        {
            float portraitX = Author.GuiRectangle.x;
            switch (Author.Alignment)
            {
                case TextAnchor.UpperLeft:
                    portraitX = Author.GuiRectangle.x;
                    break;
                case TextAnchor.UpperRight:
                    portraitX = Author.GuiRectangle.x + Author.GuiRectangle.width - Author.GuiRectangle.height;
                    break;
                case TextAnchor.UpperCenter:
                    portraitX = Author.GuiRectangle.x + Author.GuiRectangle.width / 2.0f - Author.GuiRectangle.height;
                    break;
            }
            GUI.DrawTexture(new Rect(portraitX, Author.GuiRectangle.y - Author.GuiRectangle.height, Author.GuiRectangle.height, Author.GuiRectangle.height), Author.Avatar);
        }

        GUILayout.BeginArea(Author.GuiRectangle);

		GUIStyle contentStyle;

		if (Author.Name != "[Narrator]") {
			GUIStyle authorStyle = GUIManager.Style.author;
			authorStyle.alignment = Author.Alignment;

			authorStyle.normal.textColor = Author.TextColor;
            GUILayout.Label(Author.Name, authorStyle);
			contentStyle = GUIManager.Style.content;
		} else {
			contentStyle = GUIManager.Style.narratorContent;
		}

		///= GUIManager.Style.content;

        contentStyle.normal.textColor = Author.TextColor + new Color(0.5f, 0.5f, 0.5f);
        GUILayout.Label(displayedText, contentStyle);

        GUILayout.EndArea();
    }

    public bool DisplayContinueButton(GUIStyle style)
    {
        float nextX = Author.GuiRectangle.x + Author.GuiRectangle.width - Screen.height / 8.0f;
        if(Author.Alignment == TextAnchor.UpperCenter)
            nextX = Screen.width / 2.0f - Screen.height / 26.0f;

        return GUI.Button(new Rect(nextX, Author.GuiRectangle.y + Author.GuiRectangle.height - Screen.height / 8.0f, Screen.height / 13.0f, Screen.height / 13.0f), GUIContent.none, style);
    }
}