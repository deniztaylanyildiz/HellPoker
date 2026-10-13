"""The cursed relics: a 16x16 icon each (Ui/relic_icons.png, in RelicRoster.All order), shown beside the portrait.

Order = RelicRoster.IconOrder: the demo's five (bone_die, rusty_crown, ferrymans_coin, thorned_rosary, jesters_rattle), then
Phase 2's twenty (ChapterRelics.New).
"""
import math
import os

from pixel import C, Img, sheet

ICON = 16
ORDER = ["bone_die", "rusty_crown", "ferrymans_coin", "thorned_rosary", "jesters_rattle",
         "bloody_die", "lead_purse", "ash_ledger", "candle_stub", "rat_tooth",
         "iron_vow", "pilgrims_lantern", "raven_feather", "blood_ring", "old_map", "silver_bell", "salt_circle",
         "black_book", "hangmans_rope", "liliths_tear", "abyss_coin",
         "mammons_seal", "mammons_scales", "belials_mask", "belials_tongue",
         "ash_purse", "ferrymans_lantern", "rusted_chain", "lead_seal",
         "liars_tongue", "blood_pact", "lost_souls_letter", "twin_dice", "jesters_hat", "seers_spectacles", "peasants_scythe", "kings_signet",
         "summoners_leash", "lucifers_feather", "mammons_gold_tooth"]


