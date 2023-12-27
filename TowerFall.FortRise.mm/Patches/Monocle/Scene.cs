using System.Collections.Generic;
using FortRise;

namespace Monocle;

public class patch_Scene : Scene 
{
    public List<string> SceneTags => sceneTags;
    private List<string> sceneTags;

    public void AssignTag(string tag) 
    {
        sceneTags ??= new List<string>();
        sceneTags.Add(tag); 
    }

    public bool HasTags(params string[] tags) 
    {
        sceneTags ??= new List<string>();
        foreach (var tag in tags) 
        {
            if (sceneTags.Contains(tag))
                return true;
        }
        return false;
    }

    public bool HasTag(string tags) 
    {
        sceneTags ??= new List<string>();
        return sceneTags.Contains(tags);
    }

    public void LogTags() 
    {
        foreach (var tag in sceneTags) 
        {
            Logger.Info($"[TAGS] {tag}");
        }
    }
}

public static class SceneExt 
{
    public static List<string> GetSceneTags(this Scene scene) 
    {
        return ((patch_Scene)scene).SceneTags ?? new List<string>();
    }

    public static void AssignTag(this Scene scene, string tag) 
    {
        ((patch_Scene)scene).AssignTag(tag);
    }

    public static bool HasTags(this Scene scene, params string[] tags) 
    {
        return ((patch_Scene)scene).HasTags(tags);
    }

    public static bool HasTag(this Scene scene, string tag) 
    {
        return ((patch_Scene)scene).HasTag(tag);
    }

    public static void LogTags(this Scene scene) 
    {
        foreach (var tag in scene.GetSceneTags()) 
        {
            Logger.Log($"[TAGS] {tag}");
        }
    }
}