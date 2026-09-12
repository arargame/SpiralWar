using System;
using Microsoft.Xna.Framework.Audio;

namespace SpiralWar.Audio
{
    /// <summary>Oscillator shapes available to <see cref="SoundBuilder"/>.</summary>
    public enum Waveform
    {
        /// <summary>Pure sine. No harmonics at all — the safest, roundest tone.</summary>
        Sine,

        /// <summary>Band-limited-ish triangle. Slight odd harmonics, still very soft.</summary>
        Triangle,

        /// <summary>tanh-shaped square. Punchy but without the razor edges of a real square.</summary>
        SoftSquare
    }

    /// <summary>
    /// A tiny offline synthesizer. Sounds are rendered into a float buffer at load time and
    /// handed to MonoGame as 16-bit PCM, so the game ships with zero audio assets and the
    /// designer can retune a sound by editing numbers instead of opening a DAW.
    ///
    /// Design rule for this project: every sound ends with a low-pass, a soft clip and an
    /// edge fade. That combination is what keeps repeated fire from becoming fatiguing —
    /// no DC clicks at the buffer boundaries and no energy above ~7 kHz.
    /// </summary>
    public sealed class SoundBuilder
    {
        public const int DefaultSampleRate = 44100;

        private readonly float[] _samples;
        private readonly int _sampleRate;
        private readonly Random _rng;

        public int SampleRate => _sampleRate;
        public float DurationSeconds => _samples.Length / (float)_sampleRate;

        public SoundBuilder(float seconds, int sampleRate = DefaultSampleRate, int seed = 1337)
        {
            if (seconds <= 0f) seconds = 0.01f;
            _sampleRate = sampleRate;
            _samples = new float[Math.Max(1, (int)(seconds * sampleRate))];
            _rng = new Random(seed);
        }

        // ── Envelopes ──────────────────────────────────────────────────────

        /// <summary>Fast attack then exponential fall. The bread and butter of arcade SFX.</summary>
        public static Func<float, float> Percussive(float sharpness = 4f, float attack = 0.02f)
        {
            return t =>
            {
                if (t < attack) return t / attack;
                float k = (t - attack) / (1f - attack);
                return MathF.Exp(-sharpness * k);
            };
        }

        /// <summary>Attack / sustain / release, all as fractions of the tone length.</summary>
        public static Func<float, float> Ads(float attack = 0.06f, float release = 0.35f)
        {
            return t =>
            {
                if (t < attack) return t / attack;
                if (t > 1f - release) return (1f - t) / release;
                return 1f;
            };
        }

        /// <summary>Slow swell then fall. Used by warnings and stingers.</summary>
        public static Func<float, float> Swell(float peak = 0.35f)
        {
            return t => t < peak
                ? MathF.Sin(t / peak * MathF.PI * 0.5f)
                : MathF.Cos((t - peak) / (1f - peak) * MathF.PI * 0.5f);
        }

        // ── Generators ─────────────────────────────────────────────────────

        /// <summary>
        /// Adds an oscillator whose frequency glides from <paramref name="freqStart"/> to
        /// <paramref name="freqEnd"/> over its lifetime.
        /// </summary>
        public SoundBuilder Tone(float startSec, float durSec,
                                 float freqStart, float freqEnd,
                                 float amplitude,
                                 Func<float, float> envelope,
                                 Waveform wave = Waveform.Sine)
        {
            int first = TimeToIndex(startSec);
            int count = Math.Max(1, (int)(durSec * _sampleRate));
            float phase = 0f;
            float invSr = 1f / _sampleRate;

            for (int i = 0; i < count; i++)
            {
                int idx = first + i;
                if (idx < 0) continue;
                if (idx >= _samples.Length) break;

                float t = i / (float)count;
                float freq = freqStart + (freqEnd - freqStart) * t;
                phase += MathF.Tau * freq * invSr;
                if (phase > MathF.Tau) phase -= MathF.Tau;

                _samples[idx] += Oscillate(wave, phase) * envelope(t) * amplitude;
            }

            return this;
        }

        /// <summary>Adds white noise shaped by an envelope. Pre-filtered to stay gentle.</summary>
        public SoundBuilder Noise(float startSec, float durSec, float amplitude,
                                  Func<float, float> envelope,
                                  float lowPassHz = 6000f)
        {
            int first = TimeToIndex(startSec);
            int count = Math.Max(1, (int)(durSec * _sampleRate));

            // One-pole low-pass applied while generating, so the burst is never fizzy.
            float dt = 1f / _sampleRate;
            float rc = 1f / (MathF.Tau * MathF.Max(20f, lowPassHz));
            float a = dt / (rc + dt);
            float y = 0f;

            for (int i = 0; i < count; i++)
            {
                int idx = first + i;
                if (idx < 0) continue;
                if (idx >= _samples.Length) break;

                float x = (float)(_rng.NextDouble() * 2.0 - 1.0);
                y += a * (x - y);

                float t = i / (float)count;
                _samples[idx] += y * envelope(t) * amplitude;
            }

            return this;
        }

