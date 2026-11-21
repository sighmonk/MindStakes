using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CategoriesManager : Node
{
    public static CategoriesManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<CategoriesManager>("CategoriesManager");

    public List<Category> Categories { get; private set; } = new();
    public Category RandomCategory {get; set; }
    public override void _Ready()
    {
        LoadCategories();
        SetRandomCategory();
    }
    public void SetUpCategories()
    {
        LoadCategories();
        SetRandomCategory();
    }
    private void LoadCategories()
    {
        Categories.Clear();
        Categories.AddRange(CategoryStorage.LoadCategories());
        GD.Print($"Loaded {Categories.Count} category(ies) from '{CategoryStorage.RootPath}'.");
    }

    public void SetRandomCategory()
    {
        var validCategories = Categories.Where(c => QuestionsManager.Instance.HasQuestionsForCategory(c)).ToList();

        if (validCategories.Count == 0 || GameManager.Instance.RerollCount <= 0)
        {
            RandomCategory = null;
            return;
        }

        if (RandomCategory != null && validCategories.Count > 1)
        {
            var available = validCategories.Where(c => c != RandomCategory).ToList();
            var idx = GD.Randi() % available.Count;
            RandomCategory = available[(int)idx];
        }
        else
        {
            var randomIndex = GD.Randi() % validCategories.Count;
            RandomCategory = validCategories[(int)randomIndex];
        }
    }
    public List<Category> GetAllCategories()
    {
        return Categories;
    }
}
