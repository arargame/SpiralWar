using Microsoft.Xna.Framework.Audio;

namespace SpiralWar.Audio
{
    /// <summary>
    /// The recipe book. Each method returns one finished <see cref="SoundEffect"/> built by
    /// <see cref="SoundBuilder"/>; nothing here touches the file system.
    ///
    /// Tuning notes (see docs/audio-design.md):
    ///  * Fire sounds live below 2.6 kHz and peak at 0.5 so a held trigger never fatigues.
    ///  * Impact sounds are a low "body" plus a very short filtered click: the click reads as
    ///    "I hit something" and the body reads as "it was solid".
    ///  * Everything is pitch-varied at play time, which is what removes the machine-gun buzz.
    /// </summary>
    public static class SoundBank
    {
        /// <summary>Builds every sound in <see cref="SoundId"/> order.</summary>
        public static SoundEffect[] BuildAll()
        {
            var sounds = new SoundEffect[(int)SoundId.Count];

            sounds[(int)SoundId.Shoot]           = BuildShoot();
            sounds[(int)SoundId.ShootWide]       = BuildShootWide();
            sounds[(int)SoundId.SegmentHit]      = BuildSegmentHit();
            sounds[(int)SoundId.SegmentArmorHit] = BuildArmorHit();
            sounds[(int)SoundId.SegmentDestroy]  = BuildSegmentDestroy();
            sounds[(int)SoundId.SupplyDrop]      = BuildSupplyDrop();
            sounds[(int)SoundId.SupplyPickup]    = BuildSupplyPickup();
            sounds[(int)SoundId.HealthPickup]    = BuildHealthPickup();
            sounds[(int)SoundId.PlayerHurt]      = BuildPlayerHurt();
            sounds[(int)SoundId.PlayerDown]      = BuildPlayerDown();
            sounds[(int)SoundId.LevelClear]      = BuildLevelClear();
            sounds[(int)SoundId.BossWarning]     = BuildBossWarning();
            sounds[(int)SoundId.MenuMove]        = BuildMenuMove();
            sounds[(int)SoundId.MenuSelect]      = BuildMenuSelect();
            sounds[(int)SoundId.MenuBack]        = BuildMenuBack();

            sounds[(int)SoundId.CriticalHit]     = BuildCriticalHit();
            sounds[(int)SoundId.DynamiteBlast]   = BuildDynamiteBlast();
            sounds[(int)SoundId.DynamiteChain]   = BuildDynamiteChain();
            sounds[(int)SoundId.AcidTick]        = BuildAcidTick();
            sounds[(int)SoundId.MissileLaunch]   = BuildMissileLaunch();
            sounds[(int)SoundId.MissileImpact]   = BuildMissileImpact();
            sounds[(int)SoundId.DiamondDrop]     = BuildDiamondDrop();
            sounds[(int)SoundId.DiamondPickup]   = BuildDiamondPickup();
            sounds[(int)SoundId.DiamondLevelUp]  = BuildDiamondLevelUp();
            sounds[(int)SoundId.StasisStart]     = BuildStasisStart();
            sounds[(int)SoundId.StasisEnd]       = BuildStasisEnd();

            return sounds;
        }

        // ── Weapons ────────────────────────────────────────────────────────

        /// <summary>Soft downward "thup". Deliberately dull: it is heard six times a second.</summary>
        private static SoundEffect BuildShoot()
        {
            return new SoundBuilder(0.11f, seed: 11)
                .Tone(0f, 0.09f, 760f, 240f, 0.42f, SoundBuilder.Percussive(5.5f, 0.012f), Waveform.Triangle)
                .Tone(0f, 0.05f, 1520f, 520f, 0.10f, SoundBuilder.Percussive(9f, 0.006f))
                .Noise(0f, 0.012f, 0.14f, SoundBuilder.Percussive(10f, 0.004f), lowPassHz: 3200f)
                .LowPass(2600f)
                .SoftClip(1.35f)
                .NormalizeTo(0.50f)
                .FadeEdges(3f)
                .Build("sfx_shoot");
        }

