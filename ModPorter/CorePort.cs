using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace ModPorter;

// This whole code is based on this code
// https://github.com/EverestAPI/Everest/blob/core/NETCoreifier/Coreifier.cs
// https://github.com/EverestAPI/Everest/blob/core/NETCoreifier/NetFrameworkModder.cs

public class CorePort : PortModule
{
    private bool canProceedToPort = false;
    public override bool PrivateSystemLibsRelink => true;

    public override void PrePatch(ModuleDefinition mod)
    {
        mod.RuntimeVersion = System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion;

        mod.Attributes &= ~(ModuleAttributes.Required32Bit | ModuleAttributes.Preferred32Bit);

        var attr = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>();
        var moduleAttr = mod.Assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
        if (moduleAttr != null) 
        {
            var val = (string)moduleAttr.ConstructorArguments[0].Value;
            canProceedToPort = val.StartsWith(".NETFramework");

            if (attr != null) 
            {
                moduleAttr.ConstructorArguments[0] = new CustomAttributeArgument(
                    mod.ImportReference(typeof(string)), attr.FrameworkName);
                moduleAttr.Properties.Clear();
                moduleAttr.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(
                    nameof(attr.FrameworkDisplayName), 
                    new CustomAttributeArgument(mod.ImportReference(typeof(string)), attr.FrameworkDisplayName)));
            }
            return;
        }
        canProceedToPort = mod.AssemblyReferences.Any(asmRef => asmRef.Name == "mscorlib") && 
            !mod.AssemblyReferences.Any(asmRef => asmRef.Name == "System.Runtime");
    }

    public override bool CanPort(ModuleDefinition mod)
    {
        return canProceedToPort;
    }

    protected override void Port(ModuleDefinition mod)
    {
        for (int i = 0; i < mod.AssemblyReferences.Count; i++) 
            if (Modder.PrivateSystemLibs.Contains(mod.AssemblyReferences[i].Name))
                mod.AssemblyReferences.RemoveAt(i--);
    }

    public override void MapDependecies(PortMonoModder modder)
    {
        modder.AddReference("System.Runtime");
    }

    public override void AutoPatch(PortMonoModder modder)
    {
        modder.ParseRules(modder.DependencyMap[modder.Module].First(dep => dep.Assembly.Name.Name == "ModPorter"));
    }

    public override void PatchMethod(PortMonoModder modder, MethodDefinition method)
    {
        if (modder.NoInlining && 
            (method.ImplAttributes & Mono.Cecil.MethodImplAttributes.AggressiveInlining) == 0 && 
            method.Body is Mono.Cecil.Cil.MethodBody body) 
            method.ImplAttributes |= Mono.Cecil.MethodImplAttributes.NoInlining;
        
        if (method.DeclaringType.HasGenericParameters && method.Body != null) 
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++) 
            {
                var instr = method.Body.Instructions[i];

                if (instr.OpCode == OpCodes.Ldtoken)
                    continue;
                
                if (instr.Operand is TypeReference typeRef 
                    && typeRef.Resolve() == method.DeclaringType && !typeRef.IsGenericInstance) 
                {
                    var typeInst = new GenericInstanceType(typeRef);
                    typeInst.GenericArguments.AddRange(method.DeclaringType.GenericParameters);
                    instr.Operand = typeInst;
                    continue;
                }
                if (instr.Operand is MemberReference memberRef && instr.Operand is not TypeReference &&
                    memberRef.DeclaringType.SafeResolve() == method.DeclaringType && !memberRef.DeclaringType.IsGenericInstance) 
                {
                    var typeInst = new GenericInstanceType(memberRef.DeclaringType);
                    typeInst.GenericArguments.AddRange(method.DeclaringType.GenericParameters);
                    memberRef.DeclaringType = typeInst;
                }
            }
        }
    }

    // Use the mono criteria for this, as those are known (see mono_method_check_inlining)
    private bool CanInlineLegacyCode(Mono.Cecil.Cil.MethodBody body) 
    {
        const int INLINE_LENGTH_LIMIT = 20; // mono/mini/method-to-ir.c

        if (body.CodeSize >= INLINE_LENGTH_LIMIT)
            return false;

        return true;
    }

    public override void PostPatch(ModuleDefinition mod)
    {
        base.PostPatch(mod);
    }
}