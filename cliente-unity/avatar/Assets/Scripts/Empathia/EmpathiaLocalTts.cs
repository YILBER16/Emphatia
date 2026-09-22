using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Empathia
{
    /// <summary>
    /// Voz local en Windows si B aún no tiene el WAV de C.
    /// Lee el reply_text ya recibido; no inventa otra respuesta.
    /// </summary>
    public static class EmpathiaLocalTts
    {
        public static IEnumerator Speak(string text, AudioSource audioSource, Action<bool, string> onDone)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onDone(false, "Sin texto para voz local.");
                yield break;
            }

            var wavPath = Path.Combine(Application.temporaryCachePath, "empathia-local-tts.wav");
            var txtPath = Path.Combine(Application.temporaryCachePath, "empathia-local-tts.txt");
            var ps1Path = Path.Combine(Application.temporaryCachePath, "empathia-local-tts.ps1");

            try
            {
                File.WriteAllText(txtPath, text.Trim(), new UTF8Encoding(true));
                File.WriteAllText(ps1Path,
                    "Add-Type -AssemblyName System.Speech\n" +
                    "$txt = Get-Content -LiteralPath '" + txtPath.Replace("'", "''") + "' -Raw -Encoding UTF8\n" +
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\n" +
                    "$s.Rate = -1\n" +
                    "$es = $s.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo } |" +
                    " Where-Object { $_.Culture.Name -like 'es*' } | Select-Object -First 1\n" +
                    "if ($es) { $s.SelectVoice($es.Name) }\n" +
                    "$s.SetOutputToWaveFile('" + wavPath.Replace("'", "''") + "')\n" +
                    "$s.Speak($txt)\n" +
                    "$s.Dispose()\n",
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                onDone(false, "No se pudo preparar voz local: " + ex.Message);
                yield break;
            }

            Process process = null;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1Path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
                process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                onDone(false, "No se pudo iniciar voz local: " + ex.Message);
                yield break;
            }

            var waited = 0f;
            while (process != null && !process.HasExited && waited < 40f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (process != null && !process.HasExited)
            {
                try { process.Kill(); } catch { }
                onDone(false, "La voz local tardó demasiado.");
                yield break;
            }

            if (process != null && process.ExitCode != 0)
            {
                onDone(false, "Voz local falló.");
                yield break;
            }

            if (!File.Exists(wavPath))
            {
                onDone(false, "Voz local no escribió el WAV.");
                yield break;
            }

            string decodeError;
            var clip = EmpathiaWav.TryCreateClip(File.ReadAllBytes(wavPath), "empathia-local-tts", out decodeError);
            if (clip == null)
            {
                onDone(false, decodeError ?? "No se pudo leer la voz local.");
                yield break;
            }

            if (audioSource == null)
            {
                onDone(false, "Sin AudioSource.");
                yield break;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.mute = false;
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
            audioSource.clip = clip;
            audioSource.Play();
            onDone(true, "Reproduciendo voz (" + clip.length.ToString("0.0") + "s).");
        }
    }
}