        /// <summary>The wide volley. Same character, broader and a touch lower.</summary>
        private static SoundEffect BuildShootWide()
        {
            return new SoundBuilder(0.14f, seed: 12)
                .Tone(0f, 0.11f, 620f, 190f, 0.40f, SoundBuilder.Percussive(4.5f, 0.014f), Waveform.Triangle)
                .Tone(0.004f, 0.09f, 930f, 300f, 0.18f, SoundBuilder.Percussive(6f, 0.010f), Waveform.Triangle)
                .Noise(0f, 0.020f, 0.16f, SoundBuilder.Percussive(8f, 0.005f), lowPassHz: 2800f)
                .LowPass(2300f)
                .SoftClip(1.4f)
                .NormalizeTo(0.55f)
                .FadeEdges(3f)
                .Build("sfx_shoot_wide");
        }

        // ── Impacts ────────────────────────────────────────────────────────

        /// <summary>
        /// The "it hurt" tick. Short click for clarity + low body for weight. Played with pitch
        /// tied to the damage dealt so big hits read as bigger.
        /// </summary>
        private static SoundEffect BuildSegmentHit()
        {
            return new SoundBuilder(0.10f, seed: 21)
                .FmBody(0f, 0.075f, 330f, 140f, 220f, 0.55f, SoundBuilder.Percussive(7f, 0.004f))
                .Noise(0f, 0.014f, 0.30f, SoundBuilder.Percussive(12f, 0.002f), lowPassHz: 5200f)
                .Tone(0f, 0.03f, 1250f, 700f, 0.12f, SoundBuilder.Percussive(14f, 0.002f))
                .HighPass(90f)
                .LowPass(6200f)
                .SoftClip(1.5f)
                .NormalizeTo(0.72f)
                .FadeEdges(2.5f)
                .Build("sfx_segment_hit");
        }

        /// <summary>Armored rings answer with a harder, more metallic clank.</summary>
        private static SoundEffect BuildArmorHit()
        {
            return new SoundBuilder(0.16f, seed: 22)
                .FmBody(0f, 0.13f, 470f, 317f, 430f, 0.50f, SoundBuilder.Percussive(5.5f, 0.003f))
                .FmBody(0.002f, 0.07f, 940f, 611f, 260f, 0.22f, SoundBuilder.Percussive(9f, 0.002f))
                .Noise(0f, 0.020f, 0.26f, SoundBuilder.Percussive(11f, 0.002f), lowPassHz: 6500f)
                .HighPass(120f)
                .LowPass(7000f)
                .SoftClip(1.7f)
                .NormalizeTo(0.74f)
                .FadeEdges(2.5f)
                .Build("sfx_armor_hit");
        }

        /// <summary>
        /// A critical. It plays ON TOP of the normal hit rather than replacing it, so a crit
        /// sounds like the same impact with something extra - a bright, glassy ping that cuts
        /// through without being louder.
        /// </summary>
        private static SoundEffect BuildCriticalHit()
        {
            return new SoundBuilder(0.22f, seed: 23)
                .Tone(0f, 0.18f, 2640f, 2640f, 0.26f, SoundBuilder.Percussive(5f, 0.002f))
                .Tone(0f, 0.12f, 3960f, 3700f, 0.12f, SoundBuilder.Percussive(7f, 0.002f))
                .Tone(0f, 0.06f, 1320f, 900f, 0.20f, SoundBuilder.Percussive(10f, 0.002f), Waveform.Triangle)
                .HighPass(700f)
                .LowPass(7400f)
                .SoftClip(1.3f)
                .NormalizeTo(0.58f)
                .FadeEdges(3f)
                .Build("sfx_critical");
        }

        /// <summary>Ring breaks apart: filtered burst falling into a sub thump.</summary>
        private static SoundEffect BuildSegmentDestroy()
        {
            return new SoundBuilder(0.34f, seed: 24)
                .Noise(0f, 0.22f, 0.55f, SoundBuilder.Percussive(5f, 0.004f), lowPassHz: 4200f)
                .Noise(0f, 0.10f, 0.28f, SoundBuilder.Percussive(9f, 0.002f), lowPassHz: 1400f)
                .Tone(0f, 0.30f, 260f, 58f, 0.62f, SoundBuilder.Percussive(3.2f, 0.006f), Waveform.Triangle)
                .Tone(0f, 0.09f, 900f, 320f, 0.16f, SoundBuilder.Percussive(8f, 0.003f))
                .LowPass(5200f)
                .SoftClip(1.6f)
                .NormalizeTo(0.85f)
                .FadeEdges(4f)
                .Build("sfx_segment_destroy");
        }

