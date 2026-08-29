<?php

namespace App\Support;

/**
 * Espejo de consola de laboratorio.
 * En Windows, artisan serve no muestra bien stdout/stderr de cada request;
 * este archivo sí se puede ver en vivo con serve-lab.ps1.
 */
final class LabTerminal
{
    public static function path(): string
    {
        return storage_path('logs/lab-terminal.log');
    }

    public static function write(string $message): void
    {
        $line = '['.date('H:i:s').'] '.$message;
        if (! str_ends_with($line, "\n")) {
            $line .= "\n";
        }

        $path = self::path();
        $dir = dirname($path);
        if (! is_dir($dir)) {
            @mkdir($dir, 0777, true);
        }

        @file_put_contents($path, $line, FILE_APPEND | LOCK_EX);

        // Intento de consola (a veces no se ve en Windows + artisan serve).
        @file_put_contents('php://stdout', $line);
        @file_put_contents('php://stderr', $line);
        try {
            fwrite(\STDOUT, $line);
        } catch (\Throwable) {
            // ignore
        }
        try {
            fwrite(\STDERR, $line);
        } catch (\Throwable) {
            // ignore
        }

        try {
            logger()->info(trim($message));
        } catch (\Throwable) {
            // ignore si el contenedor aún no está listo
        }
    }
}
