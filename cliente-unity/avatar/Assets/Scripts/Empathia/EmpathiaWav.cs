using System;
using System.IO;
using UnityEngine;

namespace Empathia
{
    /// <summary>
    /// Genera WAV de prueba (silencio) y captura corta de micrófono.
    /// </summary>
    public static class EmpathiaWav
    {
        public static byte[] BuildSilentWav(float seconds = 0.35f, int sampleRate = 16000)
        {
            var numSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            var data = new byte[numSamples * 2];
            return WrapPcm16Mono(data, sampleRate);
        }

        /// <param name="samplesRecorded">
        /// Muestras por canal a exportar (p. ej. Microphone.GetPosition).
        /// Si es &lt; 1, usa todo el clip.
        /// </param>
        public static byte[] FromMicrophoneClip(AudioClip clip, int samplesRecorded = -1, int sampleRate = 16000)
        {
            if (clip == null)
                return BuildSilentWav();

            var count = samplesRecorded > 0
                ? Mathf.Clamp(samplesRecorded, 1, clip.samples)
                : clip.samples;

            var samples = new float[count * clip.channels];
            if (!clip.GetData(samples, 0))
                return BuildSilentWav();

            // Mezcla a mono
            var mono = new float[count];
            if (clip.channels <= 1)
            {
                Array.Copy(samples, mono, mono.Length);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    float sum = 0f;
                    for (var c = 0; c < clip.channels; c++)
                        sum += samples[i * clip.channels + c];
                    mono[i] = sum / clip.channels;
                }
            }

            // Remuestreo lineal simple si hace falta
            var srcRate = clip.frequency;
            float[] outSamples;
            if (srcRate == sampleRate)
            {
                outSamples = mono;
            }
            else
            {
                var outLen = Mathf.Max(1, Mathf.RoundToInt(mono.Length * (sampleRate / (float)srcRate)));
                outSamples = new float[outLen];
                for (var i = 0; i < outLen; i++)
                {
                    var srcPos = i * (srcRate / (float)sampleRate);
                    var i0 = Mathf.Clamp(Mathf.FloorToInt(srcPos), 0, mono.Length - 1);
                    var i1 = Mathf.Min(i0 + 1, mono.Length - 1);
                    var t = srcPos - i0;
                    outSamples[i] = Mathf.Lerp(mono[i0], mono[i1], t);
                }
            }

            var pcm = new byte[outSamples.Length * 2];
            for (var i = 0; i < outSamples.Length; i++)
            {
                var s = Mathf.Clamp(outSamples[i], -1f, 1f);
                var v = (short)Mathf.RoundToInt(s * short.MaxValue);
                pcm[i * 2] = (byte)(v & 0xff);
                pcm[i * 2 + 1] = (byte)((v >> 8) & 0xff);
            }

            return WrapPcm16Mono(pcm, sampleRate);
        }

        public static string WriteTempWav(byte[] wavBytes, string fileName = "empathia-turn.wav")
        {
            var path = Path.Combine(Application.temporaryCachePath, fileName);
            File.WriteAllBytes(path, wavBytes);
            return path;
        }

        public static bool LooksLikeWav(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 12
                   && bytes[0] == (byte)'R' && bytes[1] == (byte)'I'
                   && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
                   && bytes[8] == (byte)'W' && bytes[9] == (byte)'A'
                   && bytes[10] == (byte)'V' && bytes[11] == (byte)'E';
        }

        public static bool LooksLikeMpeg(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3)
                return false;
            if (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
                return true;
            return bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0;
        }

        public static bool LooksLikeOgg(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 4
                   && bytes[0] == (byte)'O' && bytes[1] == (byte)'g'
                   && bytes[2] == (byte)'g' && bytes[3] == (byte)'S';
        }

        public static AudioClip TryCreateClip(byte[] wavBytes, string clipName, out string error)
        {
            error = null;
            float[] samples;
            int channels;
            int sampleRate;
            if (!TryDecodeWav(wavBytes, out samples, out channels, out sampleRate, out error))
                return null;

            var frames = samples.Length / channels;
            if (frames < 1)
            {
                error = "WAV de TTS sin muestras.";
                return null;
            }

            var clip = AudioClip.Create(
                string.IsNullOrEmpty(clipName) ? "empathia-tts" : clipName,
                frames,
                channels,
                sampleRate,
                false);
            if (!clip.SetData(samples, 0))
            {
                error = "No se pudieron cargar las muestras de TTS.";
                return null;
            }

            return clip;
        }