        // ── Dynamite ───────────────────────────────────────────────────────

        /// <summary>
        /// The payoff sound of the whole update. Three layers: a sub drop you feel, a long
        /// filtered noise body, and a short metallic crack on the transient so it reads as an
        /// explosion rather than as a wash.
        /// </summary>
        private static SoundEffect BuildDynamiteBlast()
        {
            return new SoundBuilder(0.85f, seed: 31)
                .Tone(0f, 0.70f, 150f, 34f, 0.85f, SoundBuilder.Percussive(2.6f, 0.004f), Waveform.Triangle)
                .Noise(0f, 0.60f, 0.70f, SoundBuilder.Percussive(3.4f, 0.003f), lowPassHz: 3200f)
                .Noise(0f, 0.16f, 0.45f, SoundBuilder.Percussive(8f, 0.001f), lowPassHz: 7000f)
                .FmBody(0f, 0.22f, 220f, 97f, 420f, 0.40f, SoundBuilder.Percussive(6f, 0.002f))
                .Echo(0.085f, 0.26f, 2)
                .LowPass(4600f)
                .SoftClip(1.9f)
                .NormalizeTo(0.95f)
                .FadeEdges(6f)
                .Build("sfx_dynamite");
        }

        /// <summary>
        /// A link in a chain, not the ring the player shot. Shorter, drier and 15% quieter, so
        /// a six-charge cascade builds instead of turning into one undifferentiated roar.
        /// </summary>
        private static SoundEffect BuildDynamiteChain()
        {
            return new SoundBuilder(0.46f, seed: 32)
                .Tone(0f, 0.38f, 180f, 46f, 0.72f, SoundBuilder.Percussive(3.6f, 0.003f), Waveform.Triangle)
                .Noise(0f, 0.30f, 0.56f, SoundBuilder.Percussive(4.6f, 0.002f), lowPassHz: 3600f)
                .Noise(0f, 0.09f, 0.34f, SoundBuilder.Percussive(10f, 0.001f), lowPassHz: 6800f)
                .LowPass(5000f)
                .SoftClip(1.8f)
                .NormalizeTo(0.80f)
                .FadeEdges(4f)
                .Build("sfx_dynamite_chain");
        }

        // ── Diamonds and status ────────────────────────────────────────────

        /// <summary>A single acid tick. Very quiet: it fires twice a second on many rings.</summary>
        private static SoundEffect BuildAcidTick()
        {
            return new SoundBuilder(0.09f, seed: 33)
                .Noise(0f, 0.07f, 0.30f, SoundBuilder.Percussive(9f, 0.004f), lowPassHz: 4800f)
                .Tone(0f, 0.05f, 1700f, 1150f, 0.10f, SoundBuilder.Percussive(11f, 0.003f))
                .HighPass(900f)
                .LowPass(5600f)
                .NormalizeTo(0.26f)
                .FadeEdges(3f)
                .Build("sfx_acid_tick");
        }

        /// <summary>Missile away: a short rising hiss with a thump underneath.</summary>
        private static SoundEffect BuildMissileLaunch()
        {
            return new SoundBuilder(0.26f, seed: 34)
                .Noise(0f, 0.22f, 0.42f, SoundBuilder.Percussive(3.6f, 0.02f), lowPassHz: 3000f)
                .Tone(0f, 0.16f, 220f, 520f, 0.26f, SoundBuilder.Percussive(4.5f, 0.01f), Waveform.Triangle)
                .LowPass(3400f)
                .SoftClip(1.4f)
                .NormalizeTo(0.45f)
                .FadeEdges(4f)
                .Build("sfx_missile_launch");
        }

