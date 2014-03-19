﻿using UnityEngine;
using System.Collections;
using System;

public class DialogueEntry
{
    public static int ENTRY_WIDTH = 300;
    public static int ENTRY_HEIGHT = 150;

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

	static readonly float relSize = 0.1f;

    public void Display(GUISkin guiSkin, string displayedText)
    {
        if(Author.Avatar != null)
            GUI.DrawTexture(new Rect(Author.ScreenPosition.x - ENTRY_HEIGHT, Author.ScreenPosition.y, ENTRY_HEIGHT, ENTRY_HEIGHT), Author.Avatar);

        GUILayout.BeginArea(new Rect(Author.ScreenPosition.x, Author.ScreenPosition.y, ENTRY_WIDTH, ENTRY_HEIGHT));

        if (Author.Name != "[Narrator]")
        {
            GUIStyle authorStyle = guiSkin.GetStyle("author");

            authorStyle.normal.textColor = Author.TextColor;
            GUILayout.Label(Author.Name + ":>", authorStyle);
        }

        GUIStyle contentStyle = guiSkin.GetStyle("content");

        contentStyle.normal.textColor = Author.TextColor + new Color(0.5f, 0.5f, 0.5f);
        GUILayout.Label(displayedText, contentStyle);

        GUILayout.EndArea();
    }

    public bool DisplayButton(GUIStyle style)
    {
		//return GUI.Button(new Rect(Author.ScreenPosition.x + ENTRY_WIDTH - Screen.width * 0.05f, Author.ScreenPosition.y + ENTRY_HEIGHT,  Screen.width * 0.05f, Screen.width * 0.05f), GUIContent.none, style);
		return GUI.Button(new Rect(
			(Screen.width * (1 - relSize)) / 2, Screen.height - relSize * Screen.width, Screen.width * relSize, Screen.width * relSize), GUIContent.none, style);
    }
}