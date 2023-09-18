using Mono.Cecil;
using MonoMod;

namespace ModPorter;

public abstract class PortModule // Coincidence?
{
    public PortMonoModder Modder;
    public virtual bool PrivateSystemLibsRelink => false;

    public void StartPort(PortMonoModder modder) 
    {
        Modder = modder;
        Port(modder.Module);
    }

    public virtual void AutoPatch(PortMonoModder modder) {}
    public virtual void MapDependecies(PortMonoModder modder) {}
    public virtual void PatchMethod(PortMonoModder modder, MethodDefinition method) {}

    public virtual bool CanPort(ModuleDefinition mod) => true;
    public abstract void PrePatch(ModuleDefinition mod);
    protected abstract void Port(ModuleDefinition mod);
    public virtual void PostPatch(ModuleDefinition mod) {}
}