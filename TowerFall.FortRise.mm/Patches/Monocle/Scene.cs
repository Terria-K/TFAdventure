using System.Collections.Generic;
using System.Linq;
using FortRise;

namespace Monocle;

public class patch_Scene : Scene 
{
    public List<string> SceneTags;

    public void AssignTag(string tag) 
    {
        SceneTags ??= new List<string>();
        SceneTags.Add(tag); 
    }

    public bool HasTags(params string[] tags) 
    {
        SceneTags ??= new List<string>();
        foreach (var tag in tags) 
        {
            if (SceneTags.Contains(tag))
                return true;
        }
        return false;
    }

    public bool HasTag(string tags) 
    {
        SceneTags ??= new List<string>();
        return SceneTags.Contains(tags);
    }

    public void LogTags() 
    {
        foreach (var tag in SceneTags) 
        {
            Logger.Log($"[TAGS] {tag}");
        }
    }
}

public static class SceneExt 
{
    public static List<string> GetSceneTags(this Scene scene) 
    {
        return ((patch_Scene)scene).SceneTags;
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