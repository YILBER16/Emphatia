<?php

namespace App\Http\Middleware;

use App\Models\ApiToken;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class AuthenticateApiToken
{
    public function handle(Request $request, Closure $next): Response
    {
        $header = $request->header('Authorization', '');
        if (! str_starts_with($header, 'Bearer ')) {
            return response()->json([
                'error' => ['code' => 'UNAUTHORIZED', 'message' => 'Missing bearer token'],
            ], 401);
        }

        $plain = substr($header, 7);
        $token = ApiToken::query()
            ->where('token', hash('sha256', $plain))
            ->where(function ($q) {
                $q->whereNull('expires_at')->orWhere('expires_at', '>', now());
            })
            ->first();

        if (! $token) {
            return response()->json([
                'error' => ['code' => 'UNAUTHORIZED', 'message' => 'Invalid token'],
            ], 401);
        }

        $request->setUserResolver(fn () => $token->user);

        return $next($request);
    }
}
