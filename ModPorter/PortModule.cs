using Mono.Cecil;
using MonoMod;

namespace ModPorter;

public abstract class PortModule // Coincidence?
{
    public MonoModder Modder;

    public void StartPort(MonoModder modder) 
    {
        Modder = modder;
        Port(modder.Module);
    }

    protected abstract void Port(ModuleDefinition mod);
}