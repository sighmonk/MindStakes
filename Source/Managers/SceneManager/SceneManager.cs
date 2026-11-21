using Godot;
using System;
using System.Collections.Generic;

public partial class SceneManager : Node
{
    public static SceneManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SceneManager>("SceneManager");
    public bool IsMenuOpen { get; set; }
    public Node CurrentMenu { get; set; }
    public List<string> SceneHistory { get; set; } = new List<string>();
    public Node CurrentScene { get; set; }
    public int CurrentSceneIndex { get; set; }
    private bool isFirstSceneRegistered = false;
    public void RegisterFirstScene()
    {
        if (isFirstSceneRegistered || SceneHistory.Count > 0)
        {
            return;
        }
        CurrentScene = GetTree().CurrentScene;
        if (CurrentScene != null)
        {
            SceneHistory.Add(CurrentScene.SceneFilePath);
            CurrentSceneIndex = 0;
        }
        isFirstSceneRegistered = true;
    }

    public bool IsHistoryNavigationBlocked => IsGameplayScene(GetTree().CurrentScene?.SceneFilePath);

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("GoBack"))
        {
            if (!IsHistoryNavigationBlocked)
            {
                GoBack();
            }
            GetViewport().SetInputAsHandled();
        }
        if (@event.IsActionPressed("GoForward"))
        {
            if (!IsHistoryNavigationBlocked)
            {
                GoForward();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public void ReplaceWithNewScene(string newScenePath)
    {
        if (CurrentSceneIndex < SceneHistory.Count - 1)
        {
            SceneHistory.RemoveRange(CurrentSceneIndex + 1, SceneHistory.Count - CurrentSceneIndex - 1);
        }

        SceneHistory.Add(newScenePath);
        CurrentSceneIndex = SceneHistory.Count - 1;

        ChangeScene(newScenePath);
    }

    private static bool IsGameplayScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            return false;
        }

        return scenePath.Contains("/Screens/GameModePick/", StringComparison.Ordinal)
            || scenePath.Contains("/Screens/CategoryPick/", StringComparison.Ordinal)
            || scenePath.Contains("/Screens/Betting/", StringComparison.Ordinal)
            || scenePath.Contains("/Screens/Question/", StringComparison.Ordinal)
            || scenePath.Contains("/Screens/ScoreScreen/", StringComparison.Ordinal)
            || scenePath.Contains("/Screens/Summary/", StringComparison.Ordinal);
    }

    public void ChangeScene(string newScenePath)
    {
        if (IsMenuOpen)
        {
            CurrentMenu?.QueueFree();
            CurrentMenu = null;
            IsMenuOpen = false;
        }

        GetTree().Paused = false;

        GetTree().ChangeSceneToFile(newScenePath);
    }

    public void GoBack()
    {
        if (IsHistoryNavigationBlocked)
        {
            return;
        }

        if (SceneHistory.Count > 0 && CurrentSceneIndex > 0)
        {
            CurrentSceneIndex -= 1;
            string previousScenePath = SceneHistory[CurrentSceneIndex];
            ChangeScene(previousScenePath);
        }
    }

    public void GoForward()
    {
        if (IsHistoryNavigationBlocked)
        {
            return;
        }

        if (SceneHistory.Count > 0 && CurrentSceneIndex < SceneHistory.Count - 1)
        {
            CurrentSceneIndex += 1;
            string nextScenePath = SceneHistory[CurrentSceneIndex];
            ChangeScene(nextScenePath);
        }
    }

    public void AddChildScene(string scenePath)
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene != null)
        {
            Node instance = scene.Instantiate();
            if (instance != null)
            {
                GetTree().Root.AddChild(instance);
                CurrentMenu = instance;
                IsMenuOpen = true;
            }
            else
            {
                GD.PrintErr($"Failed to instantiate scene at {scenePath}");
            }
        }
        else
        {
            GD.PrintErr($"Failed to load scene at {scenePath}");
        }
    }
}