def icon(relic_id):
    img = Img(ICON, ICON)
    if relic_id == "bone_die":
        # A yellowed bone cube, seen from a corner: a light top, a darker side, black pips.
        top = img.m_poly([(2, 5), (8, 2), (14, 5), (8, 8)])
        left = img.m_poly([(2, 5), (8, 8), (8, 15), (2, 12)])
        right = img.m_poly([(8, 8), (14, 5), (14, 12), (8, 15)])
        img.paint(left, C.BONE_MID)
        img.paint(right, C.BONE_DARK)
        img.paint(top, C.BONE)
        img.put(8, 5, C.BLACK)
        for x, y in ((4, 8), (6, 11)):
            img.put(x, y, C.BLACK)
        for x, y in ((10, 9), (12, 8), (10, 12), (12, 11)):
            img.put(x, y, C.BLOOD)
    elif relic_id == "rusty_crown":
        # A crown gone brown with rust, one point bent.
        img.paint(img.m_rect(2, 9, 13, 13), C.GOLD_MID)
        for px in (2, 7, 12):
            img.paint(img.m_poly([(px, 9), (px + 1, 4 if px != 12 else 6), (px + 2, 9)]), C.GOLD)
        img.dither(img.m_rect(2, 9, 13, 13), C.GOLD_MID, C.BLOOD_DARK, 0.45)
        img.paint(img.m_rect(2, 13, 13, 13), C.GOLD_DARK)
        img.put(7, 11, C.CRIMSON)
        img.put(8, 11, C.CRIMSON)
    elif relic_id == "ferrymans_coin":
        # An old silver obol with a hole for the string, a skull stamped on it.
        coin = img.m_ellipse(7.5, 8, 6.5, 6.5)
        img.paint(coin, C.SILVER)
        img.dither(coin & img.m_rect(9, 0, ICON, ICON), C.SILVER, C.SILVER_DARK, 0.5)
        img.inner_outline(coin, C.SILVER_DARK)
        img.paint(img.m_ellipse(7.5, 7, 2.5, 2.5), C.BONE_SHADE)
        img.put(6, 7, C.BLACK)
        img.put(9, 7, C.BLACK)
        img.paint(img.m_rect(6, 10, 9, 10), C.BONE_SHADE)
        img.put(7, 3, C.BLACK)
        img.put(8, 3, C.BLACK)
    elif relic_id == "thorned_rosary":
        # A loop of dark beads with thorns between them, a small cross hanging below.
        for i in range(10):
            a = 2 * math.pi * i / 10
            x = int(round(8 + 5.5 * math.cos(a)))
            y = int(round(6 + 4.5 * math.sin(a)))
            img.put(x, y, C.CRIMSON if i % 2 == 0 else C.GREEN_DARK)
        img.paint(img.m_rect(7, 11, 8, 15), C.BONE_DARK)
        img.paint(img.m_rect(5, 13, 10, 13), C.BONE_DARK)
        img.put(3, 3, C.GREEN)
        img.put(13, 3, C.GREEN)
    elif relic_id == "jesters_rattle":
        # A fool's stick: a little grinning head in a three-pointed cap on a crimson handle, gold bells on the points.
        img.paint(img.m_line([(9, 15), (12, 9)], 1), C.CRIMSON)
        img.paint(img.m_rect(11, 14, 12, 15), C.GOLD_MID)
        head = img.m_ellipse(7, 8, 3.5, 3.5)
        img.paint(head, C.BONE)
        img.put(6, 8, C.BLACK)
        img.put(8, 8, C.BLACK)
        img.paint(img.m_rect(6, 10, 8, 10), C.CRIMSON)
        img.paint(img.m_poly([(5, 5), (1, 2), (3, 6)]), C.CRIMSON)
        img.paint(img.m_poly([(6, 5), (7, 0), (8, 5)]), C.GOLD)
        img.paint(img.m_poly([(9, 5), (13, 2), (11, 6)]), C.VIOLET)
        img.paint(img.m_rect(4, 5, 10, 5), C.GOLD_MID)
        for x, y in ((1, 2), (7, 0), (13, 2)):
            img.put(x, y, C.GOLD_LIGHT)
    elif relic_id == "bloody_die":
        # The bone die again, soaked: a red cube with white pips and a drip.
        top = img.m_poly([(2, 5), (8, 2), (14, 5), (8, 8)])
        left = img.m_poly([(2, 5), (8, 8), (8, 15), (2, 12)])
        right = img.m_poly([(8, 8), (14, 5), (14, 12), (8, 15)])
        img.paint(left, C.BLOOD)
        img.paint(right, C.BLOOD_DARK)
        img.paint(top, C.CRIMSON)
        img.put(8, 5, C.BONE)
        for x, y in ((4, 8), (6, 11), (11, 9), (11, 12)):
            img.put(x, y, C.BONE)
        img.paint(img.m_rect(4, 13, 4, 15), C.RED)
    elif relic_id == "lead_purse":
        # A heavy grey purse tied with a cord.
        bag = img.m_ellipse(8, 10, 6, 5)
        img.paint(bag, C.SILVER_DARK)
        img.dither(bag & img.m_rect(9, 0, ICON, ICON), C.SILVER_DARK, C.BONE_SHADE, 0.5)
        img.paint(img.m_poly([(5, 6), (8, 2), (11, 6)]), C.SILVER_DARK)
        img.paint(img.m_rect(5, 5, 11, 5), C.GOLD_MID)
        img.put(7, 9, C.SILVER)
        img.put(8, 10, C.SILVER)
    elif relic_id == "ash_ledger":
        # A charred ledger: grey pages, black cover edges, a faint ember.
        img.paint(img.m_rect(2, 3, 13, 13), C.BONE_SHADE)
        img.paint(img.m_rect(3, 4, 12, 12), C.BONE_DARK)
        img.paint(img.m_rect(7, 3, 8, 13), C.NIGHT)
        for y in (6, 8, 10):
            img.paint(img.m_rect(4, y, 6, y), C.BONE_SHADE)
            img.paint(img.m_rect(9, y, 11, y), C.BONE_SHADE)
        img.put(12, 12, C.EMBER)
        img.put(11, 12, C.ORANGE)
    elif relic_id == "candle_stub":
        # A short bone-white candle with a small flame.
        img.paint(img.m_rect(5, 8, 10, 14), C.BONE_MID)
        img.paint(img.m_rect(9, 8, 10, 14), C.BONE_DARK)
        img.paint(img.m_rect(4, 14, 11, 14), C.GOLD_DARK)
        img.put(7, 7, C.BLACK)
        img.paint(img.m_ellipse(7.5, 4.5, 1.6, 2.6), C.AMBER)
        img.put(7, 5, C.EMBER)
    elif relic_id == "rat_tooth":
        # A long yellow incisor on a cord.
        img.paint(img.m_line([(3, 3), (13, 3)], 1), C.BONE_SHADE)
        img.paint(img.m_poly([(6, 4), (10, 4), (9, 14), (8, 15)]), C.BONE)
        img.paint(img.m_poly([(9, 4), (10, 4), (9, 14)]), C.GOLD_MID)
    elif relic_id == "iron_vow":
        # A dark iron ring with a lock on it.
        ring = img.m_ellipse(8, 7, 5.5, 5.5) & ~img.m_ellipse(8, 7, 3.5, 3.5)
        img.paint(ring, C.SILVER_DARK)
        img.paint(img.m_rect(5, 9, 10, 14), C.BONE_SHADE)
        img.paint(img.m_rect(6, 10, 9, 13), C.SILVER_DARK)
        img.put(7, 11, C.BLACK)
        img.put(8, 11, C.BLACK)
        img.put(7, 12, C.BLACK)
    elif relic_id == "pilgrims_lantern":
        # A small iron lantern with a warm light inside.
        img.paint(img.m_rect(7, 1, 8, 2), C.SILVER_DARK)
        img.paint(img.m_rect(4, 3, 11, 4), C.SILVER_DARK)
        img.paint(img.m_rect(4, 5, 11, 13), C.NIGHT)
        img.paint(img.m_rect(5, 6, 10, 12), C.AMBER)
        img.paint(img.m_rect(6, 8, 9, 11), C.EMBER)
        img.paint(img.m_rect(7, 5, 8, 13), C.SILVER_DARK)
        img.paint(img.m_rect(3, 14, 12, 14), C.SILVER_DARK)
    elif relic_id == "raven_feather":
        # A black feather with a violet sheen.
        img.paint(img.m_line([(3, 14), (12, 2)], 1), C.BONE_SHADE)
        img.paint(img.m_poly([(5, 11), (12, 2), (10, 10)]), C.NIGHT)
        img.paint(img.m_poly([(4, 9), (11, 3), (6, 12)]), C.DUSK)
        img.put(9, 6, C.VIOLET)
        img.put(7, 9, C.VIOLET)
    elif relic_id == "blood_ring":
        # A gold ring with a red stone.
        ring = img.m_ellipse(8, 10, 5, 4) & ~img.m_ellipse(8, 10, 3, 2.2)
        img.paint(ring, C.GOLD)
        img.dither(ring & img.m_rect(9, 0, ICON, ICON), C.GOLD, C.GOLD_MID, 0.5)
        img.paint(img.m_ellipse(8, 5, 2.6, 2.4), C.CRIMSON)
        img.put(7, 4, C.HELL)
    elif relic_id == "old_map":
        # A torn parchment with a red route and a cross.
        img.paint(img.m_poly([(2, 3), (14, 2), (13, 14), (3, 13)]), C.BONE_MID)
        img.dither(img.m_rect(2, 9, 14, 14), C.BONE_MID, C.BONE_DARK, 0.4)
        for x, y in ((4, 11), (5, 10), (6, 10), (7, 9), (8, 7), (9, 6)):
            img.put(x, y, C.BLOOD)
        img.put(10, 4, C.BLACK)
        img.put(12, 4, C.BLACK)
        img.put(11, 5, C.BLACK)
        img.put(10, 6, C.BLACK)
        img.put(12, 6, C.BLACK)
    elif relic_id == "silver_bell":
        # A small silver bell on a loop.
        img.paint(img.m_rect(7, 1, 8, 3), C.SILVER_DARK)
        bell = img.m_poly([(5, 4), (11, 4), (13, 12), (3, 12)])
        img.paint(bell, C.SILVER)
        img.dither(bell & img.m_rect(9, 0, ICON, ICON), C.SILVER, C.SILVER_DARK, 0.5)
        img.paint(img.m_rect(2, 12, 13, 13), C.SILVER_DARK)
        img.paint(img.m_rect(7, 14, 8, 15), C.BONE_SHADE)
    elif relic_id == "salt_circle":
        # A ring of white grains on the ground.
        for i in range(14):
            a = 2 * math.pi * i / 14
            x = int(round(7.5 + 6 * math.cos(a)))
            y = int(round(8 + 4.5 * math.sin(a)))
            img.put(x, y, C.WHITE if i % 2 == 0 else C.BONE)
        img.put(7, 8, C.BONE_SHADE)
    elif relic_id == "black_book":
        # A black grimoire with a red pentacle-like mark and gold corners.
        img.paint(img.m_rect(3, 2, 13, 14), C.BLACK)
        img.paint(img.m_rect(4, 3, 12, 13), C.NIGHT)
        img.paint(img.m_rect(3, 2, 4, 14), C.DUSK)
        for x, y in ((12, 3), (12, 13), (5, 3), (5, 13)):
            img.put(x, y, C.GOLD)
        img.paint(img.m_ellipse(8.5, 8, 2.6, 2.6) & ~img.m_ellipse(8.5, 8, 1.4, 1.4), C.CRIMSON)
    elif relic_id == "hangmans_rope":
        # A noose of brown rope.
        loop = img.m_ellipse(8, 10, 4, 4) & ~img.m_ellipse(8, 10, 2.5, 2.5)
        img.paint(loop, C.GOLD_DARK)
        img.paint(img.m_rect(7, 1, 8, 6), C.GOLD_DARK)
        img.paint(img.m_rect(6, 5, 9, 7), C.GOLD_MID)
        img.put(6, 6, C.GOLD_DARK)
        img.put(9, 6, C.GOLD_DARK)
    elif relic_id == "liliths_tear":
        # A lilac teardrop of crystal.
        tear = img.m_poly([(8, 1), (12, 9), (8, 14), (4, 9)]) | img.m_ellipse(8, 10, 4, 4)
        img.paint(tear, C.LILAC)
        img.dither(tear & img.m_rect(8, 0, ICON, ICON), C.LILAC, C.MAUVE, 0.5)
        img.put(6, 8, C.LILAC_LIGHT)
        img.put(6, 9, C.WHITE)
    elif relic_id == "abyss_coin":
        # A black coin with a red void in its middle.
        coin = img.m_ellipse(7.5, 8, 6.5, 6.5)
        img.paint(coin, C.DUSK)
        img.inner_outline(coin, C.GOLD_DARK)
        img.paint(img.m_ellipse(7.5, 8, 3, 3), C.BLACK)
        img.put(7, 8, C.BLOOD)
        img.put(8, 8, C.CRIMSON)
    elif relic_id == "mammons_seal":
        # Mammon's green-gold seal: a round stamp with a coin mark.
        img.paint(img.m_rect(6, 1, 9, 6), C.GOLD_DARK)
        seal = img.m_ellipse(8, 11, 6, 4)
        img.paint(seal, C.GREEN)
        img.inner_outline(seal, C.GOLD)
        img.paint(img.m_ellipse(8, 11, 2, 1.6), C.GOLD_LIGHT)
    elif relic_id == "mammons_scales":
        # Golden scales, the left pan heavier.
        img.paint(img.m_rect(7, 2, 8, 13), C.GOLD_MID)
        img.paint(img.m_rect(4, 14, 11, 14), C.GOLD_DARK)
        img.paint(img.m_line([(2, 5), (13, 3)], 1), C.GOLD)
        img.paint(img.m_rect(1, 8, 5, 9), C.GOLD)
        img.paint(img.m_rect(11, 6, 14, 7), C.GOLD)
        img.put(3, 7, C.GREEN_LIGHT)
        img.put(2, 6, C.GOLD_DARK)
        img.put(12, 4, C.GOLD_DARK)
    elif relic_id == "belials_mask":
        # Belial's silver theatre mask, a smile too wide.
        face = img.m_ellipse(8, 8, 6, 6.5)
        img.paint(face, C.SILVER)
        img.dither(face & img.m_rect(9, 0, ICON, ICON), C.SILVER, C.SILVER_DARK, 0.5)
        img.paint(img.m_rect(4, 6, 6, 7), C.BLACK)
        img.paint(img.m_rect(10, 6, 12, 7), C.BLACK)
        img.paint(img.m_poly([(4, 10), (12, 10), (8, 13)]), C.BLACK)
        img.put(8, 11, C.CRIMSON)
    elif relic_id == "belials_tongue":
        # A forked red tongue curling out of a silver ring.
        ring = img.m_ellipse(8, 4, 3.5, 3) & ~img.m_ellipse(8, 4, 2, 1.6)
        img.paint(ring, C.SILVER)
        img.paint(img.m_line([(8, 6), (8, 10), (6, 13), (4, 14)], 1), C.CRIMSON)
        img.paint(img.m_line([(8, 10), (11, 13), (13, 14)], 1), C.CRIMSON)
        img.put(8, 8, C.RED)
    elif relic_id == "ash_purse":
        # A grey purse smudged with ash, a coin peeking out.
        bag = img.m_ellipse(8, 10, 5, 4.5)
        img.paint(bag, C.SILVER_DARK)
        img.dither(bag, C.SILVER_DARK, C.BONE_SHADE, 0.4)
        img.paint(img.m_rect(6, 4, 10, 6), C.BONE_SHADE)
        img.paint(img.m_ellipse(11, 6, 2, 2), C.GOLD)
    elif relic_id == "ferrymans_lantern":
        # A small lantern with a pale green flame.
        img.paint(img.m_rect(5, 4, 11, 13), C.GOLD_DARK)
        img.paint(img.m_rect(6, 5, 10, 12), C.BLACK)
        img.paint(img.m_ellipse(8, 9, 1.5, 2.5), C.GREEN_LIGHT)
        img.paint(img.m_rect(7, 1, 9, 3), C.GOLD_DARK)
    elif relic_id == "rusted_chain":
        # Three rusted links.
        for k, (x, y) in enumerate(((3, 4), (7, 8), (11, 12))):
            link = img.m_ellipse(x + 0.5, y, 2.6, 2) & ~img.m_ellipse(x + 0.5, y, 1.2, 0.8)
            img.paint(link, C.GOLD_DARK if k % 2 else C.BLOOD)
    elif relic_id == "lead_seal":
        # A heavy grey seal stamped with a skull.
        img.paint(img.m_ellipse(8, 8, 6, 6), C.SILVER_DARK)
        img.paint(img.m_ellipse(8, 8, 4.5, 4.5), C.BONE_SHADE)
        img.paint(img.m_ellipse(8, 7, 2.2, 2), C.BONE_MID)
        img.put(7, 7, C.BLACK)
        img.put(9, 7, C.BLACK)
        img.paint(img.m_rect(7, 9, 9, 10), C.BONE_MID)
    elif relic_id == "liars_tongue":
        # A silver tongue, split, on a black lip.
        img.paint(img.m_rect(3, 5, 12, 6), C.BLACK)
        img.paint(img.m_poly([(5, 6), (11, 6), (10, 12), (8, 10), (6, 12)]), C.SILVER)
        img.put(8, 8, C.WHITE)
    elif relic_id == "blood_pact":
        # A scroll sealed with a drop of blood.
        img.paint(img.m_rect(3, 3, 12, 12), C.BONE)
        for y in (5, 7, 9):
            img.paint(img.m_rect(4, y, 10, y), C.BONE_DARK)
        img.paint(img.m_ellipse(11, 11, 2.2, 2.2), C.CRIMSON)
        img.put(11, 14, C.CRIMSON)
    elif relic_id == "lost_souls_letter":
        # A pale envelope with a lilac seal.
        img.paint(img.m_rect(2, 4, 13, 12), C.BONE)
        img.paint(img.m_line([(2, 4), (8, 9), (13, 4)], 1), C.BONE_DARK)
        img.paint(img.m_ellipse(8, 9, 1.6, 1.6), C.LILAC)
    elif relic_id == "twin_dice":
        # Two small red dice.
        for x, y in ((2, 3), (8, 8)):
            die = img.m_rect(x, y, x + 5, y + 5)
            img.paint(die, C.CRIMSON)
            img.put(x + 1, y + 1, C.BONE)
            img.put(x + 4, y + 4, C.BONE)
    elif relic_id == "jesters_hat":
        # A two-pointed jester's cap, a bell on each point.
        img.paint(img.m_poly([(3, 13), (2, 4), (8, 10)]), C.CRIMSON)
        img.paint(img.m_poly([(13, 13), (14, 4), (8, 10)]), C.VIOLET)
        img.paint(img.m_rect(3, 11, 13, 13), C.GOLD_MID)
        img.put(2, 3, C.GOLD)
        img.put(14, 3, C.GOLD)
    elif relic_id == "seers_spectacles":
        # Round spectacles, one lens glowing.
        img.paint(img.m_ellipse(5, 8, 3, 3) & ~img.m_ellipse(5, 8, 2, 2), C.GOLD)
        img.paint(img.m_ellipse(11, 8, 3, 3) & ~img.m_ellipse(11, 8, 2, 2), C.GOLD)
        img.paint(img.m_ellipse(11, 8, 2, 2), C.LILAC_LIGHT)
        img.paint(img.m_rect(7, 7, 9, 7), C.GOLD_DARK)
    elif relic_id == "peasants_scythe":
        # A scythe: a wooden haft and a curved blade.
        img.paint(img.m_line([(4, 14), (10, 3)], 1), C.GOLD_DARK)
        img.paint(img.m_poly([(10, 3), (15, 4), (12, 7), (10, 5)]), C.SILVER)
    elif relic_id == "kings_signet":
        # A gold ring with a red stone.
        img.paint(img.m_ellipse(8, 10, 5, 4) & ~img.m_ellipse(8, 10, 3, 2.4), C.GOLD)
        img.paint(img.m_ellipse(8, 5, 2.6, 2.4), C.CRIMSON)
        img.put(7, 4, C.HELL)
    elif relic_id == "summoners_leash":
        # A collar with a chain leash.
        img.paint(img.m_ellipse(6, 10, 4, 3) & ~img.m_ellipse(6, 10, 2.6, 1.8), C.BLOOD)
        for k in range(4):
            img.put(9 + k, 8 - k * 2, C.SILVER)
            img.put(10 + k, 8 - k * 2, C.SILVER_DARK)
    elif relic_id == "lucifers_feather":
        # A black feather edged with ember.
        img.paint(img.m_poly([(3, 14), (6, 7), (12, 2), (10, 9)]), C.NIGHT)
        img.paint(img.m_line([(3, 14), (11, 3)], 1), C.EMBER)
        img.put(12, 2, C.HELL)
    elif relic_id == "mammons_gold_tooth":
        # A gold molar with its roots.
        img.paint(img.m_poly([(4, 3), (12, 3), (12, 9), (10, 14), (8, 10), (6, 14), (4, 9)]), C.GOLD)
        img.paint(img.m_rect(5, 4, 7, 5), C.GOLD_LIGHT)
        img.dither(img.m_rect(4, 8, 12, 14), C.GOLD, C.GOLD_DARK, 0.3)
    img.outline(C.BLACK)
    return img
def write_all(art_dir):
    path = os.path.join(art_dir, "Ui", "relic_icons.png")
    sheet([icon(r) for r in ORDER]).save(path)
    return [path]