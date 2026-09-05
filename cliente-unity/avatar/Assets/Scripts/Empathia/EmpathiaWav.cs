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
