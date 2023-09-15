using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;

namespace MonoMod;

[MonoModCustomMethodAttribute(nameof(MonoModRules.PatchPostFix))]
internal class PostFixingAttribute : Attribute 
{
    public string TypeName;
    public string MethodName;
    public PostFixingAttribute(string typeName, string methodName, bool isStatic = false) 
    {
        TypeName = typeName;
        MethodName = methodName;
    }
}
[MonoModCustomMethodAttribute(nameof(MonoModRules.PatchPreFix))]
internal class PreFixingAttribute : Attribute 
{
    public string TypeName;
    public string MethodName;
    public PreFixingAttribute(string typeName, string methodName, bool isStatic = false) 
    {
        TypeName = typeName;
        MethodName = methodName;
    }
}

internal class PostPatchDisableTempVariant : Attribute {}
internal class PostPatchEnableTempVariant : Attribute {}
internal class PostPatchXmlToVariant : Attribute {}

[MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDarkWorldLevelSelectOverlayCtor))]
internal class PatchDarkWorldLevelSelectOverlayCtor : Attribute {}


[MonoModCustomMethodAttribute(nameof(MonoModRules.PatchFlags))]
internal class PatchFlags : Attribute {}

internal static partial class MonoModRules 
{
    private static bool IsTowerFall;
    private static bool IsWindows;
    private static Version Version;
    private static bool IsMod;
    private static bool IsFNA = true;
    public static readonly ModuleDefinition RulesModule;
    public static string ExecModName;

    public static bool CheckIfMods(ModuleDefinition mod) 
    {
        return ExecModName == mod.Name.Substring(0, mod.Name.Length - 4) + ".MonoModRules [MMILRT, ID:" + MonoModRulesManager.GetId(MonoModRule.Modder) + "]";
    }

    static MonoModRules() 
    {
        MonoModRule.Modder.MissingDependencyThrow = false;

        if (MonoModRule.Modder.WriterParameters.WriteSymbols)
            MonoModRule.Modder.WriterParameters.SymbolWriterProvider = new PortablePdbWriterProvider();

        IsWindows = PlatformHelper.Is(Platform.Windows);
        MonoModRule.Flag.Set("OS:Windows", IsWindows);
        MonoModRule.Flag.Set("OS:NotWindows", !IsWindows);
        ExecModName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        RulesModule = MonoModRule.Modder.DependencyMap.Keys.First(CheckIfMods);

        MonoModRule.Modder.PostProcessors += PostProcessor;
        IsMod = !MonoModRule.Modder.Mods.Contains(RulesModule);

        try 
        {
            using var fs = File.OpenRead("PatchVersion.txt");
            using StreamReader sr = new StreamReader(fs);
            var lines = sr.ReadToEnd().Split('\n');
            ParseArgs(lines);
        }
        catch 
        {
            // No-Op
        }


        if (IsMod) 
            return;
        

        MonoModRule.Modder.PostProcessors += PostProcessMacros;

        bool hasSteamworks = false;
        foreach (var name in MonoModRule.Modder.Module.AssemblyReferences) 
        {
            if (name.Name.Contains("Steamworks"))
                hasSteamworks = true;
        }
        MonoModRule.Flag.Set("Steamworks", hasSteamworks);
        MonoModRule.Flag.Set("NoLauncher", !hasSteamworks);
        if (hasSteamworks) 
            Console.WriteLine("[FortRise] Running on a Steam Launcher");
        else 
            Console.WriteLine("[FortRise] Running TowerFall without a Launcher");
        

        TypeDefinition t_TFGame = MonoModRule.Modder.FindType("TowerFall.TFGame")?.Resolve();
        if (t_TFGame == null)
            return;
        IsTowerFall = t_TFGame.Scope == MonoModRule.Modder.Module;

        // Get the version of TowerFall

        int[] numVersions = null;
        var ctor_TFGame = t_TFGame.FindMethod(".cctor", true);
        if (ctor_TFGame != null && ctor_TFGame.HasBody) 
        {
            var instrs = ctor_TFGame.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++) 
            {
                var instr = instrs[i];
                var ctor_Version = instr.Operand as MethodReference;
                if (instr.OpCode != OpCodes.Newobj || ctor_Version.DeclaringType?.FullName != "System.Version")
                    continue;
                
                numVersions = new int[ctor_Version.Parameters.Count];
                for (int j = -numVersions.Length; j < 0; j++) 
                    numVersions[j + numVersions.Length] = instrs[j + i].GetInt();
                
                break;
            }
        }

