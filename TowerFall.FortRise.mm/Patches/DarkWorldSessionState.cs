using FortRise.Adventure;
using MonoMod;

namespace TowerFall;

public class patch_DarkWorldSessionState : DarkWorldSessionState
{
    private Session session;
    [MonoModPublic]
    private PlayerInventory defaultInventory;
    public patch_DarkWorldSessionState(Session session) : base(session)
    {
    }

    // public extern void orig_ctor(Session session);

    // [MonoModConstructor]
    // public void ctor(Session session) 
    // {
        // orig_ctor(session);
        // var originalCount = defaultInventory.Arrows.Arrows.Count;
        // var matchArrow = session.MatchSettings.Variants switch 
        // {
        //     { StartWithBoltArrows: { Value: true } } => ArrowTypes.Bolt,
        //     { StartWithBombArrows: { Value: true } } => ArrowTypes.Bomb,
        //     { StartWithSuperBombArrows: { Value: true } } => ArrowTypes.SuperBomb,
        //     { StartWithBrambleArrows: { Value: true } } => ArrowTypes.Bramble,
        //     { StartWithDrillArrows: { Value: true } } => ArrowTypes.Drill,
        //     { StartWithFeatherArrows: { Value: true } } => ArrowTypes.Feather,
        //     { StartWithLaserArrows: { Value: true } } => ArrowTypes.Laser,
        //     { StartWithPrismArrows: { Value: true } } => ArrowTypes.Prism,
        //     { StartWithToyArrows: { Value: true } } => ArrowTypes.Toy,
        //     { StartWithTriggerArrows: { Value: true } } => ArrowTypes.Trigger,
        //     { StartWithRandomArrows: { Value: true } } => (ArrowTypes)Calc.Random.Next(0, 10),
        //     _ => ArrowTypes.Normal
        // };
        // defaultInventory.Arrows = new ArrowList(originalCount, matchArrow);
    // }

    public extern int orig_get_ContinuesRemaining();

    public int get_ContinuesRemaining() 
    {
        if (session.GetLevelSet() != "TowerFall")
        {
            int id = session.MatchSettings.LevelSystem.ID.X;
            var tower = TowerRegistry.DarkWorldGet(session.GetLevelSet(), id);
            var amountContinue = tower.MaxContinues[(int)session.MatchSettings.DarkWorldDifficulty];
            if (amountContinue >= 0)
            {
                return amountContinue - Continues;
            }
        }
        return orig_get_ContinuesRemaining();
    }
}