using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using TeuJson;
using TowerFall;

namespace FortRise;

public class FortAttribute : Attribute 
{
    public string GUID;
    public string Name;

    public FortAttribute(string guid, string name) 
    {
        GUID = guid;
        Name = name;
    }
}

public abstract partial class FortModule 
{
    public bool Enabled { get; internal set; }
    public string Name { get; internal set; }
    public string ID { get; internal set; }
    public ModuleMetadata Meta { get; internal set; }
    public bool SupportModDisabling { get; set; } = true;
    public bool RequiredRestart { get; set; }
    public bool DisposeTextureAfterUnload { get; set; } = true;

    public virtual Type SettingsType { get; }
    public ModuleSettings InternalSettings;
    public virtual Type SaveDataType { get; }
    public ModuleSaveData InternalSaveData;
    public FortContent Content;


    public abstract void Load();
    public abstract void Unload();


    internal void InternalLoad() 
    {
        LoadSettings();
        Load();
    }

    internal void InternalUnload() 
    {
        Content?.Unload(DisposeTextureAfterUnload);
        Unload();
    }

    internal void SaveData() 
    {
        if (InternalSaveData == null)
            return;

        var format = InternalSaveData.Save(this).Format;
        format.SetPath(this);
        format.Save();
    }

    internal void VerifyData() 
    {
        if (InternalSaveData == null)
            return;

        InternalSaveData.Verify();
    }

    internal void LoadData() 
    {
        InternalSaveData = (ModuleSaveData)SaveDataType?.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());
        if (InternalSaveData == null)
            return;

        var format = InternalSaveData.Formatter;
        format.SetPath(this);
        if (format.Load())
            InternalSaveData.Load(format);
    }

    public void LoadSettings() 
    {
        InternalSettings = (ModuleSettings)SettingsType?.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());

        if (InternalSettings == null)
            return;

        var path = Path.Combine("Saves", ID, Name + ".settings" + ".json");
        InternalSettings.Load(path);
    }

    public void SaveSettings() 
    {
        if (InternalSettings == null)
            return;
        var path = Path.Combine("Saves", ID, Name + ".settings" + ".json");
        InternalSettings.Save(path);
    }

    [Obsolete("Use CreateModSettings(FortRise.TextContainer) instead")]
    public virtual void CreateModSettings(List<OptionsButton> optionList) {}

    public virtual void CreateModSettings(TextContainer textContainer) {}

    internal void CreateSettings(TextContainer textContainer) 
    {
        CreateModSettings(textContainer);

        var type = SettingsType;
        var settings = InternalSettings;

        if (settings == null || type == null) 
            return;

        // where the automated settings are created
        foreach (var field in type.GetFields()) 
        {
            if (field.IsPrivate)
                continue;
            var name = field.Name;
            var fieldType = field.FieldType;
            SettingsNumberAttribute attrib = null;
            SettingsOptionsAttribute optAttrib = null;

            var ownName = field.GetCustomAttribute<SettingsNameAttribute>();
            if (ownName != null)
                name = ownName.Name;

            var fullName = $"{name}".ToUpperInvariant();

            if (fieldType == typeof(bool)) 
            {
                var defaultVal = (bool)field.GetValue(settings);
                var toggleable = new TextContainer.Toggleable(fullName, defaultVal);
                toggleable.Change(x => {
                    field.SetValue(settings, x);
                });
                textContainer.Add(toggleable);
            }
            else if (fieldType == typeof(Action)) 
            {
                var actionButton = new TextContainer.ButtonText(fullName);
                actionButton.OnConfirm = () => {
                    var action = (Action)field.GetValue(settings);
                    action?.Invoke();
                };
                textContainer.Add(actionButton);
            }
            else if ((fieldType == typeof(int)) && (optAttrib = field.GetCustomAttribute<SettingsOptionsAttribute>()) != null) 
            {
                var selectionOption = new TextContainer.SelectionOption(fullName, optAttrib.Options);
                selectionOption.Change(x => {
                    field.SetValue(settings, x.Item2);
                });
                textContainer.Add(selectionOption);
            }
            else if ((fieldType == typeof(string)) && (optAttrib = field.GetCustomAttribute<SettingsOptionsAttribute>()) != null) 
            {
                var selectionOption = new TextContainer.SelectionOption(fullName, optAttrib.Options);
                selectionOption.Change(x => {
                    field.SetValue(settings, x.Item1);
                });
                textContainer.Add(selectionOption);
            }
            else if ((fieldType == typeof(int) || fieldType == typeof(float)) && 
                (attrib = field.GetCustomAttribute<SettingsNumberAttribute>()) != null) 
            {
                var numberButton = new TextContainer.Number(fullName, attrib.Min, attrib.Max);
                numberButton.Change(x => {
                    if (field.FieldType == typeof(float))
                        field.SetValue(settings, (float)x);
                    else
                        field.SetValue(settings, x);
                });

                textContainer.Add(numberButton);
            }
        }
    }


    public virtual void LoadContent() {}
    public virtual void Initialize() {}
    public virtual void OnVariantsRegister(MatchVariants variants, bool noPerPlayer = false) {}

    public bool IsModExists(string modName) 
    {
        return RiseCore.IsModExists(modName);
    }
}

public class ModuleMetadata : IEquatable<ModuleMetadata>, IDeserialize
{
    public string Name;
    public Version Version;
    public Version FortRiseVersion;
    public string Description;
    public string Author;
    public string DLL;
    public string PathDirectory = string.Empty;
    public string PathZip = string.Empty;
    public ModuleMetadata[] Dependencies;
    public string NativePath;
    public string NativePathX86;
    public AssemblyLoadContext AssemblyContext = AssemblyLoadContext.Default;

    public ModuleMetadata() {}


    public override string ToString()
    {
        return $"Metadata: {Name} by {Author} {Version}";
    }


    public bool Equals(ModuleMetadata other)
    {
        if (other.Name != this.Name)
            return false;
        
        if (other.Version.Major != this.Version.Major)
            return false;
        
        if (this.Version.Minor < other.Version.Minor)
            return false;

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as ModuleMetadata);
    

    public override int GetHashCode()
    {
        var version = Version.Major.GetHashCode() + Version.Minor.GetHashCode();
        var name = Name.GetHashCode();
        return version + name;
    }

    public void Deserialize(JsonObject value)
    {
        Name = value["name"];
        Version = new Version(value.GetJsonValueOrNull("version") ?? "1.0.0");
        var fVersion = value.GetJsonValueOrNull("required");
        if (fVersion == null)
            FortRiseVersion = RiseCore.FortRiseVersion;
        else
            FortRiseVersion = new Version(fVersion);
        Description = value.GetJsonValueOrNull("description") ?? string.Empty;
        Author = value.GetJsonValueOrNull("author") ?? string.Empty;
        DLL = value.GetJsonValueOrNull("dll") ?? string.Empty;
        NativePath = value.GetJsonValueOrNull("nativePath") ?? string.Empty;
        NativePathX86 = value.GetJsonValueOrNull("nativePathX86") ?? string.Empty;
        var dep = value.GetJsonValueOrNull("dependencies");
        if (dep is null)
            return;
        Dependencies = dep.ConvertToArray<ModuleMetadata>();
    }

    public static bool operator ==(ModuleMetadata lhs, ModuleMetadata rhs)
    {
        if (rhs is null)
        {
            if (lhs is null)
            {
                return true;
            }

            // Only the left side is null.
            return false;
        }
        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(ModuleMetadata lhs, ModuleMetadata rhs) => !(lhs == rhs);
}