        /// <summary>Missile impact: a compact version of the dynamite blast.</summary>
        private static SoundEffect BuildMissileImpact()
        {
            return new SoundBuilder(0.34f, seed: 35)
                .Tone(0f, 0.26f, 230f, 62f, 0.66f, SoundBuilder.Percussive(4f, 0.003f), Waveform.Triangle)
                .Noise(0f, 0.20f, 0.48f, SoundBuilder.Percussive(5.5f, 0.002f), lowPassHz: 4000f)
                .LowPass(5000f)
                .SoftClip(1.7f)
                .NormalizeTo(0.78f)
                .FadeEdges(4f)
                .Build("sfx_missile_impact");
        }

        /// <summary>A diamond has been released. Glassy, high and short.</summary>
        private static SoundEffect BuildDiamondDrop()
        {
            return new SoundBuilder(0.26f, seed: 36)
                .Tone(0f, 0.22f, 2093f, 2093f, 0.26f, SoundBuilder.Percussive(5.5f, 0.02f))
                .Tone(0.01f, 0.20f, 3136f, 3136f, 0.13f, SoundBuilder.Percussive(6.5f, 0.02f))
                .HighPass(900f)
                .LowPass(7200f)
                .NormalizeTo(0.38f)
                .FadeEdges(4f)
                .Build("sfx_diamond_drop");
        }

        /// <summary>
        /// Collecting a diamond: a crystalline major triad with a long tail. Distinctly
        /// different from a supply's rising fifth, because the two matter differently.
        /// </summary>
        private static SoundEffect BuildDiamondPickup()
        {
            return new SoundBuilder(0.46f, seed: 37)
                .Tone(0.00f, 0.30f, 1046f, 1046f, 0.34f, SoundBuilder.Ads(0.03f, 0.6f))
                .Tone(0.03f, 0.30f, 1568f, 1568f, 0.24f, SoundBuilder.Ads(0.03f, 0.6f))
                .Tone(0.06f, 0.34f, 2093f, 2093f, 0.18f, SoundBuilder.Ads(0.03f, 0.7f))
                .Echo(0.10f, 0.30f, 2)
                .HighPass(500f)
                .LowPass(7600f)
                .SoftClip(1.2f)
                .NormalizeTo(0.60f)
                .FadeEdges(5f)
                .Build("sfx_diamond_pickup");
        }

        /// <summary>A stack actually levelled up - an extra octave on top of the pickup.</summary>
        private static SoundEffect BuildDiamondLevelUp()
        {
            return new SoundBuilder(0.56f, seed: 38)
                .Tone(0.00f, 0.14f, 1046f, 1046f, 0.30f, SoundBuilder.Ads(0.04f, 0.5f))
                .Tone(0.10f, 0.14f, 1568f, 1568f, 0.30f, SoundBuilder.Ads(0.04f, 0.5f))
                .Tone(0.20f, 0.34f, 2093f, 2093f, 0.36f, SoundBuilder.Ads(0.03f, 0.65f))
                .Tone(0.20f, 0.34f, 3136f, 3136f, 0.14f, SoundBuilder.Ads(0.03f, 0.65f))
                .Echo(0.11f, 0.32f, 3)
                .HighPass(500f)
                .LowPass(7800f)
                .SoftClip(1.2f)
                .NormalizeTo(0.64f)
                .FadeEdges(6f)
                .Build("sfx_diamond_levelup");
        }

        /// <summary>
        /// The stasis field snapping on. A descending shimmer: the pitch falling is the whole
        /// message, because the thing it just did was make time slower.
        /// </summary>
        private static SoundEffect BuildStasisStart()
        {
            return new SoundBuilder(0.70f, seed: 39)
                .Tone(0f, 0.60f, 1760f, 440f, 0.34f, SoundBuilder.Ads(0.04f, 0.5f), Waveform.Triangle)
                .Tone(0.02f, 0.58f, 1320f, 330f, 0.22f, SoundBuilder.Ads(0.04f, 0.5f), Waveform.Triangle)
                .Noise(0f, 0.30f, 0.16f, SoundBuilder.Percussive(4f, 0.02f), lowPassHz: 2200f)
                .Echo(0.13f, 0.30f, 2)
                .LowPass(5200f)
                .SoftClip(1.3f)
                .NormalizeTo(0.60f)
                .FadeEdges(6f)
                .Build("sfx_stasis_start");
        }

