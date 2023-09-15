using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using MonoMod;
using MonoMod.Utils;

namespace ModPorter;

public class FNAPort : PortModule
{
    protected override void Port(ModuleDefinition mod)
    {
        if (TryPort(mod)) 
        {
            Console.WriteLine("[ModPorter] FNA Porting Successful!");
            return;
        }
    }

    private bool TryPort(ModuleDefinition mod) 
    {
        // Check if the module references is XNA
        if (!mod.AssemblyReferences.Any(asmRef => asmRef.Name.StartsWith("Microsoft.Xna.Framework")))
            return false;

        // Replace XNA assembly references with FNA ones
        using var FNA = ModuleDefinition.ReadModule("FNA.dll");
        bool didReplaceXNA = ReplaceAssemblyRefs(FNA.Assembly.Name);

        // Ensure that FNA.dll can be loaded
        if (Modder.FindType("Microsoft.Xna.Framework.Game")?.SafeResolve() == null)
            throw new Exception("Failed to resolve Microsoft.Xna.Framework.Game");

        return didReplaceXNA;
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


    public bool ReplaceAssemblyRefs(AssemblyNameDefinition newRef) 
    {
        // Check if the module has a reference affected by the filter
        bool proceed0 = false;
        foreach (var asm in Modder.Module.AssemblyReferences) 
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
        foreach (var asm in Modder.Module.AssemblyReferences) 
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
            Modder.MapDependency(Modder.Module, asmRef);
            Modder.Log("[ModPorter] Adding assembly reference to " +  asmRef.FullName);
        }

        // Replace old references
        ModuleDefinition newModule = null;
        foreach (var module in Modder.DependencyMap[Modder.Module]) 
        {
            if (module.Assembly.Name.Name == newRef.Name) 
            {
                newModule = module;
                break;
            }
        }

        for (int i = 0; i < Modder.Module.AssemblyReferences.Count; i++) {
            AssemblyNameReference asmRef = Modder.Module.AssemblyReferences[i];
            if (!asmRef.Name.StartsWith("Microsoft.Xna.Framework"))
                continue;

            // Remove dependency
            Modder.Module.AssemblyReferences.RemoveAt(i--);
            var listToRemove = new List<ModuleDefinition>();
            foreach (var mod in Modder.DependencyMap[Modder.Module]) 
            {
                if (mod.Assembly.FullName == asmRef.FullName) 
                {
                    listToRemove.Add(mod);
                }
            }
            foreach (var item in listToRemove) 
            {
                Modder.DependencyMap[Modder.Module].Remove(item);
            }
            Modder.RelinkModuleMap[asmRef.Name] = newModule;
            Modder.Log("[FortRise] Replacing assembly reference " + asmRef.FullName + " -> " + newRef.FullName);
        }

        return !hasNewRef;
    }
}