        /// <summary>
        /// Adds a short frequency-modulated "body" — a sine carrier whose pitch wobbles.
        /// Gives metallic impacts their character without harsh harmonics.
        /// </summary>
        public SoundBuilder FmBody(float startSec, float durSec, float carrierHz,
                                   float modHz, float modDepth, float amplitude,
                                   Func<float, float> envelope)
        {
            int first = TimeToIndex(startSec);
            int count = Math.Max(1, (int)(durSec * _sampleRate));
            float invSr = 1f / _sampleRate;
            float carrierPhase = 0f;
            float modPhase = 0f;

            for (int i = 0; i < count; i++)
            {
                int idx = first + i;
                if (idx < 0) continue;
                if (idx >= _samples.Length) break;

                float t = i / (float)count;

                modPhase += MathF.Tau * modHz * invSr;
                float mod = MathF.Sin(modPhase) * modDepth * (1f - t);

                carrierPhase += MathF.Tau * (carrierHz + mod) * invSr;
                _samples[idx] += MathF.Sin(carrierPhase) * envelope(t) * amplitude;
            }

            return this;
        }

        // ── Filters / shaping ──────────────────────────────────────────────

        public SoundBuilder LowPass(float cutoffHz)
        {
            float dt = 1f / _sampleRate;
            float rc = 1f / (MathF.Tau * MathF.Max(20f, cutoffHz));
            float a = dt / (rc + dt);
            float y = 0f;

            for (int i = 0; i < _samples.Length; i++)
            {
                y += a * (_samples[i] - y);
                _samples[i] = y;
            }
            return this;
        }

        public SoundBuilder HighPass(float cutoffHz)
        {
            float dt = 1f / _sampleRate;
            float rc = 1f / (MathF.Tau * MathF.Max(5f, cutoffHz));
            float a = rc / (rc + dt);
            float prevIn = 0f, prevOut = 0f;

            for (int i = 0; i < _samples.Length; i++)
            {
                float x = _samples[i];
                float y = a * (prevOut + x - prevIn);
                prevIn = x;
                prevOut = y;
                _samples[i] = y;
            }
            return this;
        }

        /// <summary>tanh saturation. Adds warmth and guarantees the buffer never clips hard.</summary>
        public SoundBuilder SoftClip(float drive = 1.6f)
        {
            float norm = MathF.Tanh(drive);
            if (norm <= 0f) return this;

            for (int i = 0; i < _samples.Length; i++)
                _samples[i] = MathF.Tanh(_samples[i] * drive) / norm;

            return this;
        }

        /// <summary>Scales the whole buffer so its loudest sample hits <paramref name="peak"/>.</summary>
        public SoundBuilder NormalizeTo(float peak = 0.8f)
        {
            float max = 0f;
            for (int i = 0; i < _samples.Length; i++)
            {
                float a = MathF.Abs(_samples[i]);
                if (a > max) max = a;
            }
            if (max < 1e-6f) return this;

            float scale = peak / max;
            for (int i = 0; i < _samples.Length; i++)
                _samples[i] *= scale;

            return this;
        }

        /// <summary>Ramps the first and last few milliseconds to zero to kill boundary clicks.</summary>
        public SoundBuilder FadeEdges(float milliseconds = 4f)
        {
            int n = Math.Max(1, (int)(milliseconds * 0.001f * _sampleRate));
            n = Math.Min(n, _samples.Length / 2);

            for (int i = 0; i < n; i++)
            {
                float g = i / (float)n;
                _samples[i] *= g;
                _samples[_samples.Length - 1 - i] *= g;
            }
            return this;
        }

        /// <summary>
        /// Cheap feedback echo. Used sparingly on stingers so they feel like they happen
        /// inside the neon arena rather than in a vacuum.
        /// </summary>
        public SoundBuilder Echo(float delaySeconds, float feedback = 0.35f, int taps = 2)
        {
            int delay = Math.Max(1, (int)(delaySeconds * _sampleRate));

            for (int tap = 1; tap <= taps; tap++)
            {
                float gain = MathF.Pow(feedback, tap);
                int offset = delay * tap;

                for (int i = _samples.Length - 1; i >= offset; i--)
                    _samples[i] += _samples[i - offset] * gain;
            }
            return this;
        }

        // ── Output ─────────────────────────────────────────────────────────

        /// <summary>Converts the float buffer into a mono 16-bit PCM <see cref="SoundEffect"/>.</summary>
        public SoundEffect Build(string name)
        {
            var pcm = new byte[_samples.Length * 2];

            for (int i = 0; i < _samples.Length; i++)
            {
                float s = _samples[i];
                if (s > 1f) s = 1f;
                else if (s < -1f) s = -1f;

                short v = (short)(s * short.MaxValue);
                pcm[i * 2] = (byte)(v & 0xFF);
                pcm[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
            }

            return new SoundEffect(pcm, _sampleRate, AudioChannels.Mono) { Name = name };
        }

        // ── Internals ──────────────────────────────────────────────────────

        private int TimeToIndex(float seconds) => (int)(seconds * _sampleRate);

        private static float Oscillate(Waveform wave, float phase)
        {
            switch (wave)
            {
                case Waveform.Triangle:
                    // asin(sin(x)) is a clean triangle with naturally rolled-off harmonics.
                    return 2f / MathF.PI * MathF.Asin(MathF.Sin(phase));

                case Waveform.SoftSquare:
                    return MathF.Tanh(MathF.Sin(phase) * 3f) / MathF.Tanh(3f);

                default:
                    return MathF.Sin(phase);
            }
        }
    }
}