        public static float Peak(AudioClip clip)
        {
            if (clip == null || clip.samples < 1)
                return 0f;
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0))
                return 0f;
            var peak = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var a = Mathf.Abs(data[i]);
                if (a > peak)
                    peak = a;
            }

            return peak;
        }

        public static bool TryDecodeWav(
            byte[] bytes,
            out float[] samples,
            out int channels,
            out int sampleRate,
            out string error)
        {
            samples = null;
            channels = 1;
            sampleRate = 16000;
            error = null;

            if (!LooksLikeWav(bytes))
            {
                error = "El TTS no es un WAV RIFF.";
                return false;
            }

            var pos = 12;
            var audioFormat = 0;
            var bits = 0;
            byte[] data = null;

            while (pos + 8 <= bytes.Length)
            {
                var id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                var size = BitConverter.ToInt32(bytes, pos + 4);
                if (size < 0)
                    break;
                var body = pos + 8;
                if (body + size > bytes.Length)
                    break;

                if (id == "fmt ")
                {
                    if (size < 16)
                    {
                        error = "Chunk fmt de TTS inválido.";
                        return false;
                    }

                    audioFormat = BitConverter.ToInt16(bytes, body);
                    channels = BitConverter.ToInt16(bytes, body + 2);
                    sampleRate = BitConverter.ToInt32(bytes, body + 4);
                    bits = BitConverter.ToInt16(bytes, body + 14);
                    if (audioFormat == 0xFFFE && size >= 40)
                        audioFormat = BitConverter.ToInt16(bytes, body + 24);
                }
                else if (id == "data")
                {
                    data = new byte[size];
                    Buffer.BlockCopy(bytes, body, data, 0, size);
                }

                pos = body + size + (size & 1);
            }

            if (data == null || data.Length == 0)
            {
                error = "WAV de TTS sin chunk data.";
                return false;
            }

            if (channels < 1 || sampleRate < 1000 || bits < 8)
            {
                error = "WAV de TTS con formato no soportado.";
                return false;
            }

            var bytesPerSample = bits / 8;
            if (bytesPerSample < 1 || data.Length < bytesPerSample * channels)
            {
                error = "WAV de TTS incompleto.";
                return false;
            }

            var frames = data.Length / (bytesPerSample * channels);
            samples = new float[frames * channels];

            for (var i = 0; i < frames; i++)
            {
                for (var c = 0; c < channels; c++)
                {
                    var o = (i * channels + c) * bytesPerSample;
                    float v;
                    if (audioFormat == 3 && bits == 32)
                    {
                        v = BitConverter.ToSingle(data, o);
                    }
                    else if (bits == 8)
                    {
                        v = (data[o] - 128) / 128f;
                    }
                    else if (bits == 16)
                    {
                        v = BitConverter.ToInt16(data, o) / 32768f;
                    }
                    else if (bits == 24)
                    {
                        var n = data[o] | (data[o + 1] << 8) | (data[o + 2] << 16);
                        if ((n & 0x800000) != 0)
                            n |= unchecked((int)0xFF000000);
                        v = n / 8388608f;
                    }
                    else if (bits == 32)
                    {
                        v = BitConverter.ToInt32(data, o) / 2147483648f;
                    }
                    else
                    {
                        error = "TTS WAV de " + bits + " bits no soportado.";
                        samples = null;
                        return false;
                    }

                    samples[i * channels + c] = Mathf.Clamp(v, -1f, 1f);
                }
            }

            return true;
        }

        static byte[] WrapPcm16Mono(byte[] pcmData, int sampleRate)
        {
            using (var ms = new MemoryStream(44 + pcmData.Length))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + pcmData.Length);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(pcmData.Length);
                bw.Write(pcmData);
                return ms.ToArray();
            }
        }
    }
}
