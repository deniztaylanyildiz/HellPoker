using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What a place in the black market's scene is: a card on the stall, a card to pick (one of the carried relics), a service's
    /// tile, or the way out.</summary>
    public enum MarketSlotKind
    {
        Stall,
        Pick,
        Service,
        Leave
    }

    /// <summary>A service's tile (its icon is this place in Ui/market_services.png).</summary>
    public enum MarketService
    {
        Haggle,
        Reroll,
        Eye,
        JokerIn,
        JokerOut,
        Drop,
        Sell,
        Pawn,
        Transmute,
        Cleanse,
        BoxOpen,
        BoxDouble
    }

    /// <summary>
    /// Demo 3.5: one place in the black market's scene. A stall card (a relic, a class upgrade, the gambler's closed box face down) with
    /// its price at its foot and, under it, what else it costs (the bone merchant's years); a carried relic to pick as a small card with its
    /// price beside it; a service as a tile (its icon, its name, its price; its words in the hover box); the way out.
    /// </summary>
    public sealed class MarketSlot
    {
        public MarketSlotKind Kind { get; }
        public RelicCardData Card { get; }

        /// <summary>The stall card lies face down (the gambler's box).</summary>
        public bool FaceDown { get; }

        public MarketService Service { get; }
        public string Label { get; }
        public string Price { get; }
        public string Detail { get; }

        /// <summary>The words under a stall card ("+40 years"); empty for none.</summary>
        public string Under { get; }

        public bool Enabled { get; }

        /// <summary>Why a place not to be had is not (said when it is pressed: "Not enough coins"); empty for none.</summary>
        public string Refusal { get; }

        private MarketSlot(MarketSlotKind kind, RelicCardData card, bool faceDown, MarketService service, string label, string price, string detail,
            string under, bool enabled, string refusal)
        {
            Kind = kind;
            Card = card;
            FaceDown = faceDown;
            Service = service;
            Label = label ?? "";
            Price = price ?? "";
            Detail = detail ?? "";
            Under = under ?? "";
            Enabled = enabled;
            Refusal = refusal ?? "";
        }

        public static MarketSlot Stall(RelicCardData card, string label, string detail, bool enabled, string refusal = null, string under = null,
            bool faceDown = false) =>
            new MarketSlot(MarketSlotKind.Stall, card, faceDown, default, label, card?.Footer, detail, under, enabled, refusal);

        public static MarketSlot Pick(RelicCardData card, string price, string detail, bool enabled = true, string refusal = null) =>
            new MarketSlot(MarketSlotKind.Pick, card, false, default, card?.Name, price, detail, null, enabled, refusal);

        public static MarketSlot Tile(MarketService service, string label, string price, string detail, bool enabled = true, string refusal = null) =>
            new MarketSlot(MarketSlotKind.Service, null, false, service, label, price, detail, null, enabled, refusal);

        public static MarketSlot Leave(string label, string detail) =>
            new MarketSlot(MarketSlotKind.Leave, null, false, default, label, null, detail, null, true, null);

        /// <summary>The place as a panel's answer (its label and price together): what <c>ChapterPresenter.Panel</c> shows the tests.</summary>
        public PanelOption AsOption() =>
            new PanelOption(Price.Length > 0 && Kind != MarketSlotKind.Stall ? Label + " · " + Price : Label, Detail, Enabled, Kind == MarketSlotKind.Leave);
    }

    /// <summary>
    /// Demo 3.5: the black market as a scene of its own. The vendor (his hall, his portrait large, his words in a bubble), the purse and
    /// the relics carried at the top, the stall's cards, the services' tiles under them, LEAVE at the foot. While <see cref="Picking"/> the
    /// stall gives way to the carried relics' small cards (which one to sell, to throw away, to put in the crucible) and BACK.
    /// </summary>
    public sealed class MarketScene
    {
        /// <summary>The vendor's id ("smuggler", ...): his hall is Backgrounds/market_&lt;id&gt;, his portrait Market/&lt;id&gt;.</summary>
        public string VendorId { get; }

        public string Owner { get; }
        public string Title { get; }
        public string Line { get; }
        public int Coins { get; }
        public string RelicsLabel { get; }
        public IReadOnlyList<MarketSlot> Slots { get; }
        public int Selected { get; }

        /// <summary>The carried relics are being picked from (the stall and the tiles give way to them).</summary>
        public bool Picking { get; }

        public MarketScene(string vendorId, string owner, string title, string line, int coins, string relicsLabel, IReadOnlyList<MarketSlot> slots,
            int selected, bool picking = false)
        {
            VendorId = vendorId ?? "";
            Owner = owner ?? "";
            Title = title ?? "";
            Line = line ?? "";
            Coins = coins;
            RelicsLabel = relicsLabel ?? "";
            Slots = slots ?? Array.Empty<MarketSlot>();
            Selected = selected;
            Picking = picking;
        }

        public MarketScene WithSelected(int selected) =>
            new MarketScene(VendorId, Owner, Title, Line, Coins, RelicsLabel, Slots, selected, Picking);

        public MarketScene WithLine(string line) =>
            new MarketScene(VendorId, Owner, Title, line, Coins, RelicsLabel, Slots, Selected, Picking);

        /// <summary>The services' tiles in a row; the small cards to pick in a row.</summary>
        public const int TilesPerRow = 3, PicksPerRow = 2;

        /// <summary>
        /// Each place's row and column as the scene lays them out: the stall's cards one row, the small cards to pick two a row, the tiles
        /// three a row, LEAVE a row of its own.
        /// </summary>
        public (int row, int col)[] Grid()
        {
            var grid = new (int, int)[Slots.Count];
            int row = -1, col = 0;
            MarketSlotKind? last = null;
            for (int i = 0; i < Slots.Count; i++)
            {
                MarketSlotKind kind = Slots[i].Kind;
                int perRow = kind == MarketSlotKind.Stall ? int.MaxValue : kind == MarketSlotKind.Pick ? PicksPerRow
                    : kind == MarketSlotKind.Service ? TilesPerRow : 1;
                if (kind != last || col >= perRow)
                {
                    row++;
                    col = 0;
                }
                grid[i] = (row, col++);
                last = kind;
            }
            return grid;
        }

        /// <summary>The keys move from <paramref name="from"/>: left / right along a row, up / down to the row above or below (the nearest column).</summary>
        public int Move(int from, int dx, int dy)
        {
            if (Slots.Count == 0) return 0;
            var grid = Grid();
            from = Math.Max(0, Math.Min(Slots.Count - 1, from));
            (int row, int col) = grid[from];
            if (dx != 0)
            {
                int to = from + Math.Sign(dx);
                return to >= 0 && to < Slots.Count && grid[to].row == row ? to : from;
            }
            if (dy == 0) return from;
            int target = row + Math.Sign(dy);
            int best = -1;
            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i].row != target) continue;
                if (best < 0 || Math.Abs(grid[i].col - col) < Math.Abs(grid[best].col - col)) best = i;
            }
            return best < 0 ? from : best;
        }
    }

    /// <summary>What just happened in the market (the scene plays it over the new state).</summary>
    public enum MarketMoment
    {
        None,

        /// <summary>Something bought: the coins fly from the purse to it, it shakes, its prize flies to the relics (or the purse).</summary>
        Bought,

        /// <summary>A relic sold or the soul pawned: coins fly from the vendor to the purse.</summary>
        Paid,

        /// <summary>Not to be had: it shakes, the purse flashes.</summary>
        Refused,

        /// <summary>New wares: the stall's cards turn over.</summary>
        Rerolled
    }

    public readonly struct MarketFeel
    {
        public MarketMoment Moment { get; }

        /// <summary>The place it happened at (-1: none, e.g. a relic sold leaves no place behind).</summary>
        public int Slot { get; }

        public LootFlight Flight { get; }

        public MarketFeel(MarketMoment moment, int slot = -1, LootFlight flight = LootFlight.Relic)
        {
            Moment = moment;
            Slot = slot;
            Flight = flight;
        }

        public static MarketFeel None => new MarketFeel(MarketMoment.None);
    }

    /// <summary>The black market's scene (Demo 3.5). Its places are pressed by index (the presenter answers them).</summary>
    public interface IMarketView
    {
        event Action<int> SlotPressed;

        bool IsOpen { get; }

        /// <summary>True while a moment plays (a press hurries it).</summary>
        bool IsAnimating { get; }

        /// <summary>The scene in this state, and what just happened in it.</summary>
        void Show(MarketScene scene, MarketFeel feel);

        /// <summary>The keys point elsewhere: only the frames change.</summary>
        void Select(MarketScene scene);

        void Skip();

        void Hide();
    }
}