        if (numVersions == null) {
            throw new InvalidOperationException("Unknown version of TowerFall is being patched. Operation cancelled");
        }

        var version = numVersions.Length switch {
            2 => new Version(numVersions[0], numVersions[1]),
            3 => new Version(numVersions[0], numVersions[1], numVersions[2]),
            4 => new Version(numVersions[0], numVersions[1], numVersions[2], numVersions[3]),
            _ => throw new InvalidOperationException("Unknown version of TowerFall is being patched. Operation cancelled")
        };
        var minimumVersion = new Version(1, 3, 3, 1);
        if (version.Major == 0)
            version = minimumVersion;
        if (version < minimumVersion)
            throw new Exception($"Unsupported version of TowerFall: {version}, currently supported: {minimumVersion}");
        Version = version;
        Console.WriteLine("[FortRise] TowerFall Version is: " + Version);
        
        if (IsTowerFall) 
        {
            // Ensure that TowerFall assembly is not already modded
            // (https://github.com/MonoMod/MonoMod#how-can-i-check-if-my-assembly-has-been-modded)
            if (MonoModRule.Modder.FindType("MonoMod.WasHere") != null)
                throw new Exception("This version of TowerFall is already modded. You need a clean install of TowerFall to mod it.");
        }

        Console.WriteLine($"[FortRise] Platform Found: {PlatformHelper.Current}");

        // if (IsFNA && RelinkAgainstFNA(MonoModRule.Modder))
        //     Console.WriteLine("[FortRise] Relinking to FNA");

        static void VisitType(TypeDefinition type) {
            // Remove readonly attribute from all static fields
            // This "fixes" https://github.com/dotnet/runtime/issues/11571, which breaks some mods
            foreach (FieldDefinition field in type.Fields)
                if ((field.Attributes & FieldAttributes.Static) != 0)
                    field.Attributes &= ~FieldAttributes.InitOnly;

            // Visit nested types
            foreach (TypeDefinition nestedType in type.NestedTypes)
                VisitType(nestedType);
        }

