using System;
using System.Collections.Generic;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A Phase 2 table's hooks: the demon's rule (at a boss's table) and the class's mechanics (at every table), in that order. The
    /// presentation and the simulation reach the class's acts through <see cref="Satchel"/>, <see cref="Prophecy"/>, <see cref="Treasury"/>.
    /// </summary>
    public sealed class ChapterHooks : HookChain
    {
        /// <summary>The dice stream of a table's hooks under its seed.</summary>
        public const int HookStream = 6;

        private ChapterHooks(ClassKit kit, DemonRule rule, ClassPowers powers, UpgradeHooks upgrades) : base(new TableHooks[] { rule, powers, upgrades })
        {
            Upgrades = upgrades;
            Kit = kit;
            Rule = rule;
            Powers = powers;
        }

        public ClassKit Kit { get; }

        /// <summary>The demon's rule; null at a floor's table.</summary>
        public DemonRule Rule { get; }

        /// <summary>The class's powers; null for the Jester (his jokers are his).</summary>
        public ClassPowers Powers { get; }

        /// <summary>The class's upgrades that act in a hand (the Jester's).</summary>
        public UpgradeHooks Upgrades { get; }

        public SatchelPowers Satchel => Powers as SatchelPowers;
        public ProphecyPowers Prophecy => Powers as ProphecyPowers;
        public TreasuryPowers Treasury => Powers as TreasuryPowers;

        /// <summary>The hooks of a table: <paramref name="bossId"/> null for a floor's (no rule), the boss's id at the demon's table.</summary>
        public static ChapterHooks For(ClassKit kit, string bossId, int barStart, int seed)
        {
            IRandomSource Dice(int n) => new SystemRandomSource(RandomSeeds.Derive(RandomSeeds.Derive(seed, HookStream), n));
            DemonRule rule = bossId == null ? null : DemonRule.For(bossId, barStart, kit, Dice(1));
            ClassPowers powers;
            switch (kit.Kind)
            {
                case KitKind.Satchel: powers = new SatchelPowers(kit, Dice(2)); break;
                case KitKind.Prophecy: powers = new ProphecyPowers(kit, Dice(2)); break;
                case KitKind.Treasury: powers = new TreasuryPowers(kit, Dice(2), bossId != null); break;
                default: powers = null; break;
            }
            return new ChapterHooks(kit, rule, powers, new UpgradeHooks(kit, Dice(3)));
        }

        /// <summary>The hooks on a game: it plays by them from now on.</summary>
        public static ChapterHooks Attach(HellPokerGame game, ClassKit kit, string bossId, int barStart, int seed)
        {
            ChapterHooks hooks = For(kit, bossId, barStart, seed);
            game.UseHooks(hooks);
            // The Eye of Truth: at a demon's table one of his cards plays face up from the deal.
            if (bossId != null && kit.Has(UpgradeIds.EyeOfTruth)) game.HouseCardsOpenAtDeal = Math.Max(game.HouseCardsOpenAtDeal, 1);
            return hooks;
        }
    }
}
