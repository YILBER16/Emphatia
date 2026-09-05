using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Empathia
{
    /// <summary>
    /// Transcribe un WAV localmente (helper Python) sin llamar a C ni al stub de Whisper.
    /// </summary>
    public static class EmpathiaWavStt
    {
        [Serializable]
        class SttJson
        {
            public bool ok;
            public string text;
            public string error;
        }

        public static IEnumerator TranscribeWav(
            byte[] wavBytes,
            Action<bool, string, string> onDone)
        {
            if (wavBytes == null || wavBytes.Length < 44)
            {
                onDone(false, null, "Audio vacío.");
                yield break;
            }

            var wavPath = Path.Combine(Application.temporaryCachePath, "empathia-stt.wav");
            try
            {
                File.WriteAllBytes(wavPath, wavBytes);
            }
            catch (Exception ex)
            {
                onDone(false, null, "No se pudo guardar WAV: " + ex.Message);
                yield break;
            }

            var script = FindSttScript();
            if (string.IsNullOrEmpty(script))
            {
                onDone(false, null, "No encontré cliente-unity/tools/stt_wav.py");
                yield break;
            }

            yield return null;

            string usedCmd;
            var pyCmd = ResolvePythonCommand(script, wavPath, out usedCmd);
            if (pyCmd == null)
            {
                onDone(false, null, "No encontré Python (py/python) en PATH.");
                yield break;
            }

            Debug.Log("[Empathia] STT cmd: " + usedCmd);

            Process proc = null;
            System.Threading.Tasks.Task<string> outTask = null;
            System.Threading.Tasks.Task<string> errTask = null;
            Exception startEx = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pyCmd.Value.fileName,
                    Arguments = pyCmd.Value.args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                proc = Process.Start(psi);
                if (proc != null)
                {
                    outTask = proc.StandardOutput.ReadToEndAsync();
                    errTask = proc.StandardError.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                startEx = ex;
            }

            if (startEx != null)
            {
                onDone(false, null, "Error STT local: " + startEx.Message);
                yield break;
            }

            if (proc == null)
            {
                onDone(false, null, "No se pudo iniciar Python STT.");
                yield break;
            }

            var waited = 0f;
            while (!proc.HasExited && waited < 45f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!proc.HasExited)
            {
                try { proc.Kill(); } catch { /* ignore */ }
                try { proc.Dispose(); } catch { /* ignore */ }
                onDone(false, null, "Timeout al transcribir audio (45s).");
                yield break;
            }

            yield return null;

            string stdout = null;
            string stderr = null;
            var exitCode = proc.ExitCode;
            Exception readEx = null;
            try
            {
                stdout = outTask != null && outTask.IsCompleted
                    ? outTask.Result
                    : proc.StandardOutput.ReadToEnd();
                stderr = errTask != null && errTask.IsCompleted
                    ? errTask.Result
                    : proc.StandardError.ReadToEnd();
            }
            catch (Exception ex)
            {
                readEx = ex;
            }
            finally
            {
                try { proc.Dispose(); } catch { /* ignore */ }
            }

            if (readEx != null)
            {
                onDone(false, null, "Error leyendo STT: " + readEx.Message);
                yield break;
            }

            var line = FirstJsonLine(stdout);
            if (string.IsNullOrWhiteSpace(line))
            {
                var detail = string.IsNullOrWhiteSpace(stderr) ? ("exit=" + exitCode) : stderr.Trim();
                onDone(false, null, "STT sin respuesta. " + detail);
                yield break;
            }

            SttJson parsed = null;
            Exception parseEx = null;
            try
            {
                parsed = JsonUtility.FromJson<SttJson>(line);
            }
            catch (Exception ex)
            {
                parseEx = ex;
            }

            if (parseEx != null)
            {
                onDone(false, null, "JSON STT inválido: " + parseEx.Message + " | " + line);
                yield break;
            }

            if (parsed == null || !parsed.ok || string.IsNullOrWhiteSpace(parsed.text))
            {
                onDone(false, null, parsed != null && !string.IsNullOrWhiteSpace(parsed.error)
                    ? parsed.error
                    : "No se obtuvo texto del audio.");
                yield break;
            }

            Debug.Log("[Empathia] STT local (tu audio): " + parsed.text.Trim());
            onDone(true, parsed.text.Trim(), null);
        }

        static string FirstJsonLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            using (var reader = new StringReader(raw.Trim()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("{") && line.EndsWith("}"))
                        return line;
                }
            }
            return raw.Trim();
        }

        static string FindSttScript()
        {
            // Assets/Scripts/Empathia → …/cliente-unity/tools/stt_wav.py
            var fromData = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "stt_wav.py"));
            if (File.Exists(fromData))
                return fromData;

            var fromCwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "cliente-unity", "tools", "stt_wav.py"));
            if (File.Exists(fromCwd))
                return fromCwd;

            var fromRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "tools", "stt_wav.py"));
            if (File.Exists(fromRoot))
                return fromRoot;

            return null;
        }

        static (string fileName, string args)? ResolvePythonCommand(string script, string wavPath, out string usedCmd)
        {
            usedCmd = null;
            var quoted = "\"" + script + "\" \"" + wavPath + "\" es-ES";
            var attempts = new[]
            {
                ("py", "-3 " + quoted),
                ("python", quoted),
                ("python3", quoted),
            };

            foreach (var attempt in attempts)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = attempt.Item1,
                        Arguments = attempt.Item1 == "py" ? "-3 -c \"print(1)\"" : "-c \"print(1)\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null)
                            continue;
                        if (!proc.WaitForExit(4000))
                        {
                            try { proc.Kill(); } catch { /* ignore */ }
                            continue;
                        }
                        if (proc.ExitCode != 0)
                            continue;
                    }

                    usedCmd = attempt.Item1 + " " + attempt.Item2;
                    return (attempt.Item1, attempt.Item2);
                }
                catch
                {
                    // try next
                }
            }

            return null;
        }
    }
}