        /// <summary>The field collapsing: the same shimmer running back up.</summary>
        private static SoundEffect BuildStasisEnd()
        {
            return new SoundBuilder(0.42f, seed: 40)
                .Tone(0f, 0.34f, 440f, 1760f, 0.28f, SoundBuilder.Ads(0.05f, 0.5f), Waveform.Triangle)
                .LowPass(5600f)
                .NormalizeTo(0.44f)
                .FadeEdges(5f)
                .Build("sfx_stasis_end");
        }

        // ── Supplies ───────────────────────────────────────────────────────

        /// <summary>Quiet chime telling the player something just became collectable.</summary>
        private static SoundEffect BuildSupplyDrop()
        {
            return new SoundBuilder(0.20f, seed: 41)
                .Tone(0f, 0.16f, 1180f, 1180f, 0.30f, SoundBuilder.Percussive(6f, 0.03f))
                .Tone(0.02f, 0.14f, 1770f, 1770f, 0.14f, SoundBuilder.Percussive(7f, 0.03f))
                .LowPass(5200f)
                .NormalizeTo(0.42f)
                .FadeEdges(4f)
                .Build("sfx_supply_drop");
        }

        /// <summary>Rising fifth. Unmistakably "good thing acquired".</summary>
        private static SoundEffect BuildSupplyPickup()
        {
            return new SoundBuilder(0.26f, seed: 42)
                .Tone(0.00f, 0.10f, 659f, 659f, 0.40f, SoundBuilder.Ads(0.08f, 0.45f), Waveform.Triangle)
                .Tone(0.08f, 0.16f, 988f, 988f, 0.38f, SoundBuilder.Ads(0.06f, 0.55f), Waveform.Triangle)
                .Tone(0.08f, 0.16f, 1976f, 1976f, 0.08f, SoundBuilder.Ads(0.06f, 0.55f))
                .LowPass(6000f)
                .SoftClip(1.2f)
                .NormalizeTo(0.62f)
                .FadeEdges(4f)
                .Build("sfx_supply_pickup");
        }

        /// <summary>Major triad - warmer and longer than the weapon pickups.</summary>
        private static SoundEffect BuildHealthPickup()
        {
            return new SoundBuilder(0.40f, seed: 43)
                .Tone(0.00f, 0.12f, 523f, 523f, 0.36f, SoundBuilder.Ads(0.08f, 0.5f), Waveform.Triangle)
                .Tone(0.09f, 0.12f, 659f, 659f, 0.36f, SoundBuilder.Ads(0.08f, 0.5f), Waveform.Triangle)
                .Tone(0.18f, 0.20f, 784f, 784f, 0.40f, SoundBuilder.Ads(0.06f, 0.6f), Waveform.Triangle)
                .Echo(0.09f, 0.28f, 2)
                .LowPass(5200f)
                .SoftClip(1.2f)
                .NormalizeTo(0.62f)
                .FadeEdges(5f)
                .Build("sfx_health_pickup");
        }

        // ── Player state ───────────────────────────────────────────────────

        /// <summary>Dull, chest-level thud. Heavily low-passed so it never sounds shrill.</summary>
        private static SoundEffect BuildPlayerHurt()
        {
            return new SoundBuilder(0.30f, seed: 44)
                .Tone(0f, 0.26f, 210f, 62f, 0.70f, SoundBuilder.Percussive(4f, 0.008f), Waveform.Triangle)
                .Noise(0f, 0.09f, 0.30f, SoundBuilder.Percussive(7f, 0.004f), lowPassHz: 900f)
                .LowPass(1100f)
                .SoftClip(1.5f)
                .NormalizeTo(0.80f)
                .FadeEdges(5f)
                .Build("sfx_player_hurt");
        }

        /// <summary>Run over: long descending sweep with a tail.</summary>
        private static SoundEffect BuildPlayerDown()
        {
            return new SoundBuilder(1.10f, seed: 45)
                .Tone(0f, 0.95f, 420f, 74f, 0.62f, SoundBuilder.Percussive(1.8f, 0.03f), Waveform.Triangle)
                .Tone(0.05f, 0.80f, 208f, 55f, 0.34f, SoundBuilder.Percussive(1.6f, 0.05f))
                .Noise(0f, 0.55f, 0.20f, SoundBuilder.Percussive(3f, 0.02f), lowPassHz: 1800f)
                .Echo(0.16f, 0.30f, 2)
                .LowPass(2600f)
                .SoftClip(1.3f)
                .NormalizeTo(0.72f)
                .FadeEdges(8f)
                .Build("sfx_player_down");
        }

