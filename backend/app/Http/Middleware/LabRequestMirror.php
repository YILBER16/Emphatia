<?php

namespace App\Http\Middleware;

use App\Support\LabTerminal;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class LabRequestMirror
{
    public function handle(Request $request, Closure $next): Response
    {
        $response = $next($request);

        $path = $request->path();
        $line = $request->method().' /'.$path.' -> '.$response->getStatusCode();

        // Destacar login / texto / audio / sesión
        if (str_contains($path, 'auth/login')) {
            $line = '>>> LOGIN '.$line;
        } elseif (str_contains($path, '/text') || str_ends_with($path, '/text')) {
            $text = (string) ($request->input('text') ?? $request->input('message') ?? '');
            $text = mb_substr(trim($text), 0, 200);
            $line = '>>> [A→B TEXTO] '.$text.' | '.$line;
        } elseif (str_contains($path, '/turns')) {
            $line = '>>> [A→B AUDIO] '.$line;
        } elseif ($request->isMethod('post') && $path === 'accompaniment/sessions') {
            $line = '>>> SESIÓN '.$line;
        }

        LabTerminal::write($line);

        return $response;
    }
}