        foreach (TypeDefinition type in MonoModRule.Modder.Module.Types)
            VisitType(type);
    }

    public static void ParseArgs(string[] lines) 
    {
        foreach (var line in lines) 
        {
            var split = line.Split(':');
            switch (split[0]) 
            {
            case "FNA":
                var isTrue = bool.Parse(split[1]);
                IsFNA = isTrue;
                break;
            }
        }
    }

    public static System.Reflection.AssemblyName GetRulesAssemblyRef(string name) 
    { 
        System.Reflection.AssemblyName asmName = null;
        foreach (var asm in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies()) 
        {
            if (asm.Name.Equals(name)) 
            {
                asmName = asm;
                break;
            }
        }
        return asmName;
    }

    public static bool ReplaceAssemblyRefs(MonoModder modder, System.Reflection.AssemblyName newRef) 
    {
        // Check if the module has a reference affected by the filter
        bool proceed0 = false;
        foreach (var asm in modder.Module.AssemblyReferences) 
        {
            if (asm.Name.StartsWith("Microsoft.Xna.Framework")) 
            {
                proceed0 = true;
                break;
            }
        }
        if (!proceed0)
            return false;

        // Add new dependency and map it, if it not already exist
        bool hasNewRef = false;
        foreach (var asm in modder.Module.AssemblyReferences) 
        {
            if (asm.Name == newRef.Name) 
            {
                hasNewRef = true;
                break;
            }
        }
        if (!hasNewRef) 
        {
            AssemblyNameReference asmRef = new AssemblyNameReference(newRef.Name, newRef.Version);
            // modder.Module.AssemblyReferences.Add(asmRef);
            modder.MapDependency(modder.Module, asmRef);
            modder.Log("[FortRise] Adding assembly reference to " +  asmRef.FullName);
        }

        // Replace old references
        ModuleDefinition newModule = null;
        foreach (var module in modder.DependencyMap[modder.Module]) 
        {
            if (module.Assembly.Name.Name == newRef.Name) 
            {
                newModule = module;
                break;
            }
        }

        for (int i = 0; i < modder.Module.AssemblyReferences.Count; i++) {
            AssemblyNameReference asmRef = modder.Module.AssemblyReferences[i];
            if (!asmRef.Name.StartsWith("Microsoft.Xna.Framework"))
                continue;

            // Remove dependency
            modder.Module.AssemblyReferences.RemoveAt(i--);
            var listToRemove = new List<ModuleDefinition>();
            foreach (var mod in modder.DependencyMap[modder.Module]) 
            {
                if (mod.Assembly.FullName == asmRef.FullName) 
                {
                    listToRemove.Add(mod);
                }
            }
            foreach (var item in listToRemove) 
            {
                modder.DependencyMap[modder.Module].Remove(item);
            }
            modder.RelinkModuleMap[asmRef.Name] = newModule;
            modder.Log("[FortRise] Replacing assembly reference " + asmRef.FullName + " -> " + newRef.FullName);
        }

        return !hasNewRef;
    }


    private static bool RelinkAgainstFNA(MonoModder modder) 
    {
        // Check if the module references either XNA or FNA
        bool proceed = false;
        foreach (var asm in modder.Module.AssemblyReferences) 
        {
            if (asm.Name == "FNA" || asm.Name.StartsWith("Microsoft.Xna.Framework")) 
            {
                proceed = true;
                break;
            }
        }
        if (!proceed)
            return false;
        // if (!modder.Module.AssemblyReferences.Any(asmRef => asmRef.Name == "FNA" || asmRef.Name.StartsWith("Microsoft.Xna.Framework")))
        //     return false;

        // Replace XNA assembly references with FNA ones
        bool didReplaceXNA = ReplaceAssemblyRefs(MonoModRule.Modder, GetRulesAssemblyRef("FNA"));

        // Ensure that FNA.dll can be loaded
        if (MonoModRule.Modder.FindType("Microsoft.Xna.Framework.Game")?.SafeResolve() == null)
            throw new Exception("Failed to resolve Microsoft.Xna.Framework.Game");

        return didReplaceXNA;
    }

    public static void PatchPostFix(ILContext ctx, CustomAttribute attrib) 
    {
        string typeName = (string)attrib.ConstructorArguments[0].Value;
        string methodName = (string)attrib.ConstructorArguments[1].Value;
        bool isStatic = (bool)attrib.ConstructorArguments[2].Value;

        var method = ctx.Module.GetType(typeName).FindMethod(methodName);

        var cursor = new ILCursor(ctx);
        while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchRet())) {}
        if (!isStatic)
            cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Call, method);
    }

    public static void PatchPreFix(ILContext ctx, CustomAttribute attrib) 
    {
        string typeName = (string)attrib.ConstructorArguments[0].Value;
        string methodName = (string)attrib.ConstructorArguments[1].Value;
        bool isStatic = (bool)attrib.ConstructorArguments[2].Value;

        var method = ctx.Module.GetType(typeName).FindMethod(methodName);

        var cursor = new ILCursor(ctx);
        if (!isStatic)
            cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Call, method);
    }

    public static void PatchFlags(ILContext ctx, CustomAttribute attrib) 
    {
        var IsWindows = ctx.Module.GetType("FortRise.RiseCore").FindProperty("IsWindows").SetMethod;
        var cursor = new ILCursor(ctx);

        if (MonoModRules.IsWindows)
            cursor.Emit(OpCodes.Ldc_I4_1);
        else    
            cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Call, IsWindows);
    }

    public static void PatchDarkWorldLevelSelectOverlayCtor(ILContext ctx, CustomAttribute attrib) 
    {
        var TowerFall_MapScene = ctx.Module.Assembly.MainModule.GetType("TowerFall", "MapScene");
        var Selection = TowerFall_MapScene.FindField("Selection");

        var TowerFall_MapButton = ctx.Module.Assembly.MainModule.GetType("TowerFall", "MapButton");
        var get_Data = TowerFall_MapButton.FindMethod("TowerFall.TowerMapData get_Data()", false);

        var cursor = new ILCursor(ctx);
        var label = ctx.DefineLabel();
        cursor.GotoNext(MoveType.After, instr => instr.MatchStfld("TowerFall.DarkWorldLevelSelectOverlay", "drawStatsLerp"));

        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldfld, Selection);
        cursor.Emit(OpCodes.Callvirt, get_Data);

        cursor.GotoNext(MoveType.After, instr => instr.MatchStfld("TowerFall.DarkWorldLevelSelectOverlay", "statsID"));
        cursor.MarkLabel(label);
        cursor.GotoPrev(MoveType.After, instr => instr.MatchCallvirt(get_Data));
        cursor.Emit(OpCodes.Brfalse_S, label);
    }

    public static void PostProcessMacros(MonoModder modder) 
    {
        var matchVariant = modder.Module.Types.Where(x => x.FullName == "TowerFall.MatchVariants").First();
        foreach (TypeDefinition type in modder.Module.Types) 
        {
            if (type.FullName == "TowerFall.TemporaryVariants") 
            {
                foreach (var field in matchVariant.Fields) 
                {
                    if (!field.FieldType.Is("TowerFall.Variant"))
                        continue;
                    FieldDefinition def = new FieldDefinition(
                        "Temp" + field.Name, FieldAttributes.Public | FieldAttributes.Static, 
                        modder.Module.TypeSystem.Boolean 
                    );
                    type.Fields.Add(def);
                }
            }
            else if (type.FullName == "TowerFall.DarkWorldTowerData") 
            {
                var levelData = type.NestedTypes.Where(x => x.FullName == "TowerFall.DarkWorldTowerData/LevelData").First();
                var variantField = levelData.FindField("ActiveVariant");
                var variant = variantField.FieldType.Resolve();
                var MatchVariants = modder.Module.GetType("TowerFall.MatchVariants");
                foreach (var field in MatchVariants.Fields) 
                {
                    if (!field.FieldType.Is("TowerFall.Variant"))
                        continue;
                    var newField = new FieldDefinition(field.Name, FieldAttributes.Public, modder.Module.TypeSystem.Boolean);
                    variant.Fields.Add(newField);
                }
                foreach (var methd in levelData.Methods) 
                {
                    if (!methd.HasCustomAttribute("MonoMod.PostPatchXmlToVariant"))
                        continue;

                    var Calc = modder.Module.GetType("Monocle.Calc");
                    var HasChild = Calc.FindMethod("System.Boolean HasChild(System.Xml.XmlElement,System.String)");
                    var ChildBool = Calc.FindMethod("System.Boolean ChildBool(System.Xml.XmlElement,System.String)");

                    var il = methd.Body.GetILProcessor();
                    var firstLast = il.Body.Instructions[0];
                    il.RemoveAt(il.Body.Instructions.Count - 1);

                    for (int i = 0; i < variant.Fields.Count; i++) 
                    {
                        var field = variant.Fields[i];
                        var fieldName = variant.FindField(field.Name);
                        var entry = i == variant.Fields.Count - 1 ? 
                            il.Create(OpCodes.Ret) : il.Create(OpCodes.Ldarg_1);
                        
                        if (i == 0) 
                            il.Emit(OpCodes.Ldarg_1);
                        
                        il.Emit(OpCodes.Ldstr, field.Name);
                        il.Emit(OpCodes.Call, HasChild);
                        il.Emit(OpCodes.Brfalse_S, entry);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldflda, variantField);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, field.Name);
                        il.Emit(OpCodes.Call, ChildBool);
                        il.Emit(OpCodes.Stfld, fieldName);
                        il.Append(entry);
                    }
                }
            }
        }

        var controlType = modder.Module.GetType("TowerFall.DarkWorldControl");

        foreach (var methd in controlType.Methods) 
        {
            var tempVariant = modder.Module.GetType("TowerFall.TemporaryVariants");
            // PostPatch
            if (methd.HasCustomAttribute("MonoMod.PostPatchDisableTempVariant")) 
            {
                var TempDark = tempVariant.FindField("TempAlwaysDark");
                var Level = modder.Module.GetType("TowerFall.Level");
                var get_Session = Level.FindMethod("TowerFall.Session get_Session()");

                var Session = modder.Module.GetType("TowerFall.Session");
                var MatchSettings = Session.FindField("MatchSettings");
                var Variants = MatchSettings.FieldType.Resolve().FindField("Variants");

                
                var il = methd.Body.GetILProcessor();
                var firstLast = il.Body.Instructions[0];
                il.RemoveAt(il.Body.Instructions.Count - 1);
                for (int i = 0; i < tempVariant.Fields.Count; i++) 
                {
                    var field = tempVariant.Fields[i];
                    var variant = Variants.FieldType.Resolve().FindField(field.Name.Remove(0, 4));
                    var set_Value = variant.FieldType.Resolve().FindMethod("System.Void set_Value(System.Boolean)");
                    // if (TempVariant)
                    if (i == 0)
                        il.Emit(OpCodes.Ldsfld, field);
                    var entry = i == tempVariant.Fields.Count - 1 ? 
                        il.Create(OpCodes.Ret) : il.Create(OpCodes.Ldsfld, tempVariant.Fields[i + 1]);
                    il.Emit(OpCodes.Brfalse_S, entry);
                    // Body
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stsfld, field);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, get_Session);
                    il.Emit(OpCodes.Ldfld, MatchSettings);
                    il.Emit(OpCodes.Ldfld, Variants);
                    il.Emit(OpCodes.Ldfld, variant);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Callvirt, set_Value);
                    il.Append(entry);
                }

            }
            else if (methd.HasCustomAttribute("MonoMod.PostPatchEnableTempVariant"))  
            {
                var Level = modder.Module.GetType("TowerFall.Level");
                var get_Session = Level.FindMethod("TowerFall.Session get_Session()");

                var Session = modder.Module.GetType("TowerFall.Session");
                var TowerData = modder.Module.GetType("TowerFall.DarkWorldTowerData/LevelData");
                var variantField = TowerData.FindField("ActiveVariant");
                var MatchSettings = Session.FindField("MatchSettings");
                var Variants = MatchSettings.FieldType.Resolve().FindField("Variants");

                var il = methd.Body.GetILProcessor();
                var firstLast = il.Body.Instructions[0];
                il.RemoveAt(il.Body.Instructions.Count - 1);

                for (int i = 0; i < tempVariant.Fields.Count; i++)  
                {
                    var field = tempVariant.Fields[i];
                    var fieldVariant = field.Name.Remove(0, 4);
                    var towerVariant = variantField.FieldType.Resolve().FindField(fieldVariant);
                    var variant = Variants.FieldType.Resolve().FindField(fieldVariant);
                    var get_Value = variant.FieldType.Resolve().FindMethod("System.Boolean get_Value()");
                    var set_Value = variant.FieldType.Resolve().FindMethod("System.Void set_Value(System.Boolean)");

                    var entry = i == tempVariant.Fields.Count - 1 ? 
                        il.Create(OpCodes.Ret) : il.Create(OpCodes.Ldarg_0);
                    if (i == 0)
                        il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, get_Session);
                    il.Emit(OpCodes.Ldfld, MatchSettings);
                    il.Emit(OpCodes.Ldfld, Variants);
                    il.Emit(OpCodes.Ldfld, variant);
                    il.Emit(OpCodes.Callvirt, get_Value);
                    il.Emit(OpCodes.Brtrue_S, entry);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldflda, variantField);
                    il.Emit(OpCodes.Ldfld, towerVariant);
                    il.Emit(OpCodes.Brfalse_S, entry);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stsfld, field);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, get_Session);
                    il.Emit(OpCodes.Ldfld, MatchSettings);
                    il.Emit(OpCodes.Ldfld, Variants);
                    il.Emit(OpCodes.Ldfld, variant);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Callvirt, set_Value);
                    il.Append(entry);
                }
            }
        }
    }

    // https://github.com/EverestAPI/Everest/blob/f4545220fe22ed3f752e358741befe9cc7546234/Celeste.Mod.mm/MonoModRules.cs
    // This is to fix the Enumerators can't be decompiled
    public static void FixEnumeratorDecompile(MonoModder modder, TypeDefinition type) 
    {
        foreach (MethodDefinition method in type.Methods) 
        {
            new ILContext(method).Invoke(il => 
            {
                ILCursor cursor = new ILCursor(il);
                while (cursor.TryGotoNext(instr => instr.MatchCallvirt(out MethodReference m) &&
                    (m.Name is "System.Collections.IEnumerable.GetEnumerator" or "System.IDisposable.Dispose" ||
                        m.Name.StartsWith("<>m__Finally")))
                ) 
                {
                    cursor.Next.OpCode = OpCodes.Call;
                }
            });
        }
    }

    /* 
    The game is made on Net Framework 4.0, which means it doesn't have IteratorStateMachine.
    By doing this, we can easily hook up the enumerators via GetStateMachineTarget() from MonoMod when using hooks.
    */
    private static void AddIteratorStateMachineAttribute(MonoModder modder, MethodDefinition method) 
    {
        var moveNext = method.GetEnumeratorMoveNext();
        if (moveNext == null)
            return;

        var typeRef = modder.Module.ImportReference(
            typeof(System.Type));
        var impl = modder.Module.ImportReference(
            typeof(System.Runtime.CompilerServices.IteratorStateMachineAttribute)
            .GetConstructor(new Type[1] { typeof(Type) }));
        var customAttribute = new CustomAttribute(impl);
        var targetType = moveNext.DeclaringType;
        customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(typeRef, targetType));
        method.CustomAttributes.Add(customAttribute);
    }

    private static void PostProcessor(MonoModder modder) 
    {
        foreach (var type in modder.Module.Types) 
        {
            PostProcessType(modder, type);
        }
    }

    private static void PostProcessType(MonoModder modder, TypeDefinition type) 
    {
        if (type.IsCompilerGeneratedEnumerator() && !IsMod) 
        {
            FixEnumeratorDecompile(modder, type);
        }
        foreach (MethodDefinition method in type.Methods) 
        {
            method.FixShortLongOps();
            if (!method.HasCustomAttribute("System.Runtime.CompilerServices.IteratorStateMachineAttribute") && !IsMod) 
                AddIteratorStateMachineAttribute(modder, method);
            
        }
        foreach (TypeDefinition nested in type.NestedTypes) 
        {
            PostProcessType(modder, nested);
        }
    }
}