        /// <summary>Level cleared: rising arpeggio with a short neon tail.</summary>
        private static SoundEffect BuildLevelClear()
        {
            return new SoundBuilder(1.00f, seed: 46)
                .Tone(0.00f, 0.14f, 392f, 392f, 0.34f, SoundBuilder.Ads(0.06f, 0.5f), Waveform.Triangle)
                .Tone(0.12f, 0.14f, 523f, 523f, 0.34f, SoundBuilder.Ads(0.06f, 0.5f), Waveform.Triangle)
                .Tone(0.24f, 0.14f, 659f, 659f, 0.34f, SoundBuilder.Ads(0.06f, 0.5f), Waveform.Triangle)
                .Tone(0.36f, 0.42f, 784f, 784f, 0.42f, SoundBuilder.Ads(0.05f, 0.65f), Waveform.Triangle)
                .Tone(0.36f, 0.42f, 1568f, 1568f, 0.09f, SoundBuilder.Ads(0.05f, 0.65f))
                .Echo(0.13f, 0.32f, 3)
                .LowPass(6200f)
                .SoftClip(1.25f)
                .NormalizeTo(0.68f)
                .FadeEdges(8f)
                .Build("sfx_level_clear");
        }

        /// <summary>Boss level intro: slow, low, swelling pulse.</summary>
        private static SoundEffect BuildBossWarning()
        {
            return new SoundBuilder(1.20f, seed: 47)
                .Tone(0.00f, 0.35f, 88f, 92f, 0.60f, SoundBuilder.Swell(0.4f), Waveform.Triangle)
                .Tone(0.42f, 0.35f, 88f, 92f, 0.60f, SoundBuilder.Swell(0.4f), Waveform.Triangle)
                .Tone(0.84f, 0.35f, 110f, 118f, 0.66f, SoundBuilder.Swell(0.4f), Waveform.Triangle)
                .Noise(0f, 1.15f, 0.10f, SoundBuilder.Swell(0.5f), lowPassHz: 420f)
                .LowPass(900f)
                .SoftClip(1.4f)
                .NormalizeTo(0.70f)
                .FadeEdges(10f)
                .Build("sfx_boss_warning");
        }

        // ── UI ─────────────────────────────────────────────────────────────

        private static SoundEffect BuildMenuMove()
        {
            return new SoundBuilder(0.07f, seed: 51)
                .Tone(0f, 0.055f, 880f, 880f, 0.30f, SoundBuilder.Percussive(8f, 0.02f))
                .LowPass(4200f)
                .NormalizeTo(0.34f)
                .FadeEdges(3f)
                .Build("ui_move");
        }

        private static SoundEffect BuildMenuSelect()
        {
            return new SoundBuilder(0.18f, seed: 52)
                .Tone(0.00f, 0.07f, 660f, 660f, 0.34f, SoundBuilder.Ads(0.08f, 0.4f), Waveform.Triangle)
                .Tone(0.06f, 0.11f, 1320f, 1320f, 0.30f, SoundBuilder.Ads(0.06f, 0.5f), Waveform.Triangle)
                .LowPass(5200f)
                .NormalizeTo(0.50f)
                .FadeEdges(3f)
                .Build("ui_select");
        }

        private static SoundEffect BuildMenuBack()
        {
            return new SoundBuilder(0.16f, seed: 53)
                .Tone(0.00f, 0.07f, 620f, 620f, 0.30f, SoundBuilder.Ads(0.08f, 0.4f), Waveform.Triangle)
                .Tone(0.05f, 0.10f, 330f, 330f, 0.30f, SoundBuilder.Ads(0.06f, 0.5f), Waveform.Triangle)
                .LowPass(3800f)
                .NormalizeTo(0.44f)
                .FadeEdges(3f)
                .Build("ui_back");
        }
    }
}
