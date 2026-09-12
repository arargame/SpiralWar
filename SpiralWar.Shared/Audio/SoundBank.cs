using System;
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

            return sounds;
        }

        // ── Weapons ────────────────────────────────────────────────────────

        /// <summary>Soft downward "thup". Deliberately dull: it is heard 5 times a second.</summary>
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

        /// <summary>The diffused/triple volley. Same character, wider and a touch lower.</summary>
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
        /// The "it hurt" tick. Short click for clarity + low body for weight.
        /// Played with pitch tied to the damage dealt so big hits read as bigger.
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

        /// <summary>Armored segments answer with a harder, more metallic clank.</summary>
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

        /// <summary>Segment breaks apart: filtered burst falling into a sub thump.</summary>
        private static SoundEffect BuildSegmentDestroy()
        {
            return new SoundBuilder(0.34f, seed: 23)
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

        // ── Supplies ───────────────────────────────────────────────────────

        /// <summary>Quiet chime telling the player something just became collectable.</summary>
        private static SoundEffect BuildSupplyDrop()
        {
            return new SoundBuilder(0.20f, seed: 31)
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
            return new SoundBuilder(0.26f, seed: 32)
                .Tone(0.00f, 0.10f, 659f, 659f, 0.40f, SoundBuilder.Ads(0.08f, 0.45f), Waveform.Triangle)
                .Tone(0.08f, 0.16f, 988f, 988f, 0.38f, SoundBuilder.Ads(0.06f, 0.55f), Waveform.Triangle)
                .Tone(0.08f, 0.16f, 1976f, 1976f, 0.08f, SoundBuilder.Ads(0.06f, 0.55f))
                .LowPass(6000f)
                .SoftClip(1.2f)
                .NormalizeTo(0.62f)
                .FadeEdges(4f)
                .Build("sfx_supply_pickup");
        }

        /// <summary>Major triad — warmer and longer than the weapon pickups.</summary>
        private static SoundEffect BuildHealthPickup()
        {
            return new SoundBuilder(0.40f, seed: 33)
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
            return new SoundBuilder(0.30f, seed: 41)
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
            return new SoundBuilder(1.10f, seed: 42)
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
            return new SoundBuilder(1.00f, seed: 43)
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
            return new SoundBuilder(1.20f, seed: 44)